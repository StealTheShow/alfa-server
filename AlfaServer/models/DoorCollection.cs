using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;

namespace AlfaServer.models
{
    public class DoorCollection : List<Door>
    {
        /// <summary>
        /// ком порт
        /// </summary>
        private readonly SerialPort _serialPort;

        private readonly Object _controllerLock = new Object();

        /// <summary>
        /// конструктор без параметров. вероятно вызываться не будет.
        /// </summary>
        public DoorCollection()
        {
            _threadStatusPolling = new Thread(StatusPulling);
            _threadStatusPolling.Priority = ThreadPriority.Lowest;
            _serialPort = new SerialPort();
            _serialPort.Open();
            _serialPort.ReadTimeout = _readTimeout;
            _serialPort.WriteTimeout = _writeTimeout;
        }

        /// <summary>
        /// конструктор для порта с заданным именем
        /// </summary>
        /// <param name="portName">имя порта</param>
        public DoorCollection(string portName)
        {
            _threadStatusPolling = new Thread(StatusPulling);
            _threadStatusPolling.Priority = ThreadPriority.Lowest;
            _serialPort = new SerialPort { PortName = portName };
            _serialPort.Open();
            _serialPort.ReadTimeout = _readTimeout;
            _serialPort.WriteTimeout = _writeTimeout;
        }

        /// <summary>
        /// опрос всех возможных контроллеров и получение привязанных к ним ключей
        /// </summary>
        public void Init()
        {
            for (byte doorControllerNumber = 1; doorControllerNumber < 255; doorControllerNumber++)
            {
                Dictionary<byte, byte[]> keys = this.GetAllKeys(doorControllerNumber);
                if (keys.Count > 0)
                {
                    Add(new Door(doorControllerNumber, keys));    
                }
                
            }

            _threadStatusPolling.Start();
        }

        private int _readTimeout = 500;
        private int _writeTimeout = 500;

        /// <summary>
        /// число повторов запросов к контроллеру в случае не соответствия контнольной суммы
        /// </summary>
        private byte _numberOfRepetitions = 3;

        public void SetNumberOfRepetitions(byte number)
        {
            _numberOfRepetitions = number;
        }

        public void SetReadTimeout(int timeout)
        {
            _readTimeout = timeout;
        }

        public void SetWriteTimeout(int timeout)
        {
            _writeTimeout = timeout;
        }

        private readonly Thread _threadStatusPolling;

        private void StatusPulling()
        {
            for (int i = 0; i < 50000; i++ )
            {
                foreach (var door in this)
                {
                    byte[] state = GetStateOfSensorAndNumberLastKey(door.Number);
                    door.LastKeyNumber = state[0];
                }
                Log.Instance.Write("опрос номер = " + i);
            }
        }

        /// <summary>
        /// 1.1.	Назначить ключ.
        /// </summary>
        /// <param name="controllerNumber"> номер контролера </param>
        /// <param name="keyNumber"> порядковый номер ключа 0 - 11 </param>
        /// <param name="keyCode">код ключа (массив 5 байт)</param>
        /// <returns></returns>
        public bool SetKey(byte controllerNumber, byte keyNumber, byte[] keyCode)
        {
            if (keyCode.Length != 5)
            {
                return false;
            }

            byte[] package = new byte[9];

            package[0] = controllerNumber;
            package[1] = 4;
            package[2] = keyNumber;
            package[3] = keyCode[0];
            package[4] = keyCode[1];
            package[5] = keyCode[2];
            package[6] = keyCode[3];
            package[7] = keyCode[4];
            package[8] = GetCheckSum(package);

            lock (_controllerLock)
            {
                Log.Instance.Write("SetKey");
                _serialPort.Write(package, 0, package.Length);
            }
            

            return true;
        }

        /// <summary>
        /// 1.2.	Отменить ключ.
        /// 
        /// формат пакета
        /// [байт 00] номер контроллера двери
        /// [байт 01] код команды (77)
        /// [байт 02] номер ключа в EEPOM, который будет отменён
        /// [байт 03] контрольная сумма
        /// </summary>
        /// <param name="controllerNumber"> номер контролера </param>
        /// <param name="keyNumber"> порядковый номер ключа 0 - 11 </param>
        /// <returns></returns>
        public bool UnsetKey(byte controllerNumber, byte keyNumber)
        {
            byte[] package = new byte[4];

            package[0] = controllerNumber;
            package[1] = 77;
            package[2] = keyNumber;
            package[3] = GetCheckSum(package);

            lock (_controllerLock)
            {
                Log.Instance.Write("UnsetKey");
                _serialPort.Write(package, 0, package.Length);    
            }
            
            return true;
        }

        /// <summary>
        /// 1.3.	Изменить номер контроллера.
        /// </summary>
        /// <param name="controllerNumber">номер контролера</param>
        /// <param name="newControllerNumber">новый номер контролера</param>
        /// <returns></returns>
        public bool ChangeControllerNumber(byte controllerNumber, byte newControllerNumber)
        {
            byte[] package = new byte[4];
            package[0] = controllerNumber;
            package[1] = 39;
            package[2] = newControllerNumber;
            package[3] = GetCheckSum(package);

            lock (_controllerLock)
            {
                Log.Instance.Write("ChangeControllerNumber");
                _serialPort.Write(package, 0, package.Length);
            }

            return true;
        }

        /// <summary>
        /// 1.4.	Установить состояние исполнительных устройств (состояние выходов).
        /// 
        /// формат пакета
        /// [байт 00] номер контроллера двери
        /// [байт 01] код команды (13)
        /// [байт 02] состояния выходов
        /// [байт 03] контрольная сумма
        /// 
        /// 1.4.4.	Биты байта 02 (состояния выходов) имеют следующий смысл:
        ///     [бит 7] не имеет смысла
        ///     [бит 6] состояние контакта 2 разъёма Х3
        ///     [бит 5] не имеет смысла
        ///     [бит 4] состояние контакта 4 разъёма Х3
        ///     [бит 3] состояние контакта 6 разъёма Х3
        ///     [бит 2] состояние контакта 8 разъёма Х3
        ///     [бит 1] не имеет смысла
        ///     [бит 0] не имеет смысла
        ///
        /// Значение : “1” в соответствующем бите приводит к появлению высокого уровня на соответствующем контакте разъёма.
        /// </summary>
        /// <param name="controllerNumber">номер контролера</param>
        /// <returns></returns>
        public bool SetStateOutputs(byte controllerNumber)
        {
            // todo: нифига не понятно, доделать позже
            return true;
        }

        /// <summary>
        /// 1.5.	Получить коды всех назначенных ключей (передать содержимое EEPROM).
        /// <param name="controllerNumber">номер контролера</param>
        /// <returns>Dictionary(byte, byte[])</returns>        
        /// </summary>
        public Dictionary<byte, byte[]> GetAllKeys(byte controllerNumber)
        {
            Dictionary<byte, byte[]> keys = ReadAllKeys(controllerNumber);
            return keys;
        }

        /// <summary>
        /// чтение ключей с контроллера
        /// </summary>
        /// <returns></returns>
        private Dictionary<byte, byte[]> ReadAllKeys(byte controllerNumber)
        {
            byte[] package = new byte[3];

            package[0] = controllerNumber;
            package[1] = 5;
            package[2] = GetCheckSum(package);

            lock (_controllerLock)
            {
                Log.Instance.Write("ReadAllKeys send command");
                _serialPort.Write(package, 0, package.Length);

                Dictionary<byte, byte[]> keys = new Dictionary<byte, byte[]>();

                byte[] buffer = new byte[1];

                byte bufferLenght = (byte)buffer.Length;


                //todo бред. надо что то другое придумать.
                try
                {
                    // 12 ключей
                    for (byte keyNumber = 0; keyNumber < 12; keyNumber++)
                    {
                        byte[] key = new byte[5];
                        for (byte i = 0; i < 5; i++)
                        {
                            _serialPort.Read(buffer, 0, bufferLenght);
                            key[i] = buffer[0];
                        }

                        // каждый шестой байт хранит контрольную сумму предыдущих 5
                        _serialPort.Read(buffer, 0, bufferLenght);

                        // todo сделать обработку не правильно считаных ключей
                        if (GetCheckSum(key) == buffer[0])
                        {
                            keys.Add(keyNumber, key);
                        }
                    }
                    Log.Instance.Write("ReadAllKeys read stop");

                    return keys;
                }
                catch (TimeoutException time)
                {
                    return keys;
                }
            }            
        }

        /// <summary>
        /// 1.6.	Получить состояние датчиков и номер последнего считанного ключа.
        /// 
        /// формат пакета
        /// [байт 00] номер контроллера двери
        /// [байт 01] код команды (243)
        /// [байт 02] контрольная сумма
        /// 
        /// Состав пакета ответа контроллера:
        /// [байт 00] состояния датчиков
        /// [байт 01] номер последнего считанного ключа
        /// [байт 02] контрольная сумма
        /// </summary>
        /// <param name="controllerNumber">номер контролера</param>
        public byte[] GetStateOfSensorAndNumberLastKey(byte controllerNumber)
        {
            byte[] readPackage;

            for (int i = 0; i < _numberOfRepetitions; i++)
            {
                readPackage = ReadStateOfSensorAndNumberLastKey(controllerNumber);
                if (readPackage.Length > 1)
                {
                    return readPackage;
                }
            }

            return new byte[0];
        }

        /// <summary>
        /// считывает значения состояния датчиков и номер последнего ключа с контроллера
        /// возвращает byte[0] массив если в процессе считывания возникла ошибка
        /// </summary>
        /// <returns>byte[2]</returns>
        private byte[] ReadStateOfSensorAndNumberLastKey(byte controllerNumber)
        {
            byte[] package = new byte[3];

            package[0] = controllerNumber;
            package[1] = 243;
            package[2] = GetCheckSum(package);

            lock (_controllerLock)
            {
                Log.Instance.Write("ReadStateOfSensorAndNumberLastKey send command");
                _serialPort.Write(package, 0, package.Length);

                byte[] result = new byte[3];
                byte[] buffer = new byte[1];

                byte bufferLenght = (byte)buffer.Length;
                byte packegeLenght = (byte)result.Length;

                for (byte i = 0; i < packegeLenght - 1; i++)
                {
                    _serialPort.Read(buffer, 0, bufferLenght);
                    result[i] = buffer[0];
                }

                _serialPort.Read(buffer, 0, bufferLenght);
                Log.Instance.Write("ReadStateOfSensorAndNumberLastKey read stop");

                if (GetCheckSum(result) == buffer[0])
                {
                    return result;
                }         
            }

            return new byte[0];
        }

        /// <summary>
        /// 1.7.	Получить код последнего считанного ключа.
        /// </summary>
        /// <param name="controllerNumber">номер контроллера</param>
        /// <returns>массив 5 байт</returns>
        public byte[] GetLastKey(byte controllerNumber)
        {
            byte[] readPackage;

            for (int i = 0; i < _numberOfRepetitions; i++)
            {
                readPackage = ReadLastKey(controllerNumber);
                if (readPackage.Length > 1)
                {
                    return readPackage;
                }
            }

            return new byte[0];
        }

        /// <summary>
        /// 1.7.	Получить код последнего считанного ключа.
        /// </summary>
        /// <param name="controllerNumber">номер контролера</param>
        /// <returns>массив 5 байт</returns>
        private byte[] ReadLastKey(byte controllerNumber)
        {
            byte[] package = new byte[3];

            package[0] = controllerNumber;
            package[1] = 243;
            package[2] = GetCheckSum(package);

            // блокировка чтения/записи ком порта одновременно из нескольких потоков
            lock (_controllerLock)
            {
                Log.Instance.Write("ReadLastKey send command");
                _serialPort.Write(package, 0, package.Length);


                byte[] packege = new byte[5];
                byte[] buffer = new byte[1];

                byte packegeLenght = (byte) packege.Length;
                byte bufferLenght = (byte) buffer.Length;

                for (byte i = 0; i < packegeLenght - 1; i++)
                {
                    _serialPort.Read(buffer, 0, bufferLenght);
                    packege[i] = buffer[0];
                }

                // байт контрольной суммы
                _serialPort.Read(buffer, 0, bufferLenght);
                Log.Instance.Write("ReadLastKey read stop");

                if (GetCheckSum(packege) == buffer[0])
                {
                    return packege;
                }
            }

            return new byte[0];
        }

        private static byte GetCheckSum(byte []package)
        {
            byte checkSum = 0;
            for (byte i = 0; i < package.Length; i++)
            {
                checkSum += package[i];
            }

            return checkSum;
        }
    }
}