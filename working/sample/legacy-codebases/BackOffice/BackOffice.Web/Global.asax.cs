using System;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Configuration;
using Microsoft.Win32;
using log4net;

namespace BackOffice.Web
{
    public class Global : HttpApplication
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Global));

        protected void Application_Start(object sender, EventArgs e)
        {
            log4net.Config.XmlConfigurator.Configure();
            _log.Info("BackOffice application starting...");

            // Read configuration from Windows Registry
            try
            {
                string registryPath = ConfigurationManager.AppSettings["RegistryConfigPath"];
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath))
                {
                    if (key != null)
                    {
                        string environment = key.GetValue("Environment", "Production") as string;
                        string auditLevel = key.GetValue("AuditLevel", "Standard") as string;
                        string maxConcurrentUsers = key.GetValue("MaxConcurrentUsers", "100") as string;

                        Application["Environment"] = environment;
                        Application["AuditLevel"] = auditLevel;
                        Application["MaxConcurrentUsers"] = int.Parse(maxConcurrentUsers);

                        _log.InfoFormat("Registry config loaded - Environment: {0}, AuditLevel: {1}", environment, auditLevel);
                    }
                    else
                    {
                        _log.Warn("Registry key not found, using default configuration");
                        Application["Environment"] = "Production";
                        Application["AuditLevel"] = "Standard";
                        Application["MaxConcurrentUsers"] = 100;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Failed to read registry configuration", ex);
                Application["Environment"] = "Unknown";
                Application["AuditLevel"] = "Standard";
                Application["MaxConcurrentUsers"] = 50;
            }

            _log.Info("BackOffice application started successfully");
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            string userName = HttpContext.Current.User?.Identity?.Name ?? "Anonymous";
            Session["LoginTime"] = DateTime.Now;
            Session["UserName"] = userName;
            _log.InfoFormat("Session started for user: {0}", userName);
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            // Force HTTPS in production
            if (Application["Environment"]?.ToString() == "Production")
            {
                if (!Request.IsSecureConnection && !Request.IsLocal)
                {
                    string url = Request.Url.ToString().Replace("http:", "https:");
                    Response.Redirect(url, true);
                }
            }
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            if (HttpContext.Current.User != null && HttpContext.Current.User.Identity.IsAuthenticated)
            {
                // Windows auth is already handled by IIS
                // Just log for audit purposes
                if (Request.Path.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _log.DebugFormat("Authenticated request: {0} accessing {1}", 
                        HttpContext.Current.User.Identity.Name, Request.Path);
                }
            }
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Exception exception = Server.GetLastError();
            _log.Error("Unhandled application error", exception);

            string userName = HttpContext.Current?.User?.Identity?.Name ?? "Unknown";
            _log.ErrorFormat("Error occurred for user {0} on page {1}", userName, Request.Path);
        }

        protected void Session_End(object sender, EventArgs e)
        {
            _log.InfoFormat("Session ended for user: {0}", Session["UserName"]);
        }

        protected void Application_End(object sender, EventArgs e)
        {
            _log.Info("BackOffice application shutting down");
        }
    }
}
