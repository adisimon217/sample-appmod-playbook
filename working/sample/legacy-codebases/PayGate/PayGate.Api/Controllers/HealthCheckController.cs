using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;

namespace PayGate.Api.Controllers
{
    /// <summary>
    /// Health check endpoints for load balancer and monitoring.
    /// Exempt from API key authentication.
    /// </summary>
    [RoutePrefix("api/health")]
    [AllowAnonymous]
    public class HealthCheckController : ApiController
    {
        /// <summary>
        /// Simple liveness check - returns 200 if application is running.
        /// </summary>
        [HttpGet]
        [Route("")]
        public IHttpActionResult Ping()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = typeof(HealthCheckController).Assembly.GetName().Version.ToString(),
                MachineName = Environment.MachineName
            });
        }

        /// <summary>
        /// Deep health check - validates database connectivity and response times.
        /// </summary>
        [HttpGet]
        [Route("deep")]
        public async Task<IHttpActionResult> DeepHealthCheck()
        {
            var checks = new Dictionary<string, HealthCheckResult>();
            var overallHealthy = true;

            // Check primary database
            checks["PaymentsDB_Primary"] = await CheckDatabaseAsync("PaymentsDB");
            if (!checks["PaymentsDB_Primary"].IsHealthy) overallHealthy = false;

            // Check read-only replica
            checks["PaymentsDB_ReadOnly"] = await CheckDatabaseAsync("PaymentsDB_ReadOnly");
            if (!checks["PaymentsDB_ReadOnly"].IsHealthy) overallHealthy = false;

            // Check connection pool status
            checks["ConnectionPool"] = CheckConnectionPool();
            if (!checks["ConnectionPool"].IsHealthy) overallHealthy = false;

            var statusCode = overallHealthy ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;

            return Content(statusCode, new
            {
                Status = overallHealthy ? "Healthy" : "Degraded",
                Timestamp = DateTime.UtcNow,
                MachineName = Environment.MachineName,
                Checks = checks
            });
        }

        private async Task<HealthCheckResult> CheckDatabaseAsync(string connectionName)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var connStr = ConfigurationManager.ConnectionStrings[connectionName]?.ConnectionString;
                if (string.IsNullOrEmpty(connStr))
                {
                    return new HealthCheckResult
                    {
                        IsHealthy = false,
                        ResponseTimeMs = 0,
                        Error = "Connection string not configured"
                    };
                }

                using (var conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    using (var cmd = new SqlCommand("SELECT 1", conn))
                    {
                        await cmd.ExecuteScalarAsync();
                    }
                }

                sw.Stop();
                return new HealthCheckResult
                {
                    IsHealthy = true,
                    ResponseTimeMs = sw.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    ResponseTimeMs = sw.ElapsedMilliseconds,
                    Error = ex.Message
                };
            }
        }

        private HealthCheckResult CheckConnectionPool()
        {
            try
            {
                // Check if connection pool is not exhausted by attempting a quick connection
                var connStr = ConfigurationManager.ConnectionStrings["PaymentsDB"]?.ConnectionString;
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    return new HealthCheckResult { IsHealthy = true, ResponseTimeMs = 0 };
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("pool"))
            {
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    Error = "Connection pool exhausted"
                };
            }
            catch (Exception ex)
            {
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    Error = ex.Message
                };
            }
        }
    }

    public class HealthCheckResult
    {
        public bool IsHealthy { get; set; }
        public long ResponseTimeMs { get; set; }
        public string Error { get; set; }
    }
}
