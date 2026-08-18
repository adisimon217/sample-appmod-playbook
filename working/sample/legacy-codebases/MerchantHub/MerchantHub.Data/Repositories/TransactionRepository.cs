using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using log4net;
using MerchantHub.Core.Models;

namespace MerchantHub.Data.Repositories
{
    public interface ITransactionRepository
    {
        Transaction GetById(long transactionId);
        List<Transaction> GetByMerchantAndDateRange(int merchantId, DateTime startDate, DateTime endDate);
        List<Transaction> GetByDateRange(DateTime startDate, DateTime endDate);
        List<Transaction> GetRecentByMerchant(int merchantId, int count);
        List<Transaction> GetByCardFingerprint(string fingerprint, int merchantId, int count);
        List<Transaction> Search(int merchantId, DateTime startDate, DateTime endDate, string status, string search, int page, int pageSize);
        int SearchCount(int merchantId, DateTime startDate, DateTime endDate, string status, string search);
        List<BatchSummary> GetBatchSummary(int merchantId, DateTime date);
        int GetTodayCount();
        void Update(Transaction transaction);
    }

    /// <summary>
    /// Transaction repository - mixed EF and raw SQL patterns.
    /// Some queries use EF, complex ones use raw SQL for performance.
    /// TODO: Standardize on one approach (ticket JIRA-3892)
    /// </summary>
    public class TransactionRepository : ITransactionRepository
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(TransactionRepository));
        private readonly MerchantHubContext _context;
        private readonly string _connectionString;

        public TransactionRepository(MerchantHubContext context, string connectionString)
        {
            _context = context;
            _connectionString = connectionString;
        }

        public Transaction GetById(long transactionId)
        {
            return _context.Transactions.Find(transactionId);
        }

        /// <summary>
        /// Get transactions by merchant and date range.
        /// Uses raw SQL for this one because the EF query plan was terrible
        /// with the date range filter (see performance incident INC-2022-1134)
        /// </summary>
        public List<Transaction> GetByMerchantAndDateRange(int merchantId, DateTime startDate, DateTime endDate)
        {
            // Raw SQL - EF generated query was doing full table scan
            var sql = @"
                SELECT TransactionId, MerchantId, ReferenceNumber, AuthorizationCode,
                       Amount, RefundAmount, Fee, NetAmount, Currency, Status,
                       CardType, Last4Digits, CardFingerprint, EntryMode,
                       Description, CustomerName, CustomerEmail, BatchNumber,
                       TransactionDate, SettlementDate, CreatedDate,
                       DeclineReason, ResponseCode, TerminalId, PayGateTransactionId
                FROM MH_Transactions WITH (NOLOCK)
                WHERE MerchantId = @MerchantId
                  AND TransactionDate >= @StartDate
                  AND TransactionDate < @EndDate
                ORDER BY TransactionDate DESC";

            var transactions = new List<Transaction>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@MerchantId", merchantId);
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);
                    cmd.CommandTimeout = 60;

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            transactions.Add(MapTransaction(reader));
                        }
                    }
                }
            }

            return transactions;
        }

        public List<Transaction> GetByDateRange(DateTime startDate, DateTime endDate)
        {
            return _context.Transactions
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate < endDate)
                .ToList();
        }

        public List<Transaction> GetRecentByMerchant(int merchantId, int count)
        {
            return _context.Transactions
                .Where(t => t.MerchantId == merchantId)
                .OrderByDescending(t => t.TransactionDate)
                .Take(count)
                .ToList();
        }

        public List<Transaction> GetByCardFingerprint(string fingerprint, int merchantId, int count)
        {
            return _context.Transactions
                .Where(t => t.CardFingerprint == fingerprint && t.MerchantId == merchantId)
                .OrderByDescending(t => t.TransactionDate)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Search transactions with filtering and pagination.
        /// Raw SQL because EF doesn't handle the dynamic WHERE well.
        /// </summary>
        public List<Transaction> Search(int merchantId, DateTime startDate, DateTime endDate,
            string status, string search, int page, int pageSize)
        {
            var sql = @"
                SELECT TransactionId, MerchantId, ReferenceNumber, AuthorizationCode,
                       Amount, RefundAmount, Fee, NetAmount, Currency, Status,
                       CardType, Last4Digits, CardFingerprint, EntryMode,
                       Description, CustomerName, CustomerEmail, BatchNumber,
                       TransactionDate, SettlementDate, CreatedDate,
                       DeclineReason, ResponseCode, TerminalId, PayGateTransactionId
                FROM MH_Transactions WITH (NOLOCK)
                WHERE MerchantId = @MerchantId
                  AND TransactionDate >= @StartDate
                  AND TransactionDate < @EndDate";

            if (!string.IsNullOrEmpty(status))
                sql += " AND Status = @Status";
            if (!string.IsNullOrEmpty(search))
                sql += " AND (ReferenceNumber LIKE @Search OR CustomerName LIKE @Search OR Last4Digits LIKE @Search OR Description LIKE @Search)";

            sql += " ORDER BY TransactionDate DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var transactions = new List<Transaction>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@MerchantId", merchantId);
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);
                    cmd.Parameters.AddWithValue("@Status", (object)status ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Search", "%" + (search ?? "") + "%");
                    cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.CommandTimeout = 30;

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            transactions.Add(MapTransaction(reader));
                        }
                    }
                }
            }

            return transactions;
        }

        public int SearchCount(int merchantId, DateTime startDate, DateTime endDate, string status, string search)
        {
            var sql = @"
                SELECT COUNT(*)
                FROM MH_Transactions WITH (NOLOCK)
                WHERE MerchantId = @MerchantId
                  AND TransactionDate >= @StartDate
                  AND TransactionDate < @EndDate";

            if (!string.IsNullOrEmpty(status))
                sql += " AND Status = @Status";
            if (!string.IsNullOrEmpty(search))
                sql += " AND (ReferenceNumber LIKE @Search OR CustomerName LIKE @Search OR Last4Digits LIKE @Search)";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@MerchantId", merchantId);
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);
                    cmd.Parameters.AddWithValue("@Status", (object)status ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Search", "%" + (search ?? "") + "%");
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        public List<BatchSummary> GetBatchSummary(int merchantId, DateTime date)
        {
            var sql = @"
                SELECT BatchNumber, 
                       CAST(TransactionDate AS DATE) as BatchDate,
                       COUNT(*) as TransactionCount,
                       SUM(Amount) as TotalAmount,
                       SUM(ISNULL(Fee, 0)) as TotalFees,
                       SUM(Amount - ISNULL(Fee, 0)) as NetAmount,
                       CASE WHEN MAX(SettlementDate) IS NOT NULL THEN 'Settled' ELSE 'Pending' END as Status
                FROM MH_Transactions WITH (NOLOCK)
                WHERE MerchantId = @MerchantId
                  AND CAST(TransactionDate AS DATE) = @Date
                GROUP BY BatchNumber, CAST(TransactionDate AS DATE)
                ORDER BY BatchNumber";

            var batches = new List<BatchSummary>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@MerchantId", merchantId);
                    cmd.Parameters.AddWithValue("@Date", date.Date);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            batches.Add(new BatchSummary
                            {
                                BatchNumber = reader["BatchNumber"]?.ToString(),
                                BatchDate = (DateTime)reader["BatchDate"],
                                TransactionCount = (int)reader["TransactionCount"],
                                TotalAmount = (decimal)reader["TotalAmount"],
                                TotalFees = (decimal)reader["TotalFees"],
                                NetAmount = (decimal)reader["NetAmount"],
                                Status = reader["Status"].ToString()
                            });
                        }
                    }
                }
            }

            return batches;
        }

        public int GetTodayCount()
        {
            return _context.Transactions.Count(t => t.TransactionDate >= DateTime.Today);
        }

        public void Update(Transaction transaction)
        {
            var entry = _context.Entry(transaction);
            if (entry.State == System.Data.Entity.EntityState.Detached)
            {
                _context.Transactions.Attach(transaction);
                entry.State = System.Data.Entity.EntityState.Modified;
            }
            _context.SaveChanges();
        }

        private Transaction MapTransaction(SqlDataReader reader)
        {
            return new Transaction
            {
                TransactionId = reader.GetInt64(reader.GetOrdinal("TransactionId")),
                MerchantId = reader.GetInt32(reader.GetOrdinal("MerchantId")),
                ReferenceNumber = reader["ReferenceNumber"]?.ToString(),
                AuthorizationCode = reader["AuthorizationCode"]?.ToString(),
                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                RefundAmount = reader.IsDBNull(reader.GetOrdinal("RefundAmount")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("RefundAmount")),
                Fee = reader.IsDBNull(reader.GetOrdinal("Fee")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("Fee")),
                NetAmount = reader.IsDBNull(reader.GetOrdinal("NetAmount")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("NetAmount")),
                Currency = reader["Currency"]?.ToString(),
                Status = reader["Status"]?.ToString(),
                CardType = reader["CardType"]?.ToString(),
                Last4Digits = reader["Last4Digits"]?.ToString(),
                CardFingerprint = reader["CardFingerprint"]?.ToString(),
                EntryMode = reader["EntryMode"]?.ToString(),
                Description = reader["Description"]?.ToString(),
                CustomerName = reader["CustomerName"]?.ToString(),
                CustomerEmail = reader["CustomerEmail"]?.ToString(),
                BatchNumber = reader["BatchNumber"]?.ToString(),
                TransactionDate = reader.GetDateTime(reader.GetOrdinal("TransactionDate")),
                SettlementDate = reader.IsDBNull(reader.GetOrdinal("SettlementDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("SettlementDate")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                DeclineReason = reader["DeclineReason"]?.ToString(),
                ResponseCode = reader["ResponseCode"]?.ToString(),
                TerminalId = reader["TerminalId"]?.ToString(),
                PayGateTransactionId = reader.IsDBNull(reader.GetOrdinal("PayGateTransactionId")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("PayGateTransactionId"))
            };
        }
    }
}
