using System;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.SessionState;
using Hangfire;
using log4net;
using log4net.Config;
using MerchantHub.Core;
using MerchantHub.Core.Services;
using MerchantHub.Web.App_Start;

[assembly: XmlConfigurator(Watch = true)]

namespace MerchantHub.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MvcApplication));
        private BackgroundJobServer _backgroundJobServer;

        protected void Application_Start()
        {
            _log.Info("========================================");
            _log.Info("MerchantHub Application Starting...");
            _log.Info("Environment: " + System.Configuration.ConfigurationManager.AppSettings["MerchantHub.Environment"]);
            _log.Info("Version: " + System.Configuration.ConfigurationManager.AppSettings["MerchantHub.Version"]);
            _log.Info("========================================");

            try
            {
                // Initialize logging
                XmlConfigurator.Configure();

                // Initialize service locator (our poor man's DI)
                ServiceLocator.Initialize();

                // MVC configuration
                AreaRegistration.RegisterAllAreas();
                GlobalConfiguration.Configure(WebApiConfig.Register);
                FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
                RouteConfig.RegisterRoutes(RouteTable.Routes);
                BundleConfig.RegisterBundles(BundleTable.Bundles);

                // Hangfire configuration
                HangfireConfig.Configure();
                _backgroundJobServer = new BackgroundJobServer();

                // Schedule recurring jobs
                ScheduleRecurringJobs();

                // Verify report output directory exists
                var reportPath = System.Configuration.ConfigurationManager.AppSettings["MerchantHub.ReportOutputPath"];
                if (!System.IO.Directory.Exists(reportPath))
                {
                    _log.WarnFormat("Report output directory does not exist: {0}. Creating...", reportPath);
                    System.IO.Directory.CreateDirectory(reportPath);
                }

                _log.Info("MerchantHub Application Started Successfully");
            }
            catch (Exception ex)
            {
                _log.Fatal("FATAL ERROR during Application_Start", ex);
                throw;
            }
        }

        private void ScheduleRecurringJobs()
        {
            _log.Info("Scheduling Hangfire recurring jobs...");

            // Nightly statement generation - runs at 2:00 AM
            RecurringJob.AddOrUpdate<IReportService>(
                "nightly-statements",
                svc => svc.GenerateNightlyStatements(),
                "0 2 * * *",
                TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));

            // Hourly cache refresh - refresh merchant statistics cache
            RecurringJob.AddOrUpdate<IMerchantService>(
                "hourly-cache-refresh",
                svc => svc.RefreshMerchantCache(),
                Cron.Hourly);

            // Daily cleanup - purge old temp files and expired sessions
            RecurringJob.AddOrUpdate<IMerchantService>(
                "daily-cleanup",
                svc => svc.PerformDailyCleanup(),
                "0 3 * * *",
                TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));

            // Weekly reconciliation report
            RecurringJob.AddOrUpdate<ITransactionService>(
                "weekly-reconciliation",
                svc => svc.RunWeeklyReconciliation(),
                "0 4 * * 1",
                TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));

            _log.Info("Hangfire recurring jobs scheduled successfully");
        }

        protected void Application_End()
        {
            _log.Info("MerchantHub Application Stopping...");
            _backgroundJobServer?.Dispose();
            _log.Info("MerchantHub Application Stopped");
        }

        protected void Application_Error()
        {
            var exception = Server.GetLastError();
            _log.Error("Unhandled application error", exception);

            // Clear the error for custom error page handling
            var httpException = exception as HttpException;
            if (httpException != null)
            {
                _log.ErrorFormat("HTTP Error {0}: {1}", httpException.GetHttpCode(), httpException.Message);
            }
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            // Track active sessions for monitoring
            var sessionCount = (int)(Application["ActiveSessionCount"] ?? 0);
            Application.Lock();
            Application["ActiveSessionCount"] = sessionCount + 1;
            Application.UnLock();

            if (sessionCount > 750)
            {
                _log.WarnFormat("High session count detected: {0} active sessions", sessionCount + 1);
            }
        }

        protected void Session_End(object sender, EventArgs e)
        {
            var sessionCount = (int)(Application["ActiveSessionCount"] ?? 0);
            Application.Lock();
            Application["ActiveSessionCount"] = Math.Max(0, sessionCount - 1);
            Application.UnLock();
        }
    }
}
