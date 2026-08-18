using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using log4net;

namespace BackOffice.Business
{
    /// <summary>
    /// Handles merchant onboarding workflow.
    /// Business logic and data access are intermixed (anti-pattern).
    /// </summary>
    public class MerchantOnboardingService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MerchantOnboardingService));
        private static readonly string _connectionString = ConfigurationManager.ConnectionStrings["BackOfficeDB"].ConnectionString;

        public bool ValidateMerchantName(string merchantName)
        {
            // Non-parameterized query in business logic
            string sql = "SELECT COUNT(*) FROM Merchants WHERE MerchantName = '" + merchantName + "'";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count == 0; // true if name is available
                }
            }
        }

        public int CreateMerchant(string merchantName, string legalName, string taxId,
            string businessType, string mccCode, decimal annualVolume,
            string contactName, string email, string phone,
            string address, string city, string state, string zip,
            string bankName, string routingNumber, string accountNumber, string accountType,
            string riskTier, decimal chargebackLimit, string riskNotes, string createdBy)
        {
            _log.InfoFormat("Creating merchant: {0} by {1}", merchantName, createdBy);

            // Validation rules
            if (string.IsNullOrEmpty(merchantName))
                throw new ArgumentException("Merchant name is required");
            if (string.IsNullOrEmpty(taxId))
                throw new ArgumentException("Tax ID is required");
            if (string.IsNullOrEmpty(routingNumber) || routingNumber.Length != 9)
                throw new ArgumentException("Valid 9-digit routing number is required");

            // Business rule: high-risk merchants require manager approval
            if (riskTier == "High" || riskTier == "Critical")
            {
                _log.WarnFormat("High-risk merchant onboarding: {0}, Tier: {1}", merchantName, riskTier);
            }

            // Direct SQL execution - massive parameter list
            string sql = "EXEC sp_CreateMerchant " +
                "@MerchantName = '" + merchantName.Replace("'", "''") + "', " +
                "@LegalName = '" + legalName.Replace("'", "''") + "', " +
                "@TaxId = '" + taxId + "', " +
                "@BusinessType = '" + businessType + "', " +
                "@MccCode = '" + mccCode + "', " +
                "@AnnualVolume = " + annualVolume + ", " +
                "@ContactName = '" + contactName.Replace("'", "''") + "', " +
                "@Email = '" + email + "', " +
                "@Phone = '" + phone + "', " +
                "@Address = '" + address.Replace("'", "''") + "', " +
                "@City = '" + city + "', " +
                "@State = '" + state + "', " +
                "@Zip = '" + zip + "', " +
                "@BankName = '" + bankName.Replace("'", "''") + "', " +
                "@RoutingNumber = '" + routingNumber + "', " +
                "@AccountNumber = '" + accountNumber + "', " +
                "@AccountType = '" + accountType + "', " +
                "@RiskTier = '" + riskTier + "', " +
                "@ChargebackLimit = " + chargebackLimit + ", " +
                "@RiskNotes = '" + riskNotes.Replace("'", "''") + "', " +
                "@CreatedBy = '" + createdBy + "'";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 30;
                    object result = cmd.ExecuteScalar();
                    int merchantId = Convert.ToInt32(result);

                    _log.InfoFormat("Merchant created successfully: ID={0}, Name={1}", merchantId, merchantName);
                    return merchantId;
                }
            }
        }

        public DataTable GetMerchantList(string statusFilter = null)
        {
            string sql = "EXEC sp_GetMerchantList";
            if (!string.IsNullOrEmpty(statusFilter))
            {
                sql += " @Status = '" + statusFilter + "'";
            }

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    DataTable dt = new DataTable();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                    return dt;
                }
            }
        }

        public DataRow GetMerchantDetails(int merchantId)
        {
            // Non-parameterized
            string sql = "EXEC sp_GetMerchantDetails @MerchantId = " + merchantId;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    DataTable dt = new DataTable();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                    return dt.Rows.Count > 0 ? dt.Rows[0] : null;
                }
            }
        }
    }
}
