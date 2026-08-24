using System;
using System.Net;
using System.Web;
using System.Web.Http;

namespace PayGate.Api
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            // Force TLS 1.2 for all outbound connections (PCI DSS requirement)
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.DefaultConnectionLimit = 200;

            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalConfiguration.Configuration);

            // Initialize connection pool warming
            WarmUpConnectionPool();

            // Log application startup
            System.Diagnostics.EventLog.WriteEntry(
                "PayGate",
                "PayGate API started successfully. TLS 1.2 enforced. Connection pool warmed.",
                System.Diagnostics.EventLogEntryType.Information);
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var exception = Server.GetLastError();
            System.Diagnostics.EventLog.WriteEntry(
                "PayGate",
                $"Unhandled exception: {exception?.Message}\n{exception?.StackTrace}",
                System.Diagnostics.EventLogEntryType.Error);
        }

        protected void Application_End()
        {
            System.Diagnostics.EventLog.WriteEntry(
                "PayGate",
                "PayGate API shutting down.",
                System.Diagnostics.EventLogEntryType.Warning);
        }

        /// <summary>
        /// Pre-warm the SQL connection pool to avoid cold-start latency
        /// during peak transaction processing hours.
        /// </summary>
        private void WarmUpConnectionPool()
        {
            try
            {
                var connString = System.Configuration.ConfigurationManager
                    .ConnectionStrings["PaymentsDB"]?.ConnectionString;

                if (string.IsNullOrEmpty(connString)) return;

                // Open and immediately close 20 connections to seed the pool
                for (int i = 0; i < 20; i++)
                {
                    using (var conn = new System.Data.SqlClient.SqlConnection(connString))
                    {
                        conn.Open();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.EventLog.WriteEntry(
                    "PayGate",
                    $"Connection pool warm-up failed: {ex.Message}",
                    System.Diagnostics.EventLogEntryType.Warning);
            }
        }
    }
}
