using System;
using System.Data;
using System.Data.SqlClient;
using log4net;

namespace BackOffice.DataAccess
{
    /// <summary>
    /// Data access class for dispute management operations.
    /// </summary>
    public class DisputeDataAccess
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DisputeDataAccess));

        public DataTable GetDisputesByStatus(string status, string assignedTo = null)
        {
            // Non-parameterized stored procedure call
            string sql = "EXEC sp_GetDisputesByStatus @Status = '" + status + "'";

            if (!string.IsNullOrEmpty(assignedTo))
            {
                sql += ", @AssignedTo = '" + assignedTo + "'";
            }

            return DataAccessHelper.ExecuteDataTable(sql);
        }

        public DataRow GetDisputeById(int disputeId)
        {
            string sql = "SELECT d.*, m.MerchantName, t.Amount AS TransactionAmount, " +
                "t.CardType, t.TransactionDate " +
                "FROM Disputes d " +
                "INNER JOIN Transactions t ON d.TransactionId = t.TransactionId " +
                "INNER JOIN Merchants m ON t.MerchantId = m.MerchantId " +
                "WHERE d.DisputeId = " + disputeId;

            DataTable dt = DataAccessHelper.ExecuteDataTable(sql);
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public void UpdateDisputeResolution(int disputeId, string resolution, string notes, string resolvedBy)
        {
            string sql = "EXEC sp_UpdateDisputeResolution " +
                "@DisputeId = " + disputeId + ", " +
                "@Resolution = '" + resolution + "', " +
                "@Notes = '" + notes.Replace("'", "''") + "', " +
                "@ResolvedBy = '" + resolvedBy + "'";

            DataAccessHelper.ExecuteNonQuery(sql);
            _log.InfoFormat("Dispute {0} resolved: {1} by {2}", disputeId, resolution, resolvedBy);
        }

        public void EscalateDispute(int disputeId, string escalatedBy, string reason)
        {
            string sql = "UPDATE Disputes SET Status = 'Escalated', " +
                "EscalatedBy = '" + escalatedBy + "', " +
                "EscalatedDate = GETDATE(), " +
                "Notes = Notes + CHAR(13) + 'Escalated by " + escalatedBy + ": " + reason.Replace("'", "''") + "' " +
                "WHERE DisputeId = " + disputeId;

            DataAccessHelper.ExecuteNonQuery(sql);
        }

        public DataTable GetDisputeHistory(int disputeId)
        {
            string sql = "SELECT Action, PerformedBy, ActionDate, Notes " +
                "FROM DisputeHistory WHERE DisputeId = " + disputeId + " " +
                "ORDER BY ActionDate DESC";

            return DataAccessHelper.ExecuteDataTable(sql);
        }
    }
}
