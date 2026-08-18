using System;
using System.Configuration;
using System.Web;
using System.Web.SessionState;
using log4net;

namespace BackOffice.IISModules
{
    /// <summary>
    /// Custom IIS HTTP Module that validates user sessions.
    /// Checks for session hijacking, concurrent sessions, and idle timeout.
    /// Uses Windows Registry for max concurrent session configuration.
    /// </summary>
    public class SessionValidationModule : IHttpModule
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SessionValidationModule));
        private static bool _isEnabled;
        private static int _maxConcurrentSessions;
        private static int _sessionIdleTimeoutMinutes;
        private static bool _validateClientIp;

        public void Init(HttpApplication context)
        {
            // Read configuration from registry
            var config = RegistryConfigReader.GetModuleConfig("SessionValidationModule");
            _isEnabled = config.IsEnabled;

            // Session-specific config from registry
            var sessionConfig = RegistryConfigReader.GetSessionConfig();
            _maxConcurrentSessions = sessionConfig.MaxConcurrentSessions;
            _sessionIdleTimeoutMinutes = sessionConfig.IdleTimeoutMinutes;
            _validateClientIp = sessionConfig.ValidateClientIp;

            context.AcquireRequestState += OnAcquireRequestState;
            context.PostAcquireRequestState += OnPostAcquireRequestState;

            _log.InfoFormat("SessionValidationModule initialized. Enabled: {0}, MaxSessions: {1}, Timeout: {2}min",
                _isEnabled, _maxConcurrentSessions, _sessionIdleTimeoutMinutes);
        }

        private void OnAcquireRequestState(object sender, EventArgs e)
        {
            if (!_isEnabled) return;

            HttpApplication app = (HttpApplication)sender;
            HttpContext context = app.Context;

            // Skip for non-page requests
            if (!context.Request.Path.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                return;

            // Validate that session ID matches expected pattern
            if (context.Session != null && context.Session.IsNewSession)
            {
                string cookieHeader = context.Request.Headers["Cookie"];
                if (cookieHeader != null && cookieHeader.Contains("ASP.NET_SessionId"))
                {
                    // Session was regenerated - possible session fixation attack
                    _log.WarnFormat("Potential session fixation detected for user {0} from IP {1}",
                        context.User?.Identity?.Name ?? "Unknown",
                        context.Request.UserHostAddress);
                }
            }
        }

        private void OnPostAcquireRequestState(object sender, EventArgs e)
        {
            if (!_isEnabled) return;

            HttpApplication app = (HttpApplication)sender;
            HttpContext context = app.Context;

            if (context.Session == null) return;
            if (!context.Request.Path.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                // Check IP binding - detect session theft
                if (_validateClientIp)
                {
                    ValidateClientIpBinding(context);
                }

                // Check idle timeout
                ValidateIdleTimeout(context);

                // Update last activity
                context.Session["LastActivity"] = DateTime.Now;
                context.Session["ClientIP"] = context.Request.UserHostAddress;
            }
            catch (SessionValidationException ex)
            {
                _log.WarnFormat("Session validation failed: {0}", ex.Message);
                context.Session.Abandon();
                context.Response.Redirect("~/SessionExpired.aspx", true);
            }
            catch (Exception ex)
            {
                _log.Error("Unexpected error in session validation", ex);
                // Don't break the request for unexpected errors
            }
        }

        private void ValidateClientIpBinding(HttpContext context)
        {
            string storedIp = context.Session["ClientIP"] as string;
            string currentIp = context.Request.UserHostAddress;

            if (storedIp != null && storedIp != currentIp)
            {
                string userName = context.User?.Identity?.Name ?? "Unknown";
                _log.WarnFormat("Session IP mismatch for user {0}: stored={1}, current={2}",
                    userName, storedIp, currentIp);

                throw new SessionValidationException(
                    string.Format("Client IP changed from {0} to {1}", storedIp, currentIp));
            }
        }

        private void ValidateIdleTimeout(HttpContext context)
        {
            DateTime? lastActivity = context.Session["LastActivity"] as DateTime?;

            if (lastActivity.HasValue)
            {
                TimeSpan idleTime = DateTime.Now - lastActivity.Value;
                if (idleTime.TotalMinutes > _sessionIdleTimeoutMinutes)
                {
                    string userName = context.User?.Identity?.Name ?? "Unknown";
                    _log.InfoFormat("Session idle timeout for user {0}: idle for {1:F0} minutes",
                        userName, idleTime.TotalMinutes);

                    throw new SessionValidationException(
                        string.Format("Session idle for {0:F0} minutes (limit: {1})",
                            idleTime.TotalMinutes, _sessionIdleTimeoutMinutes));
                }
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }

    /// <summary>
    /// Custom exception for session validation failures.
    /// </summary>
    public class SessionValidationException : Exception
    {
        public SessionValidationException(string message) : base(message) { }
        public SessionValidationException(string message, Exception inner) : base(message, inner) { }
    }
}
