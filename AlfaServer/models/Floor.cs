using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using AlfaServer.Entities;
using AlfaServer.Services;
using NLog;

namespace AlfaServer.models
{
    public class ReadingKey
    {
        public ReadingKey(bool isValid, byte[] key)
        {
            IsValid = isValid;
            Key = key;
        }

        public bool IsValid;
        public byte[] Key;
    }

    public class Floor : List<Room>
    {
        public IClientServiceCallback ClientServiceCallback;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private Configuration _config = Configuration.GetInstance();

        /// <summary>
        /// конструктор для порта с заданным именем
        /// </summary>
        /// <param name="portName">имя порта</param>
        /// <param name="testingMode">запускать в тестовом режиме без подключения к ком портам</param>
        public Floor(string portName, bool testingMode = false)
        {
            _config = Configuration.GetInstance();

            _alfaEntities = new AlfaEntities();

            _currentFloor = (from floor in _alfaEntities.Floors.Include("Rooms")
                            where floor.ComPort == portName
                            select floor).FirstOrDefault();

            PortName = portName;
            _threadStatusPoll = new Thread(StatusPoll) {IsBackground = false, Priority = ThreadPriority.Lowest, Name = "Thread" + portName};

            _port = GetPort(portName, testingMode);
        }

        private IPort _port;

        /// <summary>
        /// для тестирования будет использоваться текстовый файл вместо порта
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="testingMode"></param>
        /// <returns></returns>
        private IPort GetPort(string portName, bool testingMode = false)
        {
            if (testingMode == false)
            {
                return new MoxaSerialPort(portName);
            }

            return new TestPort();
        }

        private readonly AlfaEntities _alfaEntities;

        public void AddRoom(byte controllerNumber, bool onLine, bool isProtected, long roomId)
        {
            //Dictionary<byte, ReadingKey> keys = this.GetAllKeys(controllerNumber);
            this.Add(new Room(controllerNumber, null, onLine, isProtected, roomId, 12));
            _logger.Info("room id = {0} add", roomId);
        }

        public bool IsOpen()
        {
            return _port.IsOpen();
        }

        public bool Open()
        {
            return _port.Open();
        }

        public void StartPolling()
        {
            _threadStatusPoll.Start();
        }

        public void StopPolling()
        {
            _threadStatusPoll.Abort();
        }

        /// <summary>
        /// число повторов запросов к контроллеру в случае не соответствия контнольной суммы
        /// </summary>
        private byte _numberOfRepetitions = 3;

        public void SetNumberOfRepetitions(byte number)
        {
            _numberOfRepetitions = number;
        }



        private readonly Thread _threadStatusPoll;

        /// <summary>
        /// количество опросов стоящих на охране датчиков между опросами не стоящих на охране датчиков
        /// </summary>
        int _intervalNonProtectedRoomPoll = 100;

        /// <summary>
        /// периодический опрос всех дверей на этаже
        /// </summary>
        private void StatusPoll()
        {
            int countPoll = 0;
            
            DateTime oldDate = DateTime.Now;
            TimeSpan timeSpan = new TimeSpan(0, 10, 0);
            bool checkTime = true;

            while (true)
            {
                DateTime currentDate = DateTime.Now;

                int time = _config.IntervalSleepTime * this.Count;
                int countOnLineRoom = 0;

                foreach (Room room in this)
                {
                    if (room.OnLine)
                    {
                        countOnLineRoom++;

                        if (room.IsProtected)
                        {
                            _logger.Debug("polling protected room {0}, controller {1}, port {2}", room.RoomId, _port.GetNumberLastRespondedController(), _portName);
                            PollingRoom(room, room.IsProtected);
                        }
                        else
                        {
                            if (countPoll < _intervalNonProtectedRoomPoll)
                            {
                                _logger.Debug("polling unprotected room {0}, controller {1}, port {2}", room.RoomId, _port.GetNumberLastRespondedController(), _portName);
                                PollingRoom(room, room.IsProtected);
                            }
                            countPoll = 0;
                        }

                        room.CountReadError = 0;
                    }
                    if (checkTime)
                    {
                        AlfaEntities alfaEntities = new AlfaEntities();
                        foreach (Keys key in room.CurrentRoom.Keys)
                        {
                            if (key.EndDate < currentDate)
                            {
                                key.RemoveDate = currentDate;
                                key.EndDate = null;
                                UnsetKey(room.ControllerNumber, (byte)key.CellNumber);
                            }
                        }

                        _logger.Info("room {0}, controller {1}, port {2} проверены и отменены ключи по времени");
                        alfaEntities.SaveChanges();
                    }
                    
                }

                if (countOnLineRoom > 0)
                {
                    Thread.Sleep(time / countOnLineRoom);
                }
                else
                {
                    _logger.Info("count online rooms = 0 on port = {0}", _portName);
                    Thread.Sleep(15000);
                }

                if (oldDate + timeSpan < currentDate)
                {
                    oldDate = currentDate;
                    checkTime = true;
                }
                else
                {
                    checkTime = false;
                }

                countPoll++;
                Thread.Yield();
            }
// ReSharper disable FunctionNeverReturns
        }
// ReSharper restore FunctionNeverReturns

        private void PollingRoom(Room room, bool isProtected)
        {
            byte[] state = GetStateOfSensorAndNumberLastKey(room.ControllerNumber);

            if (state.Length < 1)
            {
                room.CountReadError++;

                if (room.CountReadError > _maxCountReadError)
                {
                    if (ClientServiceCallback != null)
                    {
                        ClientServiceCallback.AlertAboutControllerNotResponsible(_portName, _port.GetNumberLastRespondedController());
                    }
                }

                _logger.Debug("ошибка считывания состояния датчика контроллера номер {0}. порт {1}",
                    _port.GetNumberLastRespondedController(),
                    PortName
                );

                return;
            }

            room.LastKeyNumber = state[1];
            room.SetState(state[0]);


            if (room.SwitchGerkon)
            {
                BitArray ba = new BitArray(new[] { state[0] });
                string outputState = "";
                foreach (bool b in ba)
                {
                    if (b)
                        outputState += "1 ";
                    else
                        outputState += "0 ";
                }

                _logger.Info("состояние геркон контроллера номер {0} изменилось. port {1} ({2})",
                    _port.GetNumberLastRespondedController(),
                    PortName,
                    outputState
                );

                if (ClientServiceCallback != null && isProtected)
                {
                    ClientServiceCallback.AlertGerkon(room.RoomId);
                }
            }
        }


        private int _maxCountReadError = Configuration.GetInstance().MaxCountReadError;

        private string _portName;
        public string PortName
        {
            get { return _portName; }
            set { _portName = value; }
        }

        /// <summary>
        /// 1.1.    Назначить ключ.
        /// </summary>
        /// <param name="controllerNumber"> номер контролера </param>
        /// <param name="keyNumber"> порядковый номер ключа 0 - 11 </param>
        /// <param name="keyCode">код ключа (массив 5 байт)</param>
        /// <param name="name">Фамилия Имя Отчество</param>
        /// <param name="endDate">Дата окончания срока действия ключа</param>
        /// <returns></returns>
        public bool SetKey(byte controllerNumber, byte keyNumber, byte[] keyCode, string name, DateTime endDate)
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

            try
            {
                _port.WriteBufferToPort(package);
            }
            catch(Exception)
            {
                return false;
            }

            foreach (Rooms room in CurrentFloor.Rooms)
            {
                Keys currentKey = (from key in room.Keys
                                  where key.CellNumber == keyNumber
                                  select key).First();

                currentKey.FIO = name;
                currentKey.RemoveDate = endDate;
                currentKey.keyCode = keyCode.ToString();
                _alfaEntities.SaveChanges();
            }
            return true;
        }

        /// <summary>
        /// 1.2.    Отменить ключ.
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

            try
            {
                _port.WriteBufferToPort(package);
            }
            catch (Exception)
            {
                return false;
            }

            foreach (Room room in this)
            {
                if (room.ControllerNumber == controllerNumber)
                {
                    foreach (Keys key in room.CurrentRoom.Keys)
                    {
                        if (key.CellNumber == keyNumber)
                        {
                            key.FIO = "";
                            key.CreateDate = null;
                            key.keyCode = "";
                            key.GuestIdn = null;
                            _alfaEntities.SaveChanges();
                        }

                        break;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 1.3.    Изменить номер контроллера.
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

            try
            {
                _port.WriteBufferToPort(package);
            }
            catch(Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 1.4.    Установить состояние исполнительных устройств (состояние выходов).
        /// 
        /// формат пакета
        /// [байт 00] номер контроллера двери
        /// [байт 01] код команды (13)
        /// [байт 02] состояния выходов
        /// [байт 03] контрольная сумма
        /// 
        /// 1.4.4.    Биты байта 02 (состояния выходов) имеют следующий смысл:
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
        ///<param name="outputState">состояние выходов</param>
        ///
        ///<returns></returns>
        private bool SetStateOutputs(byte controllerNumber, byte outputState)
        {
            byte[] package = new byte[9];

            package[0] = controllerNumber;
            package[1] = 13;
            package[2] = outputState;
            package[3] = GetCheckSum(package);


            try
            {
                _port.WriteBufferToPort(package);
            }
            catch (Exception)
            {
                return false;
            }


            return true;
        }


        /// <summary>
        /// 1.5.    Получить коды всех назначенных ключей (передать содержимое EEPROM).
        /// <param name="controllerNumber">номер контролера</param>
        /// <returns>Dictionary(byte, byte[])</returns>
        /// </summary>
        public Dictionary<byte, ReadingKey> GetAllKeys(byte controllerNumber)
        {
            Dictionary<byte, ReadingKey> resultKeys = new Dictionary<byte, ReadingKey>();
            for (byte i = 0; i < _config.CountKeysCells; i++)
            {
                resultKeys.Add(i, new ReadingKey(false, new byte[0]));
            }

            Dictionary<byte, ReadingKey> keys;
            for (byte i = 0; i < _config.CountReadingsAllKeys; i++)
            {
                bool readingSuccesfuly = true;
                keys = _port.GetAllKeys(controllerNumber);
                foreach (KeyValuePair<byte, ReadingKey> keyValuePair in keys)
                {
                    _logger.Debug("read all keys cell {0}, key {1}, valid {2}", keyValuePair.Key, BitConverter.ToString(keyValuePair.Value.Key), keyValuePair.Value.IsValid);
                }
                if (keys.Count > 0)
                {
                    _logger.Debug("попали плиат!");
                    foreach (KeyValuePair<byte, ReadingKey> keyValuePair in keys)
                    {
                        _logger.Debug("foreach плиат!");
                        if (keyValuePair.Value.IsValid)
                        {
                            _logger.Debug("cell {0} valid плиат!", keyValuePair.Key);
                            resultKeys[keyValuePair.Key] = keyValuePair.Value;
                        }
                        else
                        {
                            _logger.Debug("cell {0} not valid плиат!", keyValuePair.Key);
                            readingSuccesfuly = false;
                        }
                    }

                    if (readingSuccesfuly)
                    {
                        _logger.Debug("reading succesful");
                        break;
                    }
                        
                }
            }

            return resultKeys;
        }


        /// <summary>
        /// 1.6.    Получить состояние датчиков и номер последнего считанного ключа.
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
                readPackage = _port.GetStateOfSensorAndNumberLastKey(controllerNumber);
                if (readPackage.Length > 1)
                {
                    return readPackage;
                }
            }

            return new byte[0];
        }


        /// <summary>
        /// 1.7.    Получить код последнего считанного ключа.
        /// </summary>
        /// <param name="controllerNumber"> номер контроллера </param>
        /// <returns> массив 5 байт </returns>
        public byte[] GetLastKey(byte controllerNumber)
        {
            byte[] readPackage;

            for (int i = 0; i < _numberOfRepetitions; i++)
            {
                readPackage = _port.GetLastKey(controllerNumber);
                if (readPackage.Length > 1)
                {
                    return readPackage;
                }
            }

            return new byte[0];
        }

        /// <summary>
        /// функция подсчета контрольной суммы
        /// </summary>
        /// <param name="package"> массив для которого будет расчитана контрольная сумма </param>
        /// <returns> контрольная сумма </returns>
        private static byte GetCheckSum(byte []package)
        {
            byte checkSum = 0;
            for (byte i = 0; i < package.Length; i++)
            {
                checkSum += package[i];
            }

            return checkSum;
        }

        public bool CheckingExistenceKey(byte controllerNumber, byte[] key)
        {
            Dictionary<byte, ReadingKey> keys = GetAllKeys(controllerNumber);

            foreach (KeyValuePair<byte, ReadingKey> keyValuePair in keys)
            {
                bool isValid = true;
                if (keyValuePair.Value.Key.Length == key.Length)
                {
                    for (int i = 0; i < key.Length; i++)
                    {
                        if (keyValuePair.Value.Key[i] != key[i])
                        {
                            isValid = false;
                        }
                    }

                    if (isValid)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool SetRoomToProtect(byte controllerNumber, bool isProtected)
        {
            foreach (Room room in this)
            {
                if (room.ControllerNumber == controllerNumber)
                {
                    room.IsProtected = isProtected;

                    var currentRoom =
                        (from rooms in _alfaEntities.Rooms
                         where rooms.RoomId == room.RoomId
                         select rooms).First();

                    currentRoom.IsProtected = isProtected;

                    _alfaEntities.SaveChanges();

                    return true;
                }
            }

            return false;
        }

        public bool SetLight(byte controllerNumber, bool lightOn)
        {
            //todo уточнить управляющий бит света
            byte state;

            if (lightOn)
            {
                state = 64;
            }
            else
            {
                state = 0;
            }

            if (SetStateOutputs(controllerNumber, state))
            {
                foreach (Room room in this)
                {
                    if (room.ControllerNumber == controllerNumber)
                    {
                        var currentRoom =
                            (from rooms in _alfaEntities.Rooms
                             where rooms.ConrollerId == controllerNumber
                             select rooms).First();

                        currentRoom.LightOn = lightOn;

                        _alfaEntities.SaveChanges();

                        return true;
                    }
                }

                return true;
            }

            return false;
        }

        private Floors _currentFloor;

        public Floors CurrentFloor
        {
            get { return _currentFloor; }
        }
    }
}