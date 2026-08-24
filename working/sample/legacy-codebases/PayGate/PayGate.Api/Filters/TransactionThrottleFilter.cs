using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace PayGate.Api.Filters
{
    /// <summary>
    /// Rate limiting filter that throttles requests per merchant API key.
    /// Uses in-memory sliding window (per IIS worker process - not shared across web farm).
    /// 
    /// WARNING: This does not work correctly in a multi-server farm scenario.
    /// Each IIS worker maintains its own counter, so actual rates can be
    /// multiplied by the number of active workers/servers.
    /// 
    /// TODO: Replace with distributed rate limiting (Redis-based) for proper
    /// multi-server enforcement.
    /// </summary>
    public class TransactionThrottleFilter : ActionFilterAttribute
    {
        private static readonly ConcurrentDictionary<string, ThrottleWindow> _throttleWindows =
            new ConcurrentDictionary<string, ThrottleWindow>();

        private static readonly int MaxRequestsPerMinute =
            int.Parse(ConfigurationManager.AppSettings["PayGate:MaxRequestsPerMinute"] ?? "1000");

        private static readonly int WindowMinutes =
            int.Parse(ConfigurationManager.AppSettings["PayGate:ThrottleWindowMinutes"] ?? "1");

        // Clean up old entries every 5 minutes
        private static readonly Timer _cleanupTimer = new Timer(CleanupOldEntries, null,
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var path = actionContext.Request.RequestUri.AbsolutePath.ToLowerInvariant();

            // Don't throttle health checks or admin endpoints
            if (path.StartsWith("/api/health") || path.StartsWith("/api/admin"))
            {
                base.OnActionExecuting(actionContext);
                return;
            }

            // Get merchant ID from request properties (set by ApiKeyAuthHandler)
            string merchantId = null;
            if (actionContext.Request.Properties.ContainsKey("PayGate_MerchantId"))
            {
                merchantId = actionContext.Request.Properties["PayGate_MerchantId"] as string;
            }

            if (string.IsNullOrEmpty(merchantId))
            {
                base.OnActionExecuting(actionContext);
                return;
            }

            var window = _throttleWindows.GetOrAdd(merchantId, _ => new ThrottleWindow());
            var now = DateTime.UtcNow;

            // Check if we need to reset the window
            if ((now - window.WindowStart).TotalMinutes >= WindowMinutes)
            {
                window.Reset(now);
            }

            // Increment counter
            var currentCount = Interlocked.Increment(ref window.RequestCount);

            if (currentCount > MaxRequestsPerMinute)
            {
                System.Diagnostics.EventLog.WriteEntry("PayGate",
                    $"Rate limit exceeded for merchant {merchantId}. Count: {currentCount}/{MaxRequestsPerMinute} per {WindowMinutes} minute(s)",
                    System.Diagnostics.EventLogEntryType.Warning);

                actionContext.Response = new HttpResponseMessage((HttpStatusCode)429)
                {
                    Content = new StringContent("{\"error\":\"Rate limit exceeded. Please retry after the current window expires.\"}"),
                    ReasonPhrase = "Too Many Requests"
                };
                actionContext.Response.Headers.Add("Retry-After", "60");
                actionContext.Response.Headers.Add("X-RateLimit-Limit", MaxRequestsPerMinute.ToString());
                actionContext.Response.Headers.Add("X-RateLimit-Remaining", "0");
                return;
            }

            // Add rate limit headers
            actionContext.Request.Properties["X-RateLimit-Limit"] = MaxRequestsPerMinute;
            actionContext.Request.Properties["X-RateLimit-Remaining"] = MaxRequestsPerMinute - currentCount;

            base.OnActionExecuting(actionContext);
        }

        private static void CleanupOldEntries(object state)
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-WindowMinutes * 2);
            var keysToRemove = _throttleWindows
                .Where(kvp => kvp.Value.WindowStart < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                ThrottleWindow removed;
                _throttleWindows.TryRemove(key, out removed);
            }
        }
    }

    internal class ThrottleWindow
    {
        public DateTime WindowStart;
        public int RequestCount;

        public ThrottleWindow()
        {
            WindowStart = DateTime.UtcNow;
            RequestCount = 0;
        }

        public void Reset(DateTime now)
        {
            WindowStart = now;
            Interlocked.Exchange(ref RequestCount, 0);
        }
    }
}
