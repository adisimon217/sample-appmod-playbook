using System;
using System.Data;
using System.Data.SqlClient;
using log4net;

namespace BackOffice.DataAccess
{
    /// <summary>
    /// Data access class for merchant operations.
    /// </summary>
    public class MerchantDataAccess
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MerchantDataAccess));

        public DataTable GetMerchantList(string statusFilter = null)
        {
            string sql = "EXEC sp_GetMerchantList";
            if (!string.IsNullOrEmpty(statusFilter))
            {
                sql += " @Status = '" + statusFilter + "'";
            }

            return DataAccessHelper.ExecuteDataTable(sql);
        }

        public DataRow GetMerchantDetails(int merchantId)
        {
            string sql = "EXEC sp_GetMerchantDetails @MerchantId = " + merchantId;
            DataTable dt = DataAccessHelper.ExecuteDataTable(sql);
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public DataTable SearchMerchants(string searchTerm)
        {
            // Non-parameterized search
            string sql = "SELECT MerchantId, MerchantName, LegalName, Status, " +
                "BusinessType, MccCode, CreatedDate " +
                "FROM Merchants WHERE " +
                "MerchantName LIKE '%" + searchTerm + "%' " +
                "OR LegalName LIKE '%" + searchTerm + "%' " +
                "OR TaxId LIKE '%" + searchTerm + "%' " +
                "ORDER BY MerchantName";

            return DataAccessHelper.ExecuteDataTable(sql);
        }

        public bool MerchantNameExists(string merchantName)
        {
            return DataAccessHelper.RecordExists("Merchants", "MerchantName = '" + merchantName + "'");
        }

        public void UpdateMerchantStatus(int merchantId, string newStatus, string updatedBy)
        {
            string sql = "UPDATE Merchants SET Status = '" + newStatus + "', " +
                "LastModifiedBy = '" + updatedBy + "', " +
                "LastModifiedDate = GETDATE() " +
                "WHERE MerchantId = " + merchantId;

            DataAccessHelper.ExecuteNonQuery(sql);
            _log.InfoFormat("Merchant {0} status updated to '{1}' by {2}", merchantId, newStatus, updatedBy);
        }
    }
}
