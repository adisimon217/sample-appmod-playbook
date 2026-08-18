using System;
using System.ServiceProcess;

namespace ComplianceReporter.Service
{
    /// <summary>
    /// Entry point for the Compliance Reporter Windows Service.
    /// Runs as svc_compliance@voyager.local service account.
    /// DO NOT MODIFY - this service has been running stable since 2014.
    /// </summary>
    static class Program
    {
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ComplianceService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
