using System;
using System.Collections.Generic;

namespace AlfaServer.models
{
    class TestPort : IPort
    {
        public byte[] GetLastKey(byte controllerNumber)
        {
            throw new NotImplementedException();
        }

        public byte[] GetStateOfSensorAndNumberLastKey(byte controllerNumber)
        {
            throw new NotImplementedException();
        }

        public Dictionary<byte, ReadingKey> GetAllKeys(byte controllerNumber)
        {
            throw new NotImplementedException();
        }

        public void WriteBufferToPort(byte[] buffer)
        {
            throw new NotImplementedException();
        }

        public byte GetNumberLastRespondedController()
        {
            throw new NotImplementedException();
        }

        public bool IsOpen()
        {
            throw new NotImplementedException();
        }

        public bool Open()
        {
            throw new NotImplementedException();
        }
    }
}
