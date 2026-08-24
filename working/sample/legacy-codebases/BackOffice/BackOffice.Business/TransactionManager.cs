using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using log4net;

namespace BackOffice.Business
{
    /// <summary>
    /// TransactionManager handles business logic for transaction operations.
    /// NOTE: This class mixes business logic AND data access - legacy anti-pattern.
    /// Data access should be in the DataAccess layer, but this class bypasses it
    /// for "performance reasons" (original developer's justification from 2014).
    /// </summary>
    public class TransactionManager
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(TransactionManager));
        private static readonly string _connectionString = ConfigurationManager.ConnectionStrings["BackOfficeDB"].ConnectionString;

        public DataTable GetTransactionsByDateRange(DateTime startDate, DateTime endDate, string merchantFilter = null)
        {
            // Direct data access in business layer - anti-pattern
            string sql = "EXEC sp_GetTransactionsByDateRange @StartDate = '" +
                startDate.ToString("yyyy-MM-dd") + "', @EndDate = '" +
                endDate.ToString("yyyy-MM-dd 23:59:59") + "'";

            if (!string.IsNullOrEmpty(merchantFilter))
            {
                sql += ", @MerchantFilter = '" + merchantFilter + "'";
            }

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 120; // 2 minute timeout for large result sets
                    DataTable dt = new DataTable();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                    return dt;
                }
            }
        }

        public DataTable SearchTransactions(string searchTerm, string status, decimal? minAmount, decimal? maxAmount)
        {
            // Non-parameterized search - SQL injection risk
            string sql = "EXEC sp_SearchTransactions @SearchTerm = '%" + searchTerm + "%'";

            if (!string.IsNullOrEmpty(status))
            {
                sql += ", @Status = '" + status + "'";
            }
            if (minAmount.HasValue)
            {
                sql += ", @MinAmount = " + minAmount.Value;
            }
            if (maxAmount.HasValue)
            {
                sql += ", @MaxAmount = " + maxAmount.Value;
            }

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 60;
                    DataTable dt = new DataTable();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                    return dt;
                }
            }
        }

        public void FlagTransaction(int transactionId, string flaggedBy, string reason)
        {
            _log.InfoFormat("Flagging transaction {0} by {1}: {2}", transactionId, flaggedBy, reason);

            // Business rule: cannot flag transactions older than 90 days
            string checkSql = "SELECT TransactionDate FROM Transactions WHERE TransactionId = " + transactionId;
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(checkSql, conn))
                {
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        DateTime txDate = Convert.ToDateTime(result);
                        if ((DateTime.Now - txDate).TotalDays > 90)
                        {
                            throw new InvalidOperationException("Cannot flag transactions older than 90 days");
                        }
                    }
                }

                // Perform the flag - direct SQL in business layer
                string flagSql = "UPDATE Transactions SET IsFlagged = 1, FlaggedBy = '" + flaggedBy + "', " +
                    "FlagReason = '" + reason.Replace("'", "''") + "', FlaggedDate = GETDATE(), " +
                    "Status = 'Flagged' WHERE TransactionId = " + transactionId;

                using (SqlCommand cmd = new SqlCommand(flagSql, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Log to audit trail
                string auditSql = "EXEC sp_AuditTrail @Action = 'FLAG_TRANSACTION', " +
                    "@EntityType = 'Transaction', @EntityId = " + transactionId + ", " +
                    "@PerformedBy = '" + flaggedBy + "', @Details = 'Reason: " + reason.Replace("'", "''") + "'";

                using (SqlCommand cmd = new SqlCommand(auditSql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void BulkUpdateTransactionStatus(int[] transactionIds, string newStatus, string updatedBy)
        {
            if (transactionIds == null || transactionIds.Length == 0)
                return;

            // Build comma-separated list for IN clause - anti-pattern
            string idList = string.Join(",", transactionIds);

            _log.InfoFormat("Bulk status update for {0} transactions to '{1}' by {2}",
                transactionIds.Length, newStatus, updatedBy);

            // Direct SQL - no parameterization for the status value
            string sql = "EXEC sp_BulkUpdateStatus @TransactionIds = '" + idList + "', " +
                "@NewStatus = '" + newStatus + "', @UpdatedBy = '" + updatedBy + "'";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 180;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public DataTable GetChargebackQueue()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("EXEC sp_GetChargebackQueue", conn))
                {
                    cmd.CommandTimeout = 60;
                    DataTable dt = new DataTable();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                    return dt;
                }
            }
        }
    }
}
