using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using AlfaServer.models;
using AlfaServer.Services;
using NLog;

namespace AlfaServer
{
    partial class AlfaService : ServiceBase
    {
        public AlfaService()
        {
            InitializeComponent();
        }

        private Logger _logger = LogManager.GetCurrentClassLogger();

        private ServiceHost _host;
        protected override void OnStart(string[] args)
        {
            // TODO: Add code here to start your service.
            _logger.Info("старт");
            FloorsCollection.GetInstance();

            var serviceType = typeof(ClientService);
            _host = new ServiceHost(serviceType);
            _host.Open();
        }

        protected override void OnStop()
        {
            // TODO: Add code here to perform any tear-down necessary to stop your service.

            _logger.Info("stop service");

            _host.Close();
        }
    }
}
