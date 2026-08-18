using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PayGate.Security
{
    /// <summary>
    /// Validates merchant API keys against the database.
    /// Implements a local cache to avoid DB hit on every request.
    /// Cache TTL is 5 minutes - revoked keys may take up to 5 min to take effect.
    /// </summary>
    public class ApiKeyValidator
    {
        private readonly string _connectionString;

        // In-memory cache of validated keys (key hash -> validation result)
        // WARNING: This cache is per-AppDomain. In a web farm, revocation
        // takes up to TTL minutes to propagate to all servers.
        private static readonly ConcurrentDictionary<string, CachedValidation> _cache =
            new ConcurrentDictionary<string, CachedValidation>();

        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        public ApiKeyValidator(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Validate an API key and return associated merchant information.
        /// </summary>
        public async Task<ApiKeyValidationResult> ValidateKeyAsync(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new ApiKeyValidationResult { IsValid = false };
            }

            // Hash the key for cache lookup (don't cache plain-text keys in memory)
            var keyHash = HashKey(apiKey);

            // Check cache first
            if (_cache.TryGetValue(keyHash, out var cached))
            {
                if (DateTime.UtcNow - cached.CachedAt < CacheTtl)
                {
                    return cached.Result;
                }
                else
                {
                    // Expired - remove from cache
                    CachedValidation removed;
                    _cache.TryRemove(keyHash, out removed);
                }
            }

            // Validate against database
            var result = await ValidateFromDatabaseAsync(apiKey);

            // Cache the result
            _cache.TryAdd(keyHash, new CachedValidation
            {
                Result = result,
                CachedAt = DateTime.UtcNow
            });

            return result;
        }

        private async Task<ApiKeyValidationResult> ValidateFromDatabaseAsync(string apiKey)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (var cmd = new SqlCommand(@"
                    EXEC dbo.sp_ValidateApiKey @ApiKey", conn))
                {
                    cmd.Parameters.AddWithValue("@ApiKey", apiKey);
                    cmd.CommandTimeout = 5;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var isValid = reader.GetBoolean(reader.GetOrdinal("IsValid"));
                            if (!isValid)
                            {
                                return new ApiKeyValidationResult { IsValid = false };
                            }

                            var allowedIpsRaw = reader.IsDBNull(reader.GetOrdinal("AllowedIpAddresses"))
                                ? null : reader.GetString(reader.GetOrdinal("AllowedIpAddresses"));

                            return new ApiKeyValidationResult
                            {
                                IsValid = true,
                                ApiKeyId = reader.GetGuid(reader.GetOrdinal("ApiKeyId")),
                                MerchantId = reader.GetString(reader.GetOrdinal("MerchantId")),
                                MerchantActive = reader.GetBoolean(reader.GetOrdinal("MerchantActive")),
                                AllowedIps = string.IsNullOrEmpty(allowedIpsRaw)
                                    ? null
                                    : allowedIpsRaw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(ip => ip.Trim())
                                        .ToList()
                            };
                        }
                    }
                }
            }

            return new ApiKeyValidationResult { IsValid = false };
        }

        private string HashKey(string key)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
                return Convert.ToBase64String(hashBytes);
            }
        }

        /// <summary>
        /// Clear the validation cache (used when an admin revokes keys).
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
        }
    }

    public class ApiKeyValidationResult
    {
        public bool IsValid { get; set; }
        public Guid ApiKeyId { get; set; }
        public string MerchantId { get; set; }
        public bool MerchantActive { get; set; }
        public List<string> AllowedIps { get; set; }
    }

    internal class CachedValidation
    {
        public ApiKeyValidationResult Result { get; set; }
        public DateTime CachedAt { get; set; }
    }
}
