using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PayGate.Security;

namespace PayGate.Api.Filters
{
    /// <summary>
    /// DelegatingHandler that validates API key authentication for merchant-facing endpoints.
    /// Admin endpoints (/api/admin) bypass this handler and use Windows Auth instead.
    /// Health check endpoints (/api/health) bypass authentication entirely.
    /// </summary>
    public class ApiKeyAuthHandler : DelegatingHandler
    {
        private static readonly string ApiKeyHeaderName =
            ConfigurationManager.AppSettings["PayGate:ApiKeyHeaderName"] ?? "X-PayGate-ApiKey";

        private readonly ApiKeyValidator _validator;

        public ApiKeyAuthHandler()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["PaymentsDB"].ConnectionString;
            _validator = new ApiKeyValidator(connectionString);
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri.AbsolutePath.ToLowerInvariant();

            // Skip authentication for health endpoints
            if (path.StartsWith("/api/health"))
            {
                return await base.SendAsync(request, cancellationToken);
            }

            // Skip API key auth for admin endpoints (Windows Auth handles these)
            if (path.StartsWith("/api/admin"))
            {
                return await base.SendAsync(request, cancellationToken);
            }

            // Extract API key from header
            string apiKey = null;
            if (request.Headers.Contains(ApiKeyHeaderName))
            {
                apiKey = request.Headers.GetValues(ApiKeyHeaderName).FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return CreateUnauthorizedResponse(request, "API key is required. Include header: " + ApiKeyHeaderName);
            }

            // Validate the API key
            var validationResult = await _validator.ValidateKeyAsync(apiKey);

            if (!validationResult.IsValid)
            {
                System.Diagnostics.EventLog.WriteEntry("PayGate",
                    $"Invalid API key attempt from {GetClientIp(request)}. Key prefix: {apiKey.Substring(0, Math.Min(8, apiKey.Length))}...",
                    System.Diagnostics.EventLogEntryType.Warning);

                return CreateUnauthorizedResponse(request, "Invalid or expired API key.");
            }

            // Check if the API key's merchant is active
            if (!validationResult.MerchantActive)
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content = new StringContent("{\"error\":\"Merchant account is suspended.\"}")
                };
            }

            // Check IP whitelist if configured
            var clientIp = GetClientIp(request);
            if (validationResult.AllowedIps != null && validationResult.AllowedIps.Any())
            {
                if (!validationResult.AllowedIps.Contains(clientIp))
                {
                    System.Diagnostics.EventLog.WriteEntry("PayGate",
                        $"IP not whitelisted for merchant {validationResult.MerchantId}. IP: {clientIp}",
                        System.Diagnostics.EventLogEntryType.Warning);

                    return new HttpResponseMessage(HttpStatusCode.Forbidden)
                    {
                        Content = new StringContent("{\"error\":\"Request IP not in allowed list.\"}")
                    };
                }
            }

            // Attach merchant context to request for downstream use
            request.Properties["PayGate_MerchantId"] = validationResult.MerchantId;
            request.Properties["PayGate_ApiKeyId"] = validationResult.ApiKeyId;

            return await base.SendAsync(request, cancellationToken);
        }

        private HttpResponseMessage CreateUnauthorizedResponse(HttpRequestMessage request, string message)
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent($"{{\"error\":\"{message}\"}}")
            };
        }

        private string GetClientIp(HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                var ctx = request.Properties["MS_HttpContext"] as System.Web.HttpContextWrapper;
                // Check for proxy/load balancer forwarded IP
                var forwardedFor = ctx?.Request?.Headers["X-Forwarded-For"];
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    return forwardedFor.Split(',').First().Trim();
                }
                return ctx?.Request?.UserHostAddress ?? "0.0.0.0";
            }
            return "0.0.0.0";
        }
    }
}
