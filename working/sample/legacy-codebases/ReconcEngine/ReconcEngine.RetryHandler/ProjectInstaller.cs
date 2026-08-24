using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace ReconcEngine.RetryHandler
{
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller _serviceProcessInstaller;
        private ServiceInstaller _serviceInstaller;

        public ProjectInstaller()
        {
            _serviceProcessInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.User,
                Username = @"VOYAGER\svc_recon",
                Password = null // Set during installation
            };

            _serviceInstaller = new ServiceInstaller
            {
                ServiceName = "ReconcEngine.RetryHandler",
                DisplayName = "ReconcEngine Retry Handler Service",
                Description = "Retries failed reconciliation matches every 30 minutes with exponential backoff (max 3 attempts).",
                StartType = ServiceStartMode.Automatic,
                DelayedAutoStart = true
            };

            _serviceInstaller.AfterInstall += ServiceInstaller_AfterInstall;

            Installers.Add(_serviceProcessInstaller);
            Installers.Add(_serviceInstaller);
        }

        private void ServiceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            // Configure recovery options
            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo.FileName = "sc.exe";
                process.StartInfo.Arguments = "failure \"ReconcEngine.RetryHandler\" reset= 86400 actions= restart/60000/restart/60000//";
                process.StartInfo.UseShellExecute = false;
                process.Start();
                process.WaitForExit();
            }
        }
    }
}
