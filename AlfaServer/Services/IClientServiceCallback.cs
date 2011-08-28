using System.ServiceModel;

namespace AlfaServer.Services
{
    public interface IClientServiceCallback
    {
        [OperationContract(IsOneWay = true)]
        void AlertAboutControllerNotResponsible(string portName, byte controllerNumber);

        [OperationContract(IsOneWay = true)]
        void AlertGerkon(long roomId);

        [OperationContract(IsOneWay = true)]
        void AlertUnsetKey(string portName, byte controllerNumber);

        [OperationContract(IsOneWay = true)]
        void AlertChangeRoomsOnTheFloor(string portName);

        [OperationContract(IsOneWay = true)]
        void AlertChangeFloors(string portName);
    }
}
