using System.Collections.Generic;
using NUnit.Framework;

namespace AlfaServerTest
{
    using AlfaServer.models;
    public class FloorTest
    {
        [Test]
        public void TestConstruct()
        {
            Floor floor = new Floor("com3000", true);
            byte controllerNumber = 100;
            bool onLine = true;
            bool isProtected = true;
            long roomId = 100;
            floor.AddRoom(controllerNumber, onLine, isProtected, roomId);
            
        }
    }
}
