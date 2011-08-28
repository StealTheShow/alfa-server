using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using AlfaServer.Entities;
using NLog;

namespace AlfaServer.models
{
    public class FloorsCollection : List<Floor>
    {
        private static FloorsCollection _floorsCollection;

        private Thread _initFloors;
        private FloorsCollection()
        {
            _initFloors = new Thread(GetFloors);
            _initFloors.Start();
        }

        public static FloorsCollection GetInstance()
        {
            if (_floorsCollection == null)
            {
                _floorsCollection = new FloorsCollection();
            }

            return _floorsCollection;
        }


        private Logger _logger = LogManager.GetCurrentClassLogger();

        public void GetFloors()
        {
            AlfaEntities alfaEntities = new AlfaEntities();
            _logger.Info("alfaEntities init");
            var query = from floor in alfaEntities.Floors
                        select floor;

            List<Floor> closedPorts = new List<Floor>();

            foreach (Floors floorsItem in query)
            {
                _logger.Info(floorsItem.ComPort);
                Floor floor = new Floor(floorsItem.ComPort);
                if (floor.IsOpen())
                {
                    InitFloor(floor);
                }
                else
                {
                    closedPorts.Add(floor);
                }
            }

            while (closedPorts.Count > 0)
            {
                for (int i = 0; i < closedPorts.Count; i++)
                {
                    if (closedPorts[i].Open())
                    {
                        InitFloor(closedPorts[i]);
                        closedPorts.Remove(closedPorts[i]);
                        i--;
                    }
                }

                Thread.Sleep(3 * 600);
            }
        }

        private void InitFloor(Floor floor)
        {
            //todo почему этот метод не внутри класса Floor???
            AlfaEntities alfaEntities = new AlfaEntities();

            Floors floorsItem = (from floors in alfaEntities.Floors.Include("Rooms")
                        where floors.ComPort == floor.PortName
                        select floors).First();

            foreach (Rooms room in floorsItem.Rooms)
            {
                if (room.OnLine != null)
                {
                    if (room.IsProtected != null)
                    {
                        floor.AddRoom((byte)room.ConrollerId, (bool)room.OnLine, (bool)room.IsProtected, room.RoomId);
                    }
                    else
                    {
                        floor.AddRoom((byte)room.ConrollerId, (bool)room.OnLine, false, room.RoomId);
                    }
                }
                else
                {
                    if (room.IsProtected != null)
                    {
                        floor.AddRoom((byte)room.ConrollerId, true, (bool)room.IsProtected, room.RoomId);
                    }
                    else
                    {
                        floor.AddRoom((byte)room.ConrollerId, true, false, room.RoomId);
                    }
                }
            }

            this.Add(floor);
            _logger.Info("init floor {0}, port {1}", floorsItem.FloorName, floorsItem.ComPort);
            floor.StartPolling();
        }
    }
}
