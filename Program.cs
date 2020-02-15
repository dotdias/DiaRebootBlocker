using System.ServiceProcess;

namespace DiaRebootBlocker
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new DiaRebootBlockerService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
