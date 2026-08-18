using System;
using System.Data;
using System.Data.SqlClient;
using log4net;

namespace BackOffice.DataAccess
{
    /// <summary>
    /// Data access class for transaction operations.
    /// Uses raw ADO.NET with SqlDataReader and DataTable patterns.
    /// </summary>
    public class TransactionDataAccess
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(TransactionDataAccess));

        public DataTable GetTransactionsByDateRange(DateTime startDate, DateTime endDate)
        {
            // Using parameterized query via stored procedure
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@StartDate", SqlDbType.DateTime) { Value = startDate },
                new SqlParameter("@EndDate", SqlDbType.DateTime) { Value = endDate }
            };

            return DataAccessHelper.ExecuteStoredProcedure("sp_GetTransactionsByDateRange", parameters);
        }

        public DataTable SearchTransactions(string searchTerm, string status, decimal? minAmount, decimal? maxAmount)
        {
            // Non-parameterized - builds SQL dynamically
            string sql = "SELECT t.TransactionId, m.MerchantName, t.CardType, " +
                "RIGHT(t.CardNumber, 4) AS CardLast4, t.Amount, " +
                "t.TransactionDate, t.Status, t.ResponseCode, t.IsFlagged " +
                "FROM Transactions t " +
                "INNER JOIN Merchants m ON t.MerchantId = m.MerchantId " +
                "WHERE 1=1 ";

            if (!string.IsNullOrEmpty(searchTerm))
            {
                // SQL injection risk - concatenating user input
                sql += "AND (t.TransactionId LIKE '%" + searchTerm + "%' " +
                    "OR m.MerchantName LIKE '%" + searchTerm + "%' " +
                    "OR t.CardNumber LIKE '%" + searchTerm + "%') ";
            }

            if (!string.IsNullOrEmpty(status))
            {
                sql += "AND t.Status = '" + status + "' ";
            }

            if (minAmount.HasValue)
            {
                sql += "AND t.Amount >= " + minAmount.Value + " ";
            }

            if (maxAmount.HasValue)
            {
                sql += "AND t.Amount <= " + maxAmount.Value + " ";
            }

            sql += "ORDER BY t.TransactionDate DESC";

            return DataAccessHelper.ExecuteDataTable(sql);
        }

        public DataRow GetTransactionById(int transactionId)
        {
            // Non-parameterized
            string sql = "SELECT t.*, m.MerchantName, m.MerchantType " +
                "FROM Transactions t " +
                "INNER JOIN Merchants m ON t.MerchantId = m.MerchantId " +
                "WHERE t.TransactionId = " + transactionId;

            DataTable dt = DataAccessHelper.ExecuteDataTable(sql);
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public void UpdateTransactionFlag(int transactionId, bool isFlagged, string flaggedBy, string reason)
        {
            string sql;
            if (isFlagged)
            {
                sql = "UPDATE Transactions SET IsFlagged = 1, " +
                    "FlaggedBy = '" + flaggedBy + "', " +
                    "FlagReason = '" + reason.Replace("'", "''") + "', " +
                    "FlaggedDate = GETDATE(), Status = 'Flagged' " +
                    "WHERE TransactionId = " + transactionId;
            }
            else
            {
                sql = "UPDATE Transactions SET IsFlagged = 0, " +
                    "FlaggedBy = NULL, FlagReason = NULL, FlaggedDate = NULL " +
                    "WHERE TransactionId = " + transactionId;
            }

            DataAccessHelper.ExecuteNonQuery(sql);
        }

        public DataTable GetFlaggedTransactions(int maxRows = 100)
        {
            string sql = "SELECT TOP " + maxRows + " t.TransactionId, m.MerchantName, t.Amount, " +
                "t.TransactionDate, t.FlagReason, t.FlaggedBy, t.FlaggedDate, t.Status " +
                "FROM Transactions t " +
                "INNER JOIN Merchants m ON t.MerchantId = m.MerchantId " +
                "WHERE t.IsFlagged = 1 " +
                "ORDER BY t.FlaggedDate DESC";

            return DataAccessHelper.ExecuteDataTable(sql);
        }
    }
}
