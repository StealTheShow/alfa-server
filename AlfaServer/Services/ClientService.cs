using System;
using System.ServiceModel;
using AlfaServer.Entities;


namespace AlfaServer.Services
{
    using models;

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class ClientService : IClientService
    {
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        public bool SetKey(byte[] key, byte number, string portName, byte controllerNumber, string name, DateTime endDate)
        {
            _logger.Info("service: set key cell {0}, controller {1}, port = {2}", number, controllerNumber, portName);
            FloorsCollection floorsCollection = FloorsCollection.GetInstance();

            foreach (Floor floorsCollectionItem in floorsCollection)
            {
                if (floorsCollectionItem.PortName == portName)
                {
                    floorsCollectionItem.SetKey(controllerNumber, number, key, name, endDate);
                    if (floorsCollectionItem.CheckingExistenceKey(controllerNumber, key))
                    {
                        return true;
                    }
                    return false;
                }
            }

            return false;
        }

        public bool UnsetKey(string portName, byte controllerNumber, byte number)
        {
            _logger.Info("service: unset key cell {0}, controller {1}, port = {2}", number, controllerNumber, portName);

            FloorsCollection floorsCollection = FloorsCollection.GetInstance();
            //todo помечать в базе время удаления
            foreach (Floor floorsCollectionItem in floorsCollection)
            {
                if (floorsCollectionItem.PortName == portName)
                {
                    floorsCollectionItem.UnsetKey(controllerNumber, number);
                    return true;
                }
            }

            return false;
        }

        public byte[] ReadKey(string portName)
        {
            _logger.Info("service: read key from port = {0}", portName);

            FloorsCollection floorsCollection = FloorsCollection.GetInstance();

            foreach (Floor floor in floorsCollection)
            {
                if (floor.PortName == portName)
                {
                    return floor.GetLastKey(0);
                }
            }
            return new byte[0];
        }

        public bool SetRoomToProtect(string portName, byte controllerNumber, bool isProtected)
        {
            _logger.Info("service: set room to protect {0} controller {1}, port = {2}", isProtected, controllerNumber, portName);

            FloorsCollection floorsCollection = FloorsCollection.GetInstance();
            foreach (Floor floorsCollectionItem in floorsCollection)
            {
                if (floorsCollectionItem.PortName == portName)
                {
                    if (floorsCollectionItem.SetRoomToProtect(controllerNumber, isProtected))
                    {
                        return true;
                    }

                    return false;
                }
            }

            return false;
        }

        public bool SetLight(string portName, byte controllerNumber, bool lightOn)
        {
            _logger.Info("service: set light on {0} controller {1}, port = {2}", lightOn, controllerNumber, portName);

            FloorsCollection floorsCollection = FloorsCollection.GetInstance();

            foreach (Floor floorsCollectionItem in floorsCollection)
            {
                if (floorsCollectionItem.PortName == portName)
                {
                    if (floorsCollectionItem.SetLight(controllerNumber, lightOn))
                    {
                        return true;
                    }

                    return false;
                }
            }

            return false;
        }

        public bool Join(string portName)
        {
            _logger.Info("service: client join to {0}", portName);
            IClientServiceCallback clientServiceCallback = OperationContext.Current.GetCallbackChannel<IClientServiceCallback>();

            FloorsCollection floorsCollection = FloorsCollection.GetInstance();

            foreach (Floor floor in floorsCollection)
            {
                if (floor.PortName.ToLower() == portName.ToLower())
                {
                    if (floor.IsOpen())
                    {
                        floor.ClientServiceCallback = clientServiceCallback;
                        return true;
                    }

                    return false;
                }
            }

            return false;
        }

        public bool SetMasterKey(byte[] key)
        {
            return true;
        }

        public bool AddRoomToFloor(string portName, int roomNumber, string roomClass, byte controllerNumber, 
            bool onLine, int roomCategory, bool isProtected)
        {
            _logger.Info("service: add floor to port = {0}, controller = {1}, room number = {2}", portName, controllerNumber, roomNumber);
            FloorsCollection floorsCollection = FloorsCollection.GetInstance();
            
            AlfaEntities alfaEntities = new AlfaEntities();

            foreach (Floor floor in floorsCollection)
            {
                if (floor.PortName == portName)
                {
                    
                    Rooms room = new Rooms();
                    room.FloorId = floor.CurrentFloor.FloorId;
                    room.RoomNumber = roomNumber;
                    room.RoomClass = roomClass;
                    room.ConrollerId = controllerNumber;
                    room.OnLine = onLine;
                    room.RoomCategoriesId = roomCategory;

                    alfaEntities.SaveChanges();

                    floor.AddRoom(controllerNumber, onLine, isProtected, room.RoomId);
                }
            }

            return false;
        }

        public bool AddFloor(string portName, string floorName)
        {
            _logger.Info("service: add floor to port {0} with name {1}", portName, floorName);
            Floors floor = new Floors();
            floor.ComPort = portName;
            floor.FloorName = floorName;

            AlfaEntities alfaEntities = new AlfaEntities();
            alfaEntities.SaveChanges();

            return true;
        }

        public bool StopFloorPolling(string portName)
        {
            FloorsCollection floorsCollection = FloorsCollection.GetInstance();

            foreach (Floor floor in floorsCollection)
            {
                if (floor.PortName == portName)
                {
                    floor.StopPolling();
                    return true;
                }
            }

            return false;
        }

        public bool StartFloorPolling(string portName)
        {
            FloorsCollection floorsCollection = FloorsCollection.GetInstance();

            foreach (Floor floor in floorsCollection)
            {
                if (floor.PortName == portName)
                {
                    floor.StartPolling();
                    return true;
                }
            }

            return false;
        }
    }
}