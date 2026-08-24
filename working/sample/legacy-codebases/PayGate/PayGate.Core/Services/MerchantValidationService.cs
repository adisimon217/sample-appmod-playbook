using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using PayGate.Core.Models;

namespace PayGate.Core.Services
{
    /// <summary>
    /// Validates merchant status and configuration before processing transactions.
    /// Uses direct ADO.NET for performance (bypasses EF overhead on hot path).
    /// </summary>
    public class MerchantValidationService
    {
        private readonly string _connectionString;

        public MerchantValidationService(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Validate that a merchant is active and allowed to process transactions.
        /// This is called on every transaction - must be fast.
        /// </summary>
        public async Task<MerchantValidationResult> ValidateMerchantAsync(string merchantId)
        {
            if (string.IsNullOrWhiteSpace(merchantId))
            {
                return new MerchantValidationResult
                {
                    IsValid = false,
                    Reason = "Merchant ID is required."
                };
            }

            // Direct SQL for performance - this is in the critical transaction path
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (var cmd = new SqlCommand(@"
                    SELECT TOP 1 
                        m.MerchantId, 
                        m.Status, 
                        m.MaxTransactionsPerHour,
                        m.SuspensionReason,
                        (SELECT COUNT(*) FROM dbo.Transactions t WITH (NOLOCK)
                         WHERE t.MerchantId = m.MerchantId 
                         AND t.CreatedDate >= DATEADD(HOUR, -1, GETUTCDATE())) AS HourlyVolume
                    FROM dbo.Merchants m WITH (NOLOCK)
                    WHERE m.MerchantId = @MerchantId", conn))
                {
                    cmd.Parameters.AddWithValue("@MerchantId", merchantId);
                    cmd.CommandTimeout = 5; // 5 second timeout for validation queries

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (!reader.Read())
                        {
                            return new MerchantValidationResult
                            {
                                IsValid = false,
                                Reason = "Merchant not found.",
                                MerchantId = merchantId
                            };
                        }

                        var status = (MerchantStatus)reader.GetInt32(reader.GetOrdinal("Status"));
                        var maxTxnPerHour = reader.GetInt32(reader.GetOrdinal("MaxTransactionsPerHour"));
                        var hourlyVolume = reader.GetInt32(reader.GetOrdinal("HourlyVolume"));

                        if (status != MerchantStatus.Active)
                        {
                            var reason = status == MerchantStatus.Suspended
                                ? $"Merchant suspended: {reader["SuspensionReason"]}"
                                : $"Merchant status is {status}.";

                            return new MerchantValidationResult
                            {
                                IsValid = false,
                                Reason = reason,
                                MerchantId = merchantId,
                                Status = status
                            };
                        }

                        // Check hourly volume limit
                        if (hourlyVolume >= maxTxnPerHour)
                        {
                            return new MerchantValidationResult
                            {
                                IsValid = false,
                                Reason = $"Hourly transaction limit exceeded ({hourlyVolume}/{maxTxnPerHour}).",
                                MerchantId = merchantId,
                                Status = status
                            };
                        }

                        return new MerchantValidationResult
                        {
                            IsValid = true,
                            MerchantId = merchantId,
                            Status = status
                        };
                    }
                }
            }
        }
    }
}
