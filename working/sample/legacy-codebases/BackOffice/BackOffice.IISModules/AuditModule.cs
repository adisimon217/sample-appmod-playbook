using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Web;
using log4net;

namespace BackOffice.IISModules
{
    /// <summary>
    /// Custom IIS HTTP Module that logs all requests to the BackOffice application.
    /// Records user identity, URL, timestamp, and response time for audit compliance.
    /// Registered in web.config under system.web/httpModules and system.webServer/modules.
    /// </summary>
    public class AuditModule : IHttpModule
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(AuditModule));
        private static readonly ILog _auditLog = LogManager.GetLogger("BackOffice.Audit");
        private static string _connectionString;
        private static bool _isEnabled;
        private static string _auditLevel;

        public void Init(HttpApplication context)
        {
            // Read configuration from registry
            var config = RegistryConfigReader.GetModuleConfig("AuditModule");
            _isEnabled = config.IsEnabled;
            _auditLevel = config.AuditLevel ?? "Standard";
            _connectionString = ConfigurationManager.ConnectionStrings["BackOfficeDB"]?.ConnectionString;

            context.BeginRequest += OnBeginRequest;
            context.EndRequest += OnEndRequest;
            context.AuthenticateRequest += OnAuthenticateRequest;

            _log.InfoFormat("AuditModule initialized. Enabled: {0}, Level: {1}", _isEnabled, _auditLevel);
        }

        private void OnBeginRequest(object sender, EventArgs e)
        {
            if (!_isEnabled) return;

            HttpApplication app = (HttpApplication)sender;
            HttpContext context = app.Context;

            // Store request start time for duration calculation
            context.Items["AuditModule_StartTime"] = Stopwatch.StartNew();
        }

        private void OnAuthenticateRequest(object sender, EventArgs e)
        {
            if (!_isEnabled) return;

            HttpApplication app = (HttpApplication)sender;
            HttpContext context = app.Context;

            if (context.User != null && context.User.Identity.IsAuthenticated)
            {
                context.Items["AuditModule_UserName"] = context.User.Identity.Name;
            }
        }

        private void OnEndRequest(object sender, EventArgs e)
        {
            if (!_isEnabled) return;

            HttpApplication app = (HttpApplication)sender;
            HttpContext context = app.Context;

            try
            {
                string requestUrl = context.Request.Url.AbsolutePath;

                // Skip static resources unless in Verbose mode
                if (_auditLevel != "Verbose" && IsStaticResource(requestUrl))
                    return;

                string userName = context.Items["AuditModule_UserName"] as string ?? "Anonymous";
                Stopwatch stopwatch = context.Items["AuditModule_StartTime"] as Stopwatch;
                long elapsedMs = stopwatch?.ElapsedMilliseconds ?? 0;

                int statusCode = context.Response.StatusCode;
                string method = context.Request.HttpMethod;
                string clientIp = context.Request.UserHostAddress;
                string userAgent = context.Request.UserAgent;

                // Log to file
                _auditLog.InfoFormat("{0}|{1}|{2}|{3}|{4}|{5}ms|{6}",
                    userName, method, requestUrl, statusCode, clientIp, elapsedMs, userAgent);

                // Log to database for detailed audit trail
                if (_auditLevel == "Detailed" || _auditLevel == "Verbose")
                {
                    LogToDatabase(userName, method, requestUrl, statusCode, clientIp, elapsedMs);
                }

                // Alert on slow requests
                if (elapsedMs > 5000)
                {
                    _log.WarnFormat("Slow request detected: {0} {1} took {2}ms for user {3}",
                        method, requestUrl, elapsedMs, userName);
                }

                // Alert on errors
                if (statusCode >= 500)
                {
                    _log.ErrorFormat("Server error: {0} {1} returned {2} for user {3}",
                        method, requestUrl, statusCode, userName);
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error in AuditModule.OnEndRequest", ex);
                // Module errors should not break the application
            }
        }

        private void LogToDatabase(string userName, string method, string url, int statusCode, string clientIp, long elapsedMs)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            try
            {
                // Non-parameterized insert - legacy pattern
                string sql = string.Format(
                    "INSERT INTO AuditLog (UserName, HttpMethod, RequestUrl, StatusCode, ClientIp, ElapsedMs, RequestTime) " +
                    "VALUES ('{0}', '{1}', '{2}', {3}, '{4}', {5}, GETDATE())",
                    userName.Replace("'", "''"), method, url.Replace("'", "''"),
                    statusCode, clientIp, elapsedMs);

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Failed to write audit record to database", ex);
            }
        }

        private bool IsStaticResource(string url)
        {
            return url.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
                   url.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
                   url.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                   url.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
                   url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                   url.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
                   url.EndsWith(".woff", StringComparison.OrdinalIgnoreCase) ||
                   url.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase) ||
                   url.Contains("Telerik.Web.UI.WebResource.axd");
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
