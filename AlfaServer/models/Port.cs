using System.Collections.Generic;


namespace AlfaServer.models
{
    interface IPort
    {
        byte[] GetLastKey(byte controllerNumber);
        byte[] GetStateOfSensorAndNumberLastKey(byte controllerNumber);
        Dictionary<byte, ReadingKey> GetAllKeys(byte controllerNumber);
        void WriteBufferToPort(byte[] buffer);
        byte GetNumberLastRespondedController();
        bool IsOpen();
        bool Open();
    }
}
