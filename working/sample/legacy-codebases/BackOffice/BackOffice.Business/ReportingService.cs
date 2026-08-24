using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using log4net;

namespace BackOffice.Business
{
    /// <summary>
    /// Provides reporting data and aggregation logic.
    /// Uses direct SQL access (anti-pattern for business layer).
    /// </summary>
    public class ReportingService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReportingService));
        private static readonly string _connectionString = ConfigurationManager.ConnectionStrings["BackOfficeDB_Readonly"].ConnectionString;

        public DataTable GetDailyReport(DateTime startDate, DateTime endDate)
        {
            // Uses read-only connection for reports
            string sql = "EXEC sp_GetDailyReport @DateFrom = '" +
                startDate.ToString("yyyy-MM-dd") + "', @DateTo = '" +
                endDate.ToString("yyyy-MM-dd") + "'";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 300; // 5 minute timeout for reports
                    DataTable dt = new DataTable();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }

                    _log.InfoFormat("Daily report generated: {0} to {1}, {2} rows",
                        startDate.ToShortDateString(), endDate.ToShortDateString(), dt.Rows.Count);

                    return dt;
                }
            }
        }

        public DataTable GetDisputeAgingReport()
        {
            string sql = @"
                SELECT 
                    CASE 
                        WHEN DATEDIFF(DAY, CreatedDate, GETDATE()) <= 3 THEN '0-3 Days'
                        WHEN DATEDIFF(DAY, CreatedDate, GETDATE()) <= 7 THEN '4-7 Days'
                        WHEN DATEDIFF(DAY, CreatedDate, GETDATE()) <= 14 THEN '8-14 Days'
                        WHEN DATEDIFF(DAY, CreatedDate, GETDATE()) <= 30 THEN '15-30 Days'
                        ELSE '30+ Days'
                    END AS AgeBucket,
                    COUNT(*) AS DisputeCount,
                    SUM(Amount) AS TotalAmount,
                    AVG(Amount) AS AvgAmount
                FROM Disputes
                WHERE Status NOT IN ('Closed', 'Resolved')
                GROUP BY CASE 
                    WHEN DATEDIFF(DAY, CreatedDate, GETDATE()) <= 3 THEN '0-3 Days'
                    WHEN DATEDIFF(DAY, CreatedDate, GETDATE()) <= 7 THEN '4-7 Days'
                    WHEN DATEDIFF(DAY, CreatedDate, GETDATE()) <= 14 THEN '8-14 Days'
                    WHEN DATEDIFF(DAY, CreatedDate, GETDATE()) <= 30 THEN '15-30 Days'
                    ELSE '30+ Days'
                END
                ORDER BY MIN(DATEDIFF(DAY, CreatedDate, GETDATE()))";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 120;
                    DataTable dt = new DataTable();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                    return dt;
                }
            }
        }

        public DataTable GetMerchantVolumeReport(DateTime startDate, DateTime endDate, int topN = 20)
        {
            // Non-parameterized date values
            string sql = "SELECT TOP " + topN + " m.MerchantName, " +
                "COUNT(*) AS TransactionCount, " +
                "SUM(t.Amount) AS TotalVolume, " +
                "AVG(t.Amount) AS AvgTransaction, " +
                "SUM(CASE WHEN t.Status = 'Declined' THEN 1 ELSE 0 END) AS DeclineCount, " +
                "CAST(SUM(CASE WHEN t.Status = 'Declined' THEN 1 ELSE 0 END) AS FLOAT) / COUNT(*) * 100 AS DeclineRate " +
                "FROM Transactions t " +
                "INNER JOIN Merchants m ON t.MerchantId = m.MerchantId " +
                "WHERE t.TransactionDate BETWEEN '" + startDate.ToString("yyyy-MM-dd") + "' AND '" + endDate.ToString("yyyy-MM-dd") + "' " +
                "GROUP BY m.MerchantName " +
                "ORDER BY TotalVolume DESC";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 180;
                    DataTable dt = new DataTable();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                    return dt;
                }
            }
        }

        public DataTable GetChargebackTrendReport(DateTime startDate, DateTime endDate)
        {
            string sql = "SELECT CONVERT(VARCHAR(10), c.ChargebackDate, 120) AS ReportDate, " +
                "COUNT(*) AS ChargebackCount, " +
                "SUM(c.Amount) AS TotalAmount, " +
                "COUNT(CASE WHEN c.IsWon = 1 THEN 1 END) AS WonCount, " +
                "COUNT(CASE WHEN c.IsWon = 0 THEN 1 END) AS LostCount " +
                "FROM Chargebacks c " +
                "WHERE c.ChargebackDate BETWEEN '" + startDate.ToString("yyyy-MM-dd") + "' AND '" + endDate.ToString("yyyy-MM-dd") + "' " +
                "GROUP BY CONVERT(VARCHAR(10), c.ChargebackDate, 120) " +
                "ORDER BY ReportDate";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 120;
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
