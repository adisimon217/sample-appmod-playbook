using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using log4net;

namespace MerchantHub.Api.Filters
{
    /// <summary>
    /// API key authentication filter for merchant-facing API endpoints.
    /// Validates the X-Api-Key header against MH_ApiKeys table.
    /// 
    /// NOTE: This is a simple API key scheme. We should move to OAuth2/JWT
    ///       but this has been in place since 2019 and ~500 merchants use it.
    ///       Migration plan in JIRA-5503.
    /// </summary>
    public class ApiKeyAuthFilter : AuthorizationFilterAttribute
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ApiKeyAuthFilter));
        private const string ApiKeyHeader = "X-Api-Key";
        private const string ApiKeyQueryParam = "apikey"; // Legacy support

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            // Extract API key from header or query string (legacy)
            string apiKey = null;

            if (actionContext.Request.Headers.Contains(ApiKeyHeader))
            {
                apiKey = actionContext.Request.Headers.GetValues(ApiKeyHeader).FirstOrDefault();
            }
            else
            {
                // Legacy: some merchants pass key as query parameter
                var queryParams = actionContext.Request.GetQueryNameValuePairs();
                var keyParam = queryParams.FirstOrDefault(p => p.Key.Equals(ApiKeyQueryParam, StringComparison.OrdinalIgnoreCase));
                if (keyParam.Value != null)
                {
                    apiKey = keyParam.Value;
                    _log.WarnFormat("API key passed via query string from {0} - deprecated method",
                        GetClientIp(actionContext));
                }
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                actionContext.Response = actionContext.Request.CreateErrorResponse(
                    HttpStatusCode.Unauthorized, "API key is required. Provide via X-Api-Key header.");
                return;
            }

            // Validate API key against database
            var merchantId = ValidateApiKey(apiKey);
            if (merchantId == 0)
            {
                _log.WarnFormat("Invalid API key attempt from {0}: {1}...",
                    GetClientIp(actionContext), apiKey.Substring(0, Math.Min(7, apiKey.Length)));
                actionContext.Response = actionContext.Request.CreateErrorResponse(
                    HttpStatusCode.Unauthorized, "Invalid API key.");
                return;
            }

            // Store merchant ID for controllers to use
            actionContext.Request.Properties["MerchantId"] = merchantId;
            actionContext.Request.Properties["ApiKey"] = apiKey;

            // Update last used timestamp (fire and forget)
            try
            {
                UpdateLastUsed(apiKey);
            }
            catch (Exception ex)
            {
                _log.Warn("Failed to update API key last used timestamp", ex);
            }
        }

        private int ValidateApiKey(string apiKey)
        {
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["MerchantHubDB"].ConnectionString;
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(
                        @"SELECT m.MerchantId 
                          FROM MH_ApiKeys k
                          INNER JOIN MH_Merchants m ON k.MerchantId = m.MerchantId
                          WHERE k.ApiKey = @ApiKey AND k.IsActive = 1 AND m.Status = 'Active'", conn))
                    {
                        cmd.Parameters.AddWithValue("@ApiKey", apiKey);
                        var result = cmd.ExecuteScalar();
                        return result != null ? (int)result : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error validating API key", ex);
                return 0;
            }
        }

        private void UpdateLastUsed(string apiKey)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MerchantHubDB"].ConnectionString;
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "UPDATE MH_ApiKeys SET LastUsedDate = GETDATE(), UseCount = ISNULL(UseCount, 0) + 1 WHERE ApiKey = @ApiKey", conn))
                {
                    cmd.Parameters.AddWithValue("@ApiKey", apiKey);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private string GetClientIp(HttpActionContext context)
        {
            if (context.Request.Properties.ContainsKey("MS_HttpContext"))
            {
                dynamic httpContext = context.Request.Properties["MS_HttpContext"];
                return httpContext?.Request?.UserHostAddress ?? "unknown";
            }
            return "unknown";
        }
    }
}
