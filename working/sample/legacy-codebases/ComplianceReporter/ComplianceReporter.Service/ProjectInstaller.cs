using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace ComplianceReporter.Service
{
    /// <summary>
    /// Installs the ComplianceReporter Windows Service.
    /// Service runs under svc_compliance@voyager.local domain account.
    /// 
    /// Install command:
    ///   installutil ComplianceReporter.Service.exe
    /// 
    /// The service account requires:
    /// - Read access to SQLPROD01 (ComplianceReporting database)
    /// - Write access to D:\ComplianceReports\
    /// - Network access to sftp.mas.gov.sg:22
    /// - "Log on as a service" local policy right
    /// </summary>
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller processInstaller;
        private ServiceInstaller serviceInstaller;

        public ProjectInstaller()
        {
            processInstaller = new ServiceProcessInstaller();
            serviceInstaller = new ServiceInstaller();

            // Service account configuration
            // NOTE: Password is set during installation via installutil /username /password
            // or via the Services MMC snap-in after installation
            processInstaller.Account = ServiceAccount.User;
            processInstaller.Username = @"voyager\svc_compliance";
            processInstaller.Password = null; // Set during installation

            // Service properties
            serviceInstaller.ServiceName = "VoyagerComplianceReporter";
            serviceInstaller.DisplayName = "Voyager Compliance Reporter Service";
            serviceInstaller.Description =
                "Generates MAS regulatory compliance reports (monthly, quarterly, annual) " +
                "and uploads them to the MAS SFTP endpoint. Runs weekly on Sunday at 02:00 SGT.";
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.ServicesDependedOn = new string[]
            {
                "MSSQLSERVER",  // Depends on SQL Server being available
                "LanmanWorkstation"  // Network access for SFTP
            };

            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }

        /// <summary>
        /// After installation, ensure the Windows Event Log source is registered.
        /// </summary>
        public override void Install(System.Collections.IDictionary stateSaver)
        {
            base.Install(stateSaver);

            // Register Event Log source if not already present
            string source = "ComplianceReporter";
            string logName = "Application";

            if (!System.Diagnostics.EventLog.SourceExists(source))
            {
                System.Diagnostics.EventLog.CreateEventSource(source, logName);
            }
        }

        public override void Uninstall(System.Collections.IDictionary savedState)
        {
            base.Uninstall(savedState);

            // Remove Event Log source
            string source = "ComplianceReporter";
            if (System.Diagnostics.EventLog.SourceExists(source))
            {
                System.Diagnostics.EventLog.DeleteEventSource(source);
            }
        }
    }
}
