using System.Collections.Generic;
using System.IO.Ports;


namespace AlfaServer.models
{
    /// <summary>
    /// Контроллер.
    /// Управление одним ком портом.
    /// 
    /// todo добавить повторное считывание данных при не совпадении контрольной суммы.
    /// 
    /// </summary>
    class PortModel
    {
        private readonly SerialPort _serialPort;

        private int _readTimeout = 500;
        private int _writeTimeout = 500;

        public PortModel()
        {
            _serialPort = new SerialPort();
            _serialPort.Open();
            _serialPort.ReadTimeout = _readTimeout;
            _serialPort.WriteTimeout = _writeTimeout;
        }

        public PortModel(string portName)
        {
            _serialPort = new SerialPort {PortName = portName};
            _serialPort.Open();
            _serialPort.ReadTimeout = _readTimeout;
            _serialPort.WriteTimeout = _writeTimeout;            
        }

        public void SetReadTimeout(int timeout)
        {
            _readTimeout = timeout;
        }

        public void SetWriteTimeout(int timeout)
        {
            _writeTimeout = timeout;
        }

        /// <summary>
        /// 1.1.	Назначить ключ.
        /// 
        /// формат пакета
        /// [байт 00] номер контроллера двери
        /// [байт 01] код команды (4)
        /// [байт 02] номер ключа в EEPROM, который будет назначен
        /// [байт 03] код ключа, байт 0
        /// [байт 04] код ключа, байт 1
        /// [байт 05] код ключа, байт 2
        /// [байт 06] код ключа, байт 3
        /// [байт 07] код ключа, байт 4
        /// [байт 08] контрольная сумма
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
            package[2] = keyCode[0];
            package[3] = keyCode[0];
            package[4] = keyCode[1];
            package[5] = keyCode[2];
            package[6] = keyCode[3];
            package[7] = keyCode[4];
            package[8] = GetCheckSum(package);

            _serialPort.Write(package, 0, package.Length);

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

            _serialPort.Write(package, 0, package.Length);

            return true;
        }

        /// <summary>
        /// 1.3.	Изменить номер контроллера.
        /// 
        /// формат пакета
        /// [байт 00] номер контроллера двери
        /// [байт 01] код команды (39)
        /// [байт 02] новый номер контроллера двери
        /// [байт 03] контрольная сумма
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

            _serialPort.Write(package, 0, package.Length);

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
        /// 
        /// формат пакета
        /// [байт 00] номер контроллера двери
        /// [байт 01] код команды (5)
        /// [байт 02] контрольная сумма
        /// 
        /// формат ответа
        /// [байт 00] код ключа 0, байт 0
        /// [байт 01] код ключа 0, байт 1
        /// [байт 02] код ключа 0, байт 2
        /// [байт 03] код ключа 0, байт 3
        /// [байт 04] код ключа 0, байт 4
        /// [байт 05] контрольная сумма байтов 00 – 04
        /// 12 раз
        /// 
        /// <param name="controllerNumber">номер контролера</param>
        /// <returns>Dictionary(byte, byte[])</returns>        
        /// </summary>
        public Dictionary<byte, byte[]> GetAllKeys(byte controllerNumber)
        {
            byte[] package = new byte[3];

            package[0] = controllerNumber;
            package[1] = 5;
            package[2] = GetCheckSum(package);

            _serialPort.Write(package, 0, package.Length);

            Dictionary<byte, byte[]> keys = ReadAllKeys();

            return keys;
        }
        
        /// <summary>
        /// чтение ключей с контроллера
        /// </summary>
        /// <returns></returns>
        private Dictionary<byte, byte[]> ReadAllKeys()
        {
            Dictionary<byte, byte[]> keys = new Dictionary<byte, byte[]>();

            byte[] buffer = new byte[1];

            byte bufferLenght = (byte)buffer.Length;

            for (byte keyNumber = 0; keyNumber < 12; keyNumber++ )
            {
                byte []key = new byte[5];
                for (byte i = 0; i < 6; i++)
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

            return keys;
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
            byte[] package = new byte[3];

            package[0] = controllerNumber;
            package[1] = 243;
            package[2] = GetCheckSum(package);

            _serialPort.Write(package, 0, package.Length);

            byte[] readPackage = ReadStateOfSensorAndNumberLastKey();

            return readPackage;
        }

        /// <summary>
        /// считывает значения состояния датчиков и номер последнего ключа с контроллера
        /// возвращает byte[0] массив если в процессе считывания возникла ошибка
        /// </summary>
        /// <returns>byte[2]</returns>
        private byte[] ReadStateOfSensorAndNumberLastKey()
        {
            byte[] result = new byte[3];
            byte[] buffer = new byte[1];

            byte bufferLenght = (byte)buffer.Length;
            byte packegeLenght = (byte)result.Length;

            for (byte i = 0; i < packegeLenght - 1; i++)
            {
                _serialPort.Read(buffer, 0, bufferLenght) ;
                result[i] = buffer[0];
            }

            _serialPort.Read(buffer, 0, bufferLenght);
            
            if (GetCheckSum(result) == buffer[0])
            {
                return result;
            }
            return new byte[0];
        }

        /// <summary>
        /// 1.7.	Получить код последнего считанного ключа.
        /// 
        /// формат пакета
        /// [байт 00] номер контроллера двери
        /// [байт 01] код команды
        /// [байт 02] контрольная сумма
        /// 
        /// Состав пакета ответа контроллера:
        /// [байт 00] код ключа, байт 0
        /// [байт 01] код ключа, байт 1
        /// [байт 02] код ключа, байт 2
        /// [байт 03] код ключа, байт 3
        /// [байт 04] код ключа, байт 4
        /// [байт 05] контрольная сумма
        /// </summary>
        /// <param name="controllerNumber">номер контролера</param>
        public byte[] GetLastKey(byte controllerNumber)
        {
            byte[] package = new byte[3];

            package[0] = controllerNumber;
            package[1] = 243;
            package[2] = GetCheckSum(package);

            _serialPort.Write(package, 0, package.Length);

            byte[] readPackage = ReadLastKey();

            return readPackage;
        }

        private byte[] ReadLastKey()
        {
            
            byte[] packege = new byte[5];
            byte[] buffer = new byte[1];

            byte packegeLenght = (byte)packege.Length;
            byte bufferLenght = (byte)buffer.Length;

            for (byte i = 0; i < packegeLenght - 1; i++)
            {
                _serialPort.Read(buffer, 0, bufferLenght);
                packege[i] = buffer[0];
            }

            // байт контрольной суммы
            _serialPort.Read(buffer, 0, bufferLenght);

            if (GetCheckSum(packege) == buffer[0])
            {
                return packege;
            }

            return new byte[0];
        }

        private static byte GetCheckSum(byte []package)
        {
            byte checkSum = 0;
            for (byte i = 0; i < package.Length - 1; i++)
            {
                checkSum += package[i];
            }

            return checkSum;
        }
    }
}
