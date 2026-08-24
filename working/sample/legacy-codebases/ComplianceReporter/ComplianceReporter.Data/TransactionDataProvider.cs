using System;
using System.Data;
using System.Data.SqlClient;

namespace ComplianceReporter.Data
{
    /// <summary>
    /// Provides transaction and compliance data from PayGateDB for report generation.
    /// All data is read-only and accessed via Linked Server queries.
    /// 
    /// Data sources in PayGateDB:
    /// - dbo.Transactions (core transaction log)
    /// - dbo.Merchants (merchant details)
    /// - dbo.Settlements (settlement batches)
    /// - dbo.ComplianceAlerts (AML/CFT alerts)
    /// - dbo.KycRecords (KYC verification status)
    /// </summary>
    public class TransactionDataProvider
    {
        private readonly PayGateDataAccess _dataAccess;

        public TransactionDataProvider(PayGateDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        /// <summary>
        /// Gets monthly transaction summary data for the MAS report.
        /// Returns a DataSet with TransactionSummary and CrossBorderTransactions tables.
        /// </summary>
        public DataSet GetMonthlyTransactionSummary(int year, int month)
        {
            ComplianceQueryBuilder queryBuilder = new ComplianceQueryBuilder(_dataAccess);
            string query = queryBuilder.BuildMonthlyTransactionSummaryQuery(year, month);

            string[] tableNames = new string[]
            {
                "TransactionSummary",
                "CrossBorderTransactions"
            };

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Year", SqlDbType.Int) { Value = year },
                new SqlParameter("@Month", SqlDbType.Int) { Value = month }
            };

            DataSet result = _dataAccess.ExecuteQueryMultiResult(query, parameters, tableNames);

            // Add computed columns for the report
            if (result.Tables.Contains("TransactionSummary"))
            {
                AddComputedSummaryColumns(result.Tables["TransactionSummary"]);
            }

            return result;
        }

        /// <summary>
        /// Gets quarterly compliance data for the MAS quarterly report.
        /// Returns DataSet with ComplianceChecks, KycStatus, AlertsSummary, ThresholdBreaches tables.
        /// </summary>
        public DataSet GetQuarterlyComplianceData(int year, int quarter)
        {
            ComplianceQueryBuilder queryBuilder = new ComplianceQueryBuilder(_dataAccess);
            string query = queryBuilder.BuildQuarterlyComplianceQuery(year, quarter);

            string[] tableNames = new string[]
            {
                "ComplianceChecks",
                "KycStatus",
                "AlertsSummary",
                "ThresholdBreaches"
            };

            // Calculate quarter date range
            int startMonth = ((quarter - 1) * 3) + 1;
            DateTime quarterStart = new DateTime(year, startMonth, 1);
            DateTime quarterEnd = quarterStart.AddMonths(3).AddDays(-1);

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@StartDate", SqlDbType.DateTime) { Value = quarterStart },
                new SqlParameter("@EndDate", SqlDbType.DateTime) { Value = quarterEnd },
                new SqlParameter("@Year", SqlDbType.Int) { Value = year },
                new SqlParameter("@Quarter", SqlDbType.Int) { Value = quarter }
            };

            DataSet result = _dataAccess.ExecuteQueryMultiResult(query, parameters, tableNames);
            return result;
        }

        /// <summary>
        /// Gets annual audit data for the MAS annual report.
        /// Returns comprehensive DataSet with full-year transaction and compliance data.
        /// </summary>
        public DataSet GetAnnualAuditData(int year)
        {
            ComplianceQueryBuilder queryBuilder = new ComplianceQueryBuilder(_dataAccess);
            string query = queryBuilder.BuildAnnualAuditQuery(year);

            string[] tableNames = new string[]
            {
                "AnnualTransactionSummary",
                "MerchantPortfolio",
                "ComplianceIncidents",
                "StrFilings",
                "RegulatoryActions"
            };

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Year", SqlDbType.Int) { Value = year },
                new SqlParameter("@StartDate", SqlDbType.DateTime) { Value = new DateTime(year, 1, 1) },
                new SqlParameter("@EndDate", SqlDbType.DateTime) { Value = new DateTime(year, 12, 31, 23, 59, 59) }
            };

            DataSet result = _dataAccess.ExecuteQueryMultiResult(query, parameters, tableNames);
            return result;
        }

        /// <summary>
        /// Adds computed columns needed by the report template.
        /// These calculations were previously done in the .rdlc expressions
        /// but moved here in 2018 for better performance.
        /// </summary>
        private void AddComputedSummaryColumns(DataTable summaryTable)
        {
            if (!summaryTable.Columns.Contains("TotalVolumeFormatted"))
            {
                summaryTable.Columns.Add("TotalVolumeFormatted", typeof(string));
            }

            if (!summaryTable.Columns.Contains("PercentageOfTotal"))
            {
                summaryTable.Columns.Add("PercentageOfTotal", typeof(decimal));
            }

            // Calculate total for percentage computation
            decimal grandTotal = 0;
            foreach (DataRow row in summaryTable.Rows)
            {
                if (row["TransactionValue"] != DBNull.Value)
                {
                    grandTotal += Convert.ToDecimal(row["TransactionValue"]);
                }
            }

            // Compute formatted values
            foreach (DataRow row in summaryTable.Rows)
            {
                if (row["TransactionValue"] != DBNull.Value)
                {
                    decimal value = Convert.ToDecimal(row["TransactionValue"]);
                    row["TotalVolumeFormatted"] = value.ToString("N2");
                    row["PercentageOfTotal"] = (grandTotal > 0) ?
                        Math.Round((value / grandTotal) * 100, 2) : 0;
                }
            }
        }
    }
}
