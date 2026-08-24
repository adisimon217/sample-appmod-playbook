using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using log4net;

namespace BackOffice.Business
{
    /// <summary>
    /// Processes dispute lifecycle events.
    /// Mixes business rules with direct data access (anti-pattern).
    /// </summary>
    public class DisputeProcessor
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DisputeProcessor));
        private static readonly string _connectionString = ConfigurationManager.ConnectionStrings["BackOfficeDB"].ConnectionString;
        private static readonly int _autoEscalationDays;

        static DisputeProcessor()
        {
            _autoEscalationDays = int.Parse(ConfigurationManager.AppSettings["DisputeAutoEscalationDays"] ?? "5");
        }

        public DataTable GetDisputesByStatus(string status, string assignedTo = null)
        {
            // Direct data access in business layer
            string sql = "EXEC sp_GetDisputesByStatus @Status = '" + status + "'";

            if (!string.IsNullOrEmpty(assignedTo))
            {
                sql += ", @AssignedTo = '" + assignedTo + "'";
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

        public void ResolveDispute(int disputeId, string resolution, string notes, string resolvedBy)
        {
            _log.InfoFormat("Resolving dispute {0} with resolution '{1}' by {2}", disputeId, resolution, resolvedBy);

            // Business rules validation
            if (string.IsNullOrEmpty(resolution))
                throw new ArgumentException("Resolution type is required");

            if (resolution == "RefundFull" || resolution == "RefundPartial")
            {
                // Business rule: refunds require checking merchant credit
                ValidateMerchantCreditForRefund(disputeId);
            }

            // Direct SQL execution in business layer
            string sql = "EXEC sp_UpdateDisputeResolution " +
                "@DisputeId = " + disputeId + ", " +
                "@Resolution = '" + resolution + "', " +
                "@Notes = '" + notes.Replace("'", "''") + "', " +
                "@ResolvedBy = '" + resolvedBy + "'";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // If fraud confirmed, flag the merchant
                if (resolution == "FraudConfirmed")
                {
                    FlagMerchantForReview(disputeId, conn);
                }
            }
        }

        private void ValidateMerchantCreditForRefund(int disputeId)
        {
            string sql = "SELECT m.CreditLimit, m.CurrentBalance, d.Amount " +
                "FROM Disputes d " +
                "INNER JOIN Transactions t ON d.TransactionId = t.TransactionId " +
                "INNER JOIN Merchants m ON t.MerchantId = m.MerchantId " +
                "WHERE d.DisputeId = " + disputeId;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            decimal creditLimit = reader.GetDecimal(0);
                            decimal currentBalance = reader.GetDecimal(1);
                            decimal refundAmount = reader.GetDecimal(2);

                            if (currentBalance + refundAmount > creditLimit)
                            {
                                _log.WarnFormat("Merchant credit limit exceeded for dispute {0}. Balance: {1}, Refund: {2}, Limit: {3}",
                                    disputeId, currentBalance, refundAmount, creditLimit);
                                // Don't throw - just log warning. Operations decides.
                            }
                        }
                    }
                }
            }
        }

        private void FlagMerchantForReview(int disputeId, SqlConnection conn)
        {
            string sql = "UPDATE m SET m.RiskLevel = 'High', m.UnderReview = 1, " +
                "m.ReviewReason = 'Fraud confirmed on dispute #" + disputeId + "' " +
                "FROM Merchants m " +
                "INNER JOIN Transactions t ON m.MerchantId = t.MerchantId " +
                "INNER JOIN Disputes d ON t.TransactionId = d.TransactionId " +
                "WHERE d.DisputeId = " + disputeId;

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.ExecuteNonQuery();
            }

            _log.WarnFormat("Merchant flagged for review due to fraud on dispute {0}", disputeId);
        }

        public void AutoEscalateOverdueDisputes()
        {
            _log.Info("Running auto-escalation for overdue disputes");

            string sql = "UPDATE Disputes SET Status = 'Escalated', " +
                "EscalatedBy = 'SYSTEM', EscalatedDate = GETDATE(), " +
                "Notes = Notes + CHAR(13) + 'Auto-escalated: exceeded " + _autoEscalationDays + " day SLA' " +
                "WHERE Status IN ('New', 'UnderReview') " +
                "AND DATEDIFF(DAY, CreatedDate, GETDATE()) > " + _autoEscalationDays + " " +
                "AND Status != 'Escalated'";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    int affected = cmd.ExecuteNonQuery();
                    _log.InfoFormat("Auto-escalated {0} overdue disputes", affected);
                }
            }
        }
    }
}
