using System;
using System.Collections.Generic;
using System.Linq;
using AlfaServer.Entities;
using NLog;

namespace AlfaServer.models
{
    /// <summary>
    /// контроллер двери
    /// </summary>
    public class Room
    {
//        private readonly byte[] _key;

        /// <summary>
        /// инициаллизация контроллера двери
        /// </summary>
        /// <param name="number">номер контролера</param>
        /// <param name="keys">список ключей</param>
        /// <param name="onLine">отвечает контроллери или нет</param>
        /// <param name="isProtected">стоит на охране</param>
        /// <param name="roomId">идентификатор комнаты в таблице</param>
        /// <param name="keyNumber"> номер последнего считанного ключа 12 - означает что ключа не было считано</param>
        public Room(byte number, Dictionary<byte, ReadingKey> keys, bool onLine, bool isProtected, long roomId, byte keyNumber = (byte)12)
        {
            _roomId = roomId;
            _alfaEntities = new AlfaEntities();

            _currentRoom = (from room in _alfaEntities.Rooms.Include("Keys")
                           where room.RoomId == roomId
                           select room).FirstOrDefault();


            ControllerNumber = number;

            if (keys != null)
            {
                UpdateKeysState(keys);
            }
            
            LastKeyNumber = keyNumber;
            _onLine = onLine;
            _isProtected = isProtected;
        }

        private Logger _logger = LogManager.GetCurrentClassLogger();

        private Configuration _config = Configuration.GetInstance();

        private void UpdateKeysState(Dictionary<byte, ReadingKey> keys)
        {
            for (byte i = 0; i < _config.CountKeysCells; i++)
            {
                bool keyIsFound = false;
                _logger.Debug("step {0}", i);
                foreach (Keys keyItem in _currentRoom.Keys)
                {
                    if (keys.Count > 0)
                    {
                        if (keyItem.CellNumber == i)
                        {
                            _logger.Debug("keyItem.CellNumber {0}", keyItem.CellNumber);
                            if (keys[i].IsValid)
                            {
                                _logger.Debug("keys IsValid", keys[i].IsValid);
                                keyItem.keyCode = BitConverter.ToString(keys[i].Key);
                            }

                            keyIsFound = true;
                            break;
                        }
                    }
                    else
                    {
                        _logger.Error("roomId = {0} не удалось получить список ключей", _roomId);
                    }
                }

                if (!keyIsFound)
                {
                    _logger.Debug("key {0} not found", i);
                    Keys keysItem = new Keys();
                    keysItem.CellNumber = i;
                    keysItem.EndDate = null;
                    keysItem.CreateDate = DateTime.Now;
                    keysItem.Type = 0;
                    keysItem.keyCode = "00";
                    keysItem.FIO = "";
                    _currentRoom.Keys.Add(keysItem);
                }
            }

            _alfaEntities.SaveChanges();
        }

        private readonly AlfaEntities _alfaEntities;

        public void SaveChanges()
        {
            _alfaEntities.SaveChanges();
        }

        private long _roomId;

        public long RoomId
        {
            get { return _roomId; }
            set { _roomId = value; }
        }

        /// <summary>
        /// номер контроллера двери
        /// </summary>
        public byte ControllerNumber { get; set; }
        /// <summary>
        /// списко ключей, номера ключей и их значения
        /// </summary>
        public Dictionary<byte, byte[]> Keys { get; set; }

        /// <summary>
        /// номер последнего считанного ключа
        /// </summary>
        public byte LastKeyNumber { get; set; }

        private bool _onLine;
        //зависший контроллер или нет
        public bool OnLine
        {
            get { return _onLine; }
            set { _onLine = value; }
        }

        private bool _isProtected;
        // стоит на охране
        public bool IsProtected
        {
            get { return _isProtected; }
            set { _isProtected = value; }
        }

        private int _countReadError;
        // количество раз которые контроллер не ответил
        public int CountReadError
        {
            get { return _countReadError; }
            set { _countReadError = value; }
        }

        // геркон
        public bool SwitchGerkon
        {
            get
            {
                if ((_oldState & 128) != (_newState & 128))
                {
                    // состояние поменялось
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// установка состояния датчиков
        /// </summary>
        /// <param name="newState"></param>
        public void SetState(byte newState)
        {
            _oldState = _newState;
            _newState = newState;
        }

        // старое состояние датчиков
        private byte _oldState;
        // новое состояние датчиков
        private byte _newState;

        private Rooms _currentRoom;
        public Rooms CurrentRoom
        {
            get { return _currentRoom; }
        }

    }
}
