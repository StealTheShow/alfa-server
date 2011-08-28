using System.ServiceModel;
using AlfaServer.models;
using AlfaServer.Services;
using NLog;

namespace AlfaServer
{
    class ConsoleApplication
    {
        private Logger _logger = LogManager.GetCurrentClassLogger();

        private ServiceHost _host;

        public ConsoleApplication()
        {
            _logger.Info("старт");

            _logger.Info("init collection");
            FloorsCollection.GetInstance();

            _logger.Info("init service");

            var serviceType = typeof(ClientService);
            _host = new ServiceHost(serviceType);
            _logger.Info("opening service");
            _host.Open();
            

        }
    }
}
