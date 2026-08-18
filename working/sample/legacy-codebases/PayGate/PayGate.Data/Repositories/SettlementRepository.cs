using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using PayGate.Core.Models;

namespace PayGate.Data.Repositories
{
    /// <summary>
    /// Data access for settlement records and reconciliation.
    /// </summary>
    public class SettlementRepository
    {
        private readonly string _connectionString;

        public SettlementRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Get unsettled (captured but not yet settled) transactions by card network.
        /// </summary>
        public async Task<List<Transaction>> GetUnsettledTransactionsByNetworkAsync(
            string cardBrand, DateTime settlementDate)
        {
            var transactions = new List<Transaction>();

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    SELECT t.*
                    FROM dbo.Transactions t WITH (NOLOCK)
                    WHERE t.CardBrand = @CardBrand
                      AND t.Status = @CapturedStatus
                      AND CAST(t.ProcessedDate AS DATE) = @SettlementDate
                      AND t.TransactionType IN (1, 2, 3) -- Auth, Capture, Sale
                    ORDER BY t.ProcessedDate ASC", conn))
                {
                    cmd.Parameters.AddWithValue("@CardBrand", cardBrand);
                    cmd.Parameters.AddWithValue("@CapturedStatus", (int)TransactionStatus.Captured);
                    cmd.Parameters.AddWithValue("@SettlementDate", settlementDate.Date);
                    cmd.CommandTimeout = 60;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            transactions.Add(new Transaction
                            {
                                TransactionId = reader.GetGuid(reader.GetOrdinal("TransactionId")),
                                MerchantId = reader.GetString(reader.GetOrdinal("MerchantId")),
                                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                                Currency = reader.GetString(reader.GetOrdinal("Currency")),
                                CardBrand = reader.GetString(reader.GetOrdinal("CardBrand")),
                                AuthorizationCode = reader.IsDBNull(reader.GetOrdinal("AuthorizationCode"))
                                    ? null : reader.GetString(reader.GetOrdinal("AuthorizationCode")),
                                ProcessedDate = reader.IsDBNull(reader.GetOrdinal("ProcessedDate"))
                                    ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("ProcessedDate"))
                            });
                        }
                    }
                }
            }

            return transactions;
        }

        /// <summary>
        /// Create a settlement record.
        /// </summary>
        public async Task CreateSettlementAsync(Settlement settlement)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    INSERT INTO dbo.Settlements (
                        SettlementId, BatchId, Network, SettlementDate, TotalAmount,
                        TransactionCount, Status, FileName, FileGeneratedDate, FileSentDate
                    ) VALUES (
                        @SettlementId, @BatchId, @Network, @SettlementDate, @TotalAmount,
                        @TransactionCount, @Status, @FileName, @FileGeneratedDate, @FileSentDate
                    )", conn))
                {
                    cmd.Parameters.AddWithValue("@SettlementId", settlement.SettlementId);
                    cmd.Parameters.AddWithValue("@BatchId", settlement.BatchId);
                    cmd.Parameters.AddWithValue("@Network", settlement.Network);
                    cmd.Parameters.AddWithValue("@SettlementDate", settlement.SettlementDate);
                    cmd.Parameters.AddWithValue("@TotalAmount", settlement.TotalAmount);
                    cmd.Parameters.AddWithValue("@TransactionCount", settlement.TransactionCount);
                    cmd.Parameters.AddWithValue("@Status", (int)settlement.Status);
                    cmd.Parameters.AddWithValue("@FileName", (object)settlement.FileName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FileGeneratedDate", (object)settlement.FileGeneratedDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FileSentDate", (object)settlement.FileSentDate ?? DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Get settlement records by date.
        /// </summary>
        public async Task<List<Settlement>> GetByDateAsync(DateTime settlementDate, string network = null)
        {
            var settlements = new List<Settlement>();

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var sql = @"
                    SELECT * FROM dbo.Settlements WITH (NOLOCK)
                    WHERE CAST(SettlementDate AS DATE) = @SettlementDate";

                if (!string.IsNullOrEmpty(network))
                    sql += " AND Network = @Network";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@SettlementDate", settlementDate.Date);
                    if (!string.IsNullOrEmpty(network))
                        cmd.Parameters.AddWithValue("@Network", network);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            settlements.Add(MapSettlement(reader));
                        }
                    }
                }
            }

            return settlements;
        }

        /// <summary>
        /// Get a specific settlement batch.
        /// </summary>
        public async Task<Settlement> GetBatchAsync(Guid batchId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(
                    "SELECT * FROM dbo.Settlements WITH (NOLOCK) WHERE BatchId = @BatchId", conn))
                {
                    cmd.Parameters.AddWithValue("@BatchId", batchId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                            return MapSettlement(reader);
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// Reconcile a settlement batch against response data from card network.
        /// </summary>
        public async Task<ReconciliationResult> ReconcileSettlementAsync(
            string network, DateTime settlementDate, List<Guid> matchedTransactionIds)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand("EXEC dbo.sp_ReconcileSettlement @Network, @SettlementDate, @MatchedCount", conn))
                {
                    cmd.Parameters.AddWithValue("@Network", network);
                    cmd.Parameters.AddWithValue("@SettlementDate", settlementDate);
                    cmd.Parameters.AddWithValue("@MatchedCount", matchedTransactionIds?.Count ?? 0);
                    cmd.CommandTimeout = 120;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new ReconciliationResult
                            {
                                Success = true,
                                MatchedCount = (int)reader["MatchedCount"],
                                UnmatchedCount = (int)reader["UnmatchedCount"],
                                TotalReconciled = (decimal)reader["TotalReconciled"]
                            };
                        }
                    }
                }
            }

            return new ReconciliationResult { Success = false };
        }

        /// <summary>
        /// Get chargeback report for admin dashboard.
        /// </summary>
        public async Task<object> GetChargebackReportAsync(DateTime startDate, DateTime endDate)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand("EXEC dbo.sp_GetChargebackReport @StartDate, @EndDate", conn))
                {
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);
                    cmd.CommandTimeout = 30;

                    var results = new List<object>();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new
                            {
                                MerchantId = reader["MerchantId"],
                                ChargebackCount = reader["ChargebackCount"],
                                ChargebackAmount = reader["ChargebackAmount"],
                                TransactionCount = reader["TransactionCount"],
                                ChargebackRate = reader["ChargebackRate"]
                            });
                        }
                    }
                    return results;
                }
            }
        }

        private Settlement MapSettlement(SqlDataReader reader)
        {
            return new Settlement
            {
                SettlementId = reader.GetGuid(reader.GetOrdinal("SettlementId")),
                BatchId = reader.GetGuid(reader.GetOrdinal("BatchId")),
                Network = reader.GetString(reader.GetOrdinal("Network")),
                SettlementDate = reader.GetDateTime(reader.GetOrdinal("SettlementDate")),
                TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                TransactionCount = reader.GetInt32(reader.GetOrdinal("TransactionCount")),
                Status = (SettlementStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                FileName = reader.IsDBNull(reader.GetOrdinal("FileName"))
                    ? null : reader.GetString(reader.GetOrdinal("FileName"))
            };
        }
    }
}
