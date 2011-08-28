using System.Collections.Generic;
using NUnit.Framework;

namespace AlfaServerTest
{
    using AlfaServer.models;
    public class DoorTest
    {
        [Test]
        public void TestConstruct()
        {
            Dictionary<byte, ReadingKey> keys = new Dictionary<byte, ReadingKey>();

            keys.Add(1, new ReadingKey(true, new byte[] {1, 2, 3, 4, 5}));
            keys.Add(2, new ReadingKey(true, new byte[] {6, 7, 8, 9, 10}));

            Room room = new Room(1, keys, true, true, 1);

            Assert.That(room.Keys, Is.EqualTo(keys));
            Assert.That(room.ControllerNumber, Is.EqualTo(1));

            room = new Room(1, keys, true, true, 1);

            Assert.That(room.Keys, Is.EqualTo(keys));
            Assert.That(room.ControllerNumber, Is.EqualTo(1));
        }
    }
}
