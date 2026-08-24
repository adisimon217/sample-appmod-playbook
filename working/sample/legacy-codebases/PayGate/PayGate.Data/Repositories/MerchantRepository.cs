using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using PayGate.Core.Models;

namespace PayGate.Data.Repositories
{
    /// <summary>
    /// Data access for merchant management.
    /// Uses a mix of EF6 (for simple CRUD) and raw ADO.NET (for complex queries).
    /// </summary>
    public class MerchantRepository
    {
        private readonly string _connectionString;

        public MerchantRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Get merchant by ID.
        /// </summary>
        public async Task<Merchant> GetByIdAsync(string merchantId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    SELECT * FROM dbo.Merchants WITH (NOLOCK)
                    WHERE MerchantId = @MerchantId", conn))
                {
                    cmd.Parameters.AddWithValue("@MerchantId", merchantId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Merchant
                            {
                                MerchantId = reader.GetString(reader.GetOrdinal("MerchantId")),
                                BusinessName = reader.GetString(reader.GetOrdinal("BusinessName")),
                                Status = (MerchantStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                MaxTransactionsPerHour = reader.GetInt32(reader.GetOrdinal("MaxTransactionsPerHour")),
                                SettlementSchedule = reader.IsDBNull(reader.GetOrdinal("SettlementSchedule"))
                                    ? null : reader.GetString(reader.GetOrdinal("SettlementSchedule")),
                                MccCode = reader.IsDBNull(reader.GetOrdinal("MccCode"))
                                    ? null : reader.GetString(reader.GetOrdinal("MccCode"))
                            };
                        }
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// Create a new merchant record.
        /// </summary>
        public async Task CreateAsync(Merchant merchant)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand("EXEC dbo.sp_MerchantOnboarding @MerchantId, @BusinessName, @TaxId, @ContactEmail, @ContactPhone, @MccCode, @SettlementSchedule, @MaxTransactionsPerHour", conn))
                {
                    cmd.Parameters.AddWithValue("@MerchantId", merchant.MerchantId);
                    cmd.Parameters.AddWithValue("@BusinessName", merchant.BusinessName);
                    cmd.Parameters.AddWithValue("@TaxId", (object)merchant.TaxId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ContactEmail", (object)merchant.ContactEmail ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ContactPhone", (object)merchant.ContactPhone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MccCode", (object)merchant.MccCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SettlementSchedule", (object)merchant.SettlementSchedule ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MaxTransactionsPerHour", merchant.MaxTransactionsPerHour);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Update merchant configuration.
        /// </summary>
        public async Task UpdateConfigurationAsync(
            string merchantId, int? maxTxnPerHour, string settlementSchedule,
            string webhookUrl, string[] allowedIps)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    UPDATE dbo.Merchants
                    SET MaxTransactionsPerHour = ISNULL(@MaxTxnPerHour, MaxTransactionsPerHour),
                        SettlementSchedule = ISNULL(@SettlementSchedule, SettlementSchedule),
                        WebhookUrl = ISNULL(@WebhookUrl, WebhookUrl),
                        AllowedIpAddresses = @AllowedIps
                    WHERE MerchantId = @MerchantId", conn))
                {
                    cmd.Parameters.AddWithValue("@MerchantId", merchantId);
                    cmd.Parameters.AddWithValue("@MaxTxnPerHour", (object)maxTxnPerHour ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SettlementSchedule", (object)settlementSchedule ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@WebhookUrl", (object)webhookUrl ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AllowedIps",
                        allowedIps != null ? string.Join(",", allowedIps) : (object)DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Suspend a merchant account.
        /// </summary>
        public async Task SuspendAsync(string merchantId, string reason, string suspendedBy)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    EXEC dbo.sp_UpdateMerchantStatus 
                        @MerchantId, @Status, @Reason, @UpdatedBy", conn))
                {
                    cmd.Parameters.AddWithValue("@MerchantId", merchantId);
                    cmd.Parameters.AddWithValue("@Status", (int)MerchantStatus.Suspended);
                    cmd.Parameters.AddWithValue("@Reason", (object)reason ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UpdatedBy", suspendedBy);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Get merchant transaction volume statistics.
        /// </summary>
        public async Task<MerchantVolume> GetTransactionVolumeAsync(
            string merchantId, DateTime startDate, DateTime endDate)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    SELECT 
                        @MerchantId AS MerchantId,
                        COUNT(*) AS TotalTransactions,
                        ISNULL(SUM(Amount), 0) AS TotalAmount,
                        SUM(CASE WHEN Status IN (1,2,7) THEN 1 ELSE 0 END) AS SuccessfulTransactions,
                        SUM(CASE WHEN Status IN (3,4) THEN 1 ELSE 0 END) AS FailedTransactions,
                        ISNULL(AVG(Amount), 0) AS AverageTransactionAmount,
                        SUM(CASE WHEN Status = 8 THEN 1 ELSE 0 END) AS ChargebackCount,
                        ISNULL(SUM(CASE WHEN Status = 8 THEN ABS(Amount) ELSE 0 END), 0) AS ChargebackAmount
                    FROM dbo.Transactions WITH (NOLOCK)
                    WHERE MerchantId = @MerchantId
                      AND CreatedDate >= @StartDate
                      AND CreatedDate <= @EndDate", conn))
                {
                    cmd.Parameters.AddWithValue("@MerchantId", merchantId);
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);
                    cmd.CommandTimeout = 30;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new MerchantVolume
                            {
                                MerchantId = merchantId,
                                StartDate = startDate,
                                EndDate = endDate,
                                TotalTransactions = (int)reader["TotalTransactions"],
                                TotalAmount = (decimal)reader["TotalAmount"],
                                SuccessfulTransactions = (int)reader["SuccessfulTransactions"],
                                FailedTransactions = (int)reader["FailedTransactions"],
                                AverageTransactionAmount = (decimal)reader["AverageTransactionAmount"],
                                ChargebackCount = (int)reader["ChargebackCount"],
                                ChargebackAmount = (decimal)reader["ChargebackAmount"]
                            };
                        }
                    }
                }
            }

            return new MerchantVolume { MerchantId = merchantId, StartDate = startDate, EndDate = endDate };
        }
    }
}
