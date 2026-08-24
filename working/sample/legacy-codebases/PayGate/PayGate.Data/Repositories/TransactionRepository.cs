using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using PayGate.Core.Models;

namespace PayGate.Data.Repositories
{
    /// <summary>
    /// Transaction data access layer.
    /// 
    /// IMPORTANT: This repository uses raw ADO.NET (SqlCommand, SqlBulkCopy)
    /// instead of Entity Framework for all write operations. At 50K transactions/hour,
    /// EF's change tracking and object materialization overhead is unacceptable.
    /// 
    /// Read operations for non-critical paths (reporting, admin) use EF via PayGateDbContext.
    /// </summary>
    public class TransactionRepository
    {
        private readonly string _connectionString;

        // Retry policy for transient SQL failures (deadlocks, timeouts)
        private static readonly AsyncRetryPolicy _retryPolicy = Policy
            .Handle<SqlException>(ex =>
                ex.Number == 1205 || // Deadlock victim
                ex.Number == -2 ||   // Timeout
                ex.Number == 40613 || // Database unavailable (AG failover)
                ex.Number == 40197)   // Service error
            .WaitAndRetryAsync(3,
                retryAttempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    EventLog.WriteEntry("PayGate",
                        $"SQL retry {retryCount} after {timeSpan.TotalMs}ms. Error: {((SqlException)exception).Number}",
                        EventLogEntryType.Warning);
                });

        public TransactionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Insert a single transaction using parameterized SQL command.
        /// Optimized for speed - bypasses EF change tracking entirely.
        /// </summary>
        public async Task InsertAsync(Transaction transaction)
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    using (var cmd = new SqlCommand(@"
                        INSERT INTO dbo.Transactions (
                            TransactionId, MerchantId, Amount, Currency, CardNumber,
                            CardExpiry, CardholderName, TransactionType, Status,
                            CreatedDate, ProcessedDate, AuthorizationCode, ResponseCode,
                            ResponseMessage, IpAddress, UserAgent, BatchId,
                            OriginalTransactionId, ProcessingFee, CardBrand, CardBin,
                            CardLast4, LatencyMs
                        ) VALUES (
                            @TransactionId, @MerchantId, @Amount, @Currency, @CardNumber,
                            @CardExpiry, @CardholderName, @TransactionType, @Status,
                            @CreatedDate, @ProcessedDate, @AuthorizationCode, @ResponseCode,
                            @ResponseMessage, @IpAddress, @UserAgent, @BatchId,
                            @OriginalTransactionId, @ProcessingFee, @CardBrand, @CardBin,
                            @CardLast4, @LatencyMs
                        )", conn))
                    {
                        cmd.CommandTimeout = 10;

                        cmd.Parameters.AddWithValue("@TransactionId", transaction.TransactionId);
                        cmd.Parameters.AddWithValue("@MerchantId", transaction.MerchantId);
                        cmd.Parameters.AddWithValue("@Amount", transaction.Amount);
                        cmd.Parameters.AddWithValue("@Currency", transaction.Currency ?? "USD");
                        cmd.Parameters.AddWithValue("@CardNumber", MaskCardNumber(transaction.CardNumber));
                        cmd.Parameters.AddWithValue("@CardExpiry", (object)transaction.CardExpiry ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CardholderName", (object)transaction.CardholderName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TransactionType", (int)transaction.TransactionType);
                        cmd.Parameters.AddWithValue("@Status", (int)transaction.Status);
                        cmd.Parameters.AddWithValue("@CreatedDate", transaction.CreatedDate);
                        cmd.Parameters.AddWithValue("@ProcessedDate", (object)transaction.ProcessedDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@AuthorizationCode", (object)transaction.AuthorizationCode ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ResponseCode", (object)transaction.ResponseCode ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ResponseMessage", (object)transaction.ResponseMessage ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IpAddress", (object)transaction.IpAddress ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UserAgent", (object)transaction.UserAgent ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BatchId", (object)transaction.BatchId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@OriginalTransactionId", (object)transaction.OriginalTransactionId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ProcessingFee", (object)transaction.ProcessingFee ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CardBrand", (object)transaction.CardBrand ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CardBin", (object)transaction.CardBin ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CardLast4", (object)transaction.CardLast4 ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LatencyMs", (object)transaction.LatencyMs ?? DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            });
        }

        /// <summary>
        /// Bulk insert transactions using SqlBulkCopy.
        /// Used for batch processing and migration scenarios.
        /// Can insert 5000+ rows in under 2 seconds.
        /// </summary>
        public async Task BulkInsertAsync(IEnumerable<Transaction> transactions)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("TransactionId", typeof(Guid));
            dataTable.Columns.Add("MerchantId", typeof(string));
            dataTable.Columns.Add("Amount", typeof(decimal));
            dataTable.Columns.Add("Currency", typeof(string));
            dataTable.Columns.Add("CardNumber", typeof(string));
            dataTable.Columns.Add("TransactionType", typeof(int));
            dataTable.Columns.Add("Status", typeof(int));
            dataTable.Columns.Add("CreatedDate", typeof(DateTime));
            dataTable.Columns.Add("ProcessedDate", typeof(DateTime));
            dataTable.Columns.Add("AuthorizationCode", typeof(string));
            dataTable.Columns.Add("ResponseCode", typeof(string));
            dataTable.Columns.Add("ProcessingFee", typeof(decimal));
            dataTable.Columns.Add("CardBrand", typeof(string));
            dataTable.Columns.Add("CardBin", typeof(string));
            dataTable.Columns.Add("CardLast4", typeof(string));
            dataTable.Columns.Add("BatchId", typeof(Guid));
            dataTable.Columns.Add("LatencyMs", typeof(int));

            foreach (var txn in transactions)
            {
                dataTable.Rows.Add(
                    txn.TransactionId,
                    txn.MerchantId,
                    txn.Amount,
                    txn.Currency ?? "USD",
                    MaskCardNumber(txn.CardNumber),
                    (int)txn.TransactionType,
                    (int)txn.Status,
                    txn.CreatedDate,
                    txn.ProcessedDate ?? (object)DBNull.Value,
                    txn.AuthorizationCode ?? (object)DBNull.Value,
                    txn.ResponseCode ?? (object)DBNull.Value,
                    txn.ProcessingFee ?? (object)DBNull.Value,
                    txn.CardBrand ?? (object)DBNull.Value,
                    txn.CardBin ?? (object)DBNull.Value,
                    txn.CardLast4 ?? (object)DBNull.Value,
                    txn.BatchId ?? (object)DBNull.Value,
                    txn.LatencyMs ?? (object)DBNull.Value);
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock, null))
                {
                    bulkCopy.DestinationTableName = "dbo.Transactions";
                    bulkCopy.BatchSize = 1000;
                    bulkCopy.BulkCopyTimeout = 60;
                    bulkCopy.EnableStreaming = true;

                    // Map columns explicitly
                    bulkCopy.ColumnMappings.Add("TransactionId", "TransactionId");
                    bulkCopy.ColumnMappings.Add("MerchantId", "MerchantId");
                    bulkCopy.ColumnMappings.Add("Amount", "Amount");
                    bulkCopy.ColumnMappings.Add("Currency", "Currency");
                    bulkCopy.ColumnMappings.Add("CardNumber", "CardNumber");
                    bulkCopy.ColumnMappings.Add("TransactionType", "TransactionType");
                    bulkCopy.ColumnMappings.Add("Status", "Status");
                    bulkCopy.ColumnMappings.Add("CreatedDate", "CreatedDate");
                    bulkCopy.ColumnMappings.Add("ProcessedDate", "ProcessedDate");
                    bulkCopy.ColumnMappings.Add("AuthorizationCode", "AuthorizationCode");
                    bulkCopy.ColumnMappings.Add("ResponseCode", "ResponseCode");
                    bulkCopy.ColumnMappings.Add("ProcessingFee", "ProcessingFee");
                    bulkCopy.ColumnMappings.Add("CardBrand", "CardBrand");
                    bulkCopy.ColumnMappings.Add("CardBin", "CardBin");
                    bulkCopy.ColumnMappings.Add("CardLast4", "CardLast4");
                    bulkCopy.ColumnMappings.Add("BatchId", "BatchId");
                    bulkCopy.ColumnMappings.Add("LatencyMs", "LatencyMs");

                    await bulkCopy.WriteToServerAsync(dataTable);
                }
            }
        }

        /// <summary>
        /// Get transaction by ID.
        /// </summary>
        public async Task<Transaction> GetByIdAsync(Guid transactionId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(
                    "SELECT * FROM dbo.Transactions WITH (NOLOCK) WHERE TransactionId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", transactionId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                            return MapTransaction(reader);
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// Get transactions by merchant with pagination.
        /// </summary>
        public async Task<List<Transaction>> GetByMerchantAsync(
            string merchantId, DateTime startDate, DateTime endDate, int page, int pageSize)
        {
            var transactions = new List<Transaction>();

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    SELECT * FROM dbo.Transactions WITH (NOLOCK)
                    WHERE MerchantId = @MerchantId
                      AND CreatedDate >= @StartDate
                      AND CreatedDate <= @EndDate
                    ORDER BY CreatedDate DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", conn))
                {
                    cmd.Parameters.AddWithValue("@MerchantId", merchantId);
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);
                    cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            transactions.Add(MapTransaction(reader));
                    }
                }
            }

            return transactions;
        }

        /// <summary>
        /// Update transaction status.
        /// </summary>
        public async Task UpdateStatusAsync(Guid transactionId, TransactionStatus newStatus)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    UPDATE dbo.Transactions 
                    SET Status = @Status, 
                        SettledDate = CASE WHEN @Status = 7 THEN GETUTCDATE() ELSE SettledDate END
                    WHERE TransactionId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", transactionId);
                    cmd.Parameters.AddWithValue("@Status", (int)newStatus);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Get card velocity (number of transactions for a masked card in a time window).
        /// Used for fraud detection.
        /// </summary>
        public async Task<int> GetCardVelocityAsync(string maskedCard, TimeSpan window)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM dbo.Transactions WITH (NOLOCK)
                    WHERE CardNumber = @CardNumber
                      AND CreatedDate >= @CutoffDate", conn))
                {
                    cmd.Parameters.AddWithValue("@CardNumber", maskedCard);
                    cmd.Parameters.AddWithValue("@CutoffDate", DateTime.UtcNow.Subtract(window));
                    cmd.CommandTimeout = 5;

                    return (int)await cmd.ExecuteScalarAsync();
                }
            }
        }

        /// <summary>
        /// Check for duplicate transactions (same merchant, amount, card in a short window).
        /// </summary>
        public async Task<bool> IsDuplicateTransactionAsync(
            string merchantId, decimal amount, string cardLast4, TimeSpan window)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    SELECT CASE WHEN EXISTS (
                        SELECT 1 FROM dbo.Transactions WITH (NOLOCK)
                        WHERE MerchantId = @MerchantId
                          AND Amount = @Amount
                          AND CardLast4 = @CardLast4
                          AND CreatedDate >= @CutoffDate
                          AND Status NOT IN (3, 4, 5) -- Not declined/failed/voided
                    ) THEN 1 ELSE 0 END", conn))
                {
                    cmd.Parameters.AddWithValue("@MerchantId", merchantId);
                    cmd.Parameters.AddWithValue("@Amount", amount);
                    cmd.Parameters.AddWithValue("@CardLast4", cardLast4);
                    cmd.Parameters.AddWithValue("@CutoffDate", DateTime.UtcNow.Subtract(window));
                    cmd.CommandTimeout = 5;

                    return (int)await cmd.ExecuteScalarAsync() == 1;
                }
            }
        }

        /// <summary>
        /// Get peak hour metrics for admin dashboard.
        /// </summary>
        public async Task<PeakHourMetrics> GetPeakHourMetricsAsync(DateTime date)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand("EXEC dbo.sp_GetPeakHourMetrics @Date", conn))
                {
                    cmd.Parameters.AddWithValue("@Date", date);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new PeakHourMetrics
                            {
                                PeakVolume = reader.GetInt32(reader.GetOrdinal("PeakVolume")),
                                PeakHour = reader.GetInt32(reader.GetOrdinal("PeakHour")),
                                TotalVolume = reader.GetInt32(reader.GetOrdinal("TotalVolume")),
                                AvgLatencyMs = reader.GetDouble(reader.GetOrdinal("AvgLatencyMs")),
                                P99LatencyMs = reader.GetDouble(reader.GetOrdinal("P99LatencyMs"))
                            };
                        }
                        return new PeakHourMetrics();
                    }
                }
            }
        }

        /// <summary>
        /// Get count of failed transactions for a date.
        /// </summary>
        public async Task<int> GetFailedTransactionCountAsync(DateTime date)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM dbo.Transactions WITH (NOLOCK)
                    WHERE CAST(CreatedDate AS DATE) = @Date
                      AND Status IN (3, 4) -- Declined, Failed", conn))
                {
                    cmd.Parameters.AddWithValue("@Date", date.Date);
                    return (int)await cmd.ExecuteScalarAsync();
                }
            }
        }

        /// <summary>
        /// Get revenue report using stored procedure.
        /// </summary>
        public async Task<object> GetRevenueReportAsync(DateTime startDate, DateTime endDate)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand("EXEC dbo.sp_GetRevenueReport @StartDate, @EndDate", conn))
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
                                Date = reader["ReportDate"],
                                TotalAmount = reader["TotalAmount"],
                                TransactionCount = reader["TransactionCount"],
                                TotalFees = reader["TotalFees"],
                                NetRevenue = reader["NetRevenue"]
                            });
                        }
                    }
                    return results;
                }
            }
        }

        /// <summary>
        /// Archive old transactions to archive table (retention policy).
        /// </summary>
        public async Task<int> ArchiveTransactionsAsync(DateTime cutoffDate)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand("EXEC dbo.sp_ArchiveTransactions @CutoffDate", conn))
                {
                    cmd.Parameters.AddWithValue("@CutoffDate", cutoffDate);
                    cmd.CommandTimeout = 300; // 5 minutes for large archive operations
                    return await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private string MaskCardNumber(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 10)
                return cardNumber;

            // Store as BIN + masked middle + last 4: 411111XXXXXX1234
            var bin = cardNumber.Substring(0, 6);
            var last4 = cardNumber.Substring(cardNumber.Length - 4);
            var masked = new string('X', cardNumber.Length - 10);
            return bin + masked + last4;
        }

        private Transaction MapTransaction(SqlDataReader reader)
        {
            return new Transaction
            {
                TransactionId = reader.GetGuid(reader.GetOrdinal("TransactionId")),
                MerchantId = reader.GetString(reader.GetOrdinal("MerchantId")),
                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                Currency = reader.GetString(reader.GetOrdinal("Currency")),
                Status = (TransactionStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                TransactionType = (TransactionType)reader.GetInt32(reader.GetOrdinal("TransactionType")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                AuthorizationCode = reader.IsDBNull(reader.GetOrdinal("AuthorizationCode"))
                    ? null : reader.GetString(reader.GetOrdinal("AuthorizationCode")),
                ResponseCode = reader.IsDBNull(reader.GetOrdinal("ResponseCode"))
                    ? null : reader.GetString(reader.GetOrdinal("ResponseCode")),
                CardBrand = reader.IsDBNull(reader.GetOrdinal("CardBrand"))
                    ? null : reader.GetString(reader.GetOrdinal("CardBrand")),
                CardLast4 = reader.IsDBNull(reader.GetOrdinal("CardLast4"))
                    ? null : reader.GetString(reader.GetOrdinal("CardLast4"))
            };
        }
    }
}
