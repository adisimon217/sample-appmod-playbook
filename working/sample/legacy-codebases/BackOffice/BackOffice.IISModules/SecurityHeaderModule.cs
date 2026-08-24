using System;
using System.Web;
using log4net;

namespace BackOffice.IISModules
{
    /// <summary>
    /// Custom IIS HTTP Module that adds security headers to all responses.
    /// Adds X-Frame-Options, X-Content-Type-Options, X-XSS-Protection, 
    /// Content-Security-Policy, and Strict-Transport-Security headers.
    /// Configuration read from Windows Registry.
    /// </summary>
    public class SecurityHeaderModule : IHttpModule
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SecurityHeaderModule));
        private static bool _isEnabled;
        private static string _frameOptions;
        private static string _contentSecurityPolicy;
        private static bool _enforceHsts;
        private static int _hstsMaxAge;

        public void Init(HttpApplication context)
        {
            // Read configuration from registry
            var config = RegistryConfigReader.GetModuleConfig("SecurityHeaderModule");
            _isEnabled = config.IsEnabled;

            // Additional security-specific config from registry
            var secConfig = RegistryConfigReader.GetSecurityConfig();
            _frameOptions = secConfig.FrameOptions ?? "DENY";
            _contentSecurityPolicy = secConfig.ContentSecurityPolicy ?? "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'";
            _enforceHsts = secConfig.EnforceHsts;
            _hstsMaxAge = secConfig.HstsMaxAge > 0 ? secConfig.HstsMaxAge : 31536000;

            context.PreSendRequestHeaders += OnPreSendRequestHeaders;

            _log.InfoFormat("SecurityHeaderModule initialized. Enabled: {0}, HSTS: {1}", _isEnabled, _enforceHsts);
        }

        private void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            if (!_isEnabled) return;

            HttpApplication app = (HttpApplication)sender;
            HttpResponse response = app.Context.Response;

            try
            {
                // Prevent clickjacking
                response.Headers.Set("X-Frame-Options", _frameOptions);

                // Prevent MIME-type sniffing
                response.Headers.Set("X-Content-Type-Options", "nosniff");

                // Enable XSS filter
                response.Headers.Set("X-XSS-Protection", "1; mode=block");

                // Content Security Policy
                response.Headers.Set("Content-Security-Policy", _contentSecurityPolicy);

                // Remove server identification headers
                response.Headers.Remove("Server");
                response.Headers.Remove("X-Powered-By");
                response.Headers.Remove("X-AspNet-Version");

                // HSTS for HTTPS connections
                if (_enforceHsts && app.Context.Request.IsSecureConnection)
                {
                    response.Headers.Set("Strict-Transport-Security",
                        string.Format("max-age={0}; includeSubDomains", _hstsMaxAge));
                }

                // Referrer policy
                response.Headers.Set("Referrer-Policy", "strict-origin-when-cross-origin");

                // Permissions policy (legacy Feature-Policy replacement)
                response.Headers.Set("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
            }
            catch (Exception ex)
            {
                _log.Error("Error adding security headers", ex);
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
