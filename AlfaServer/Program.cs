using System.ServiceProcess;

namespace AlfaServer
{
    using models;


    static class Program
    {

        static void Main(string[] args)
        {
            new ConsoleApplication();

//            if (args[0] == "service")
//            {
//                ServiceBase[] ServicesToRun;
//                ServicesToRun = new ServiceBase[] 
//                { 
//                    new AlfaService() 
//                };
//                ServiceBase.Run(ServicesToRun);
//            }
//
//            if (args[0] == "console")
//            {
//                new ConsoleApplication();
//            }
        }
    }
}
