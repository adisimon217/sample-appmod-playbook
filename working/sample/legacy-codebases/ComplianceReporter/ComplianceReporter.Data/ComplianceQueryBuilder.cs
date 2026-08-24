using System;
using System.Text;

namespace ComplianceReporter.Data
{
    /// <summary>
    /// Builds SQL queries for compliance reporting.
    /// All queries reference PayGateDB tables via Linked Server.
    /// 
    /// Linked Server reference format:
    ///   [PAYGATE_SERVER].[PayGateDB].[dbo].[TableName]
    /// 
    /// WARNING: These queries have been tuned for performance over years.
    /// Do not modify without consulting the DBA team (John Tan, ext. 4521).
    /// The PayGateDB Transactions table has 800M+ rows.
    /// </summary>
    public class ComplianceQueryBuilder
    {
        private readonly PayGateDataAccess _dataAccess;

        public ComplianceQueryBuilder(PayGateDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        /// <summary>
        /// Builds the monthly transaction summary query.
        /// Returns two result sets: TransactionSummary, CrossBorderTransactions
        /// </summary>
        public string BuildMonthlyTransactionSummaryQuery(int year, int month)
        {
            string txnTable = _dataAccess.GetLinkedTableName("Transactions");
            string merchantTable = _dataAccess.GetLinkedTableName("Merchants");
            string settlementTable = _dataAccess.GetLinkedTableName("Settlements");

            StringBuilder query = new StringBuilder();

            // Result Set 1: Transaction Summary by Payment Type
            query.AppendLine("-- Monthly Transaction Summary by Payment Type");
            query.AppendLine("-- MAS Report: PSN02-Form1A");
            query.AppendFormat("SELECT pt.PaymentTypeName,{0}", Environment.NewLine);
            query.AppendFormat("       COUNT(t.TransactionID) AS TransactionCount,{0}", Environment.NewLine);
            query.AppendFormat("       SUM(t.TransactionAmount) AS TransactionValue,{0}", Environment.NewLine);
            query.AppendFormat("       t.CurrencyCode,{0}", Environment.NewLine);
            query.AppendFormat("       CASE WHEN t.IsInternational = 1 THEN 'International' ELSE 'Domestic' END AS TransactionType{0}", Environment.NewLine);
            query.AppendFormat("FROM {0} t{1}", txnTable, Environment.NewLine);
            query.AppendFormat("INNER JOIN [{0}].[{1}].[dbo].[PaymentTypes] pt ON t.PaymentTypeID = pt.PaymentTypeID{2}",
                "PAYGATE_SERVER", "PayGateDB", Environment.NewLine);
            query.AppendLine("WHERE YEAR(t.TransactionDate) = @Year");
            query.AppendLine("  AND MONTH(t.TransactionDate) = @Month");
            query.AppendLine("  AND t.TransactionStatus IN ('Completed', 'Settled')");
            query.AppendLine("GROUP BY pt.PaymentTypeName, t.CurrencyCode, t.IsInternational");
            query.AppendLine("ORDER BY TransactionValue DESC;");
            query.AppendLine();

            // Result Set 2: Cross-Border Transaction Breakdown
            query.AppendLine("-- Cross-Border Transaction Breakdown");
            query.AppendLine("-- MAS Report: PSN02-Form1B");
            query.AppendFormat("SELECT t.OriginCountry,{0}", Environment.NewLine);
            query.AppendFormat("       t.DestinationCountry,{0}", Environment.NewLine);
            query.AppendFormat("       t.CurrencyCode,{0}", Environment.NewLine);
            query.AppendFormat("       COUNT(t.TransactionID) AS TransactionCount,{0}", Environment.NewLine);
            query.AppendFormat("       SUM(t.TransactionAmount) AS TransactionValue,{0}", Environment.NewLine);
            query.AppendFormat("       SUM(CASE WHEN t.TransactionAmount > 20000 THEN 1 ELSE 0 END) AS HighValueCount{0}", Environment.NewLine);
            query.AppendFormat("FROM {0} t{1}", txnTable, Environment.NewLine);
            query.AppendLine("WHERE YEAR(t.TransactionDate) = @Year");
            query.AppendLine("  AND MONTH(t.TransactionDate) = @Month");
            query.AppendLine("  AND t.IsInternational = 1");
            query.AppendLine("  AND t.TransactionStatus IN ('Completed', 'Settled')");
            query.AppendLine("GROUP BY t.OriginCountry, t.DestinationCountry, t.CurrencyCode");
            query.AppendLine("ORDER BY TransactionValue DESC;");

            return query.ToString();
        }

        /// <summary>
        /// Builds the quarterly compliance query.
        /// Returns four result sets: ComplianceChecks, KycStatus, AlertsSummary, ThresholdBreaches
        /// </summary>
        public string BuildQuarterlyComplianceQuery(int year, int quarter)
        {
            string txnTable = _dataAccess.GetLinkedTableName("Transactions");
            string merchantTable = _dataAccess.GetLinkedTableName("Merchants");
            string alertsTable = _dataAccess.GetLinkedTableName("ComplianceAlerts");
            string kycTable = _dataAccess.GetLinkedTableName("KycRecords");

            StringBuilder query = new StringBuilder();

            // Result Set 1: Compliance Checks Summary
            query.AppendLine("-- Quarterly Compliance Checks (AML/CFT)");
            query.AppendFormat("SELECT ca.AlertCategory,{0}", Environment.NewLine);
            query.AppendFormat("       ca.Severity,{0}", Environment.NewLine);
            query.AppendFormat("       COUNT(*) AS AlertCount,{0}", Environment.NewLine);
            query.AppendFormat("       SUM(CASE WHEN ca.ResolutionStatus = 'Resolved' THEN 1 ELSE 0 END) AS ResolvedCount,{0}", Environment.NewLine);
            query.AppendFormat("       SUM(CASE WHEN ca.ResolutionStatus = 'Pending' THEN 1 ELSE 0 END) AS PendingCount,{0}", Environment.NewLine);
            query.AppendFormat("       SUM(CASE WHEN ca.EscalatedToSTR = 1 THEN 1 ELSE 0 END) AS EscalatedToStrCount{0}", Environment.NewLine);
            query.AppendFormat("FROM {0} ca{1}", alertsTable, Environment.NewLine);
            query.AppendLine("WHERE ca.AlertDate BETWEEN @StartDate AND @EndDate");
            query.AppendLine("GROUP BY ca.AlertCategory, ca.Severity");
            query.AppendLine("ORDER BY ca.Severity, AlertCount DESC;");
            query.AppendLine();

            // Result Set 2: KYC Status of Merchant Portfolio
            query.AppendLine("-- KYC Verification Status");
            query.AppendFormat("SELECT k.VerificationStatus,{0}", Environment.NewLine);
            query.AppendFormat("       k.RiskCategory,{0}", Environment.NewLine);
            query.AppendFormat("       COUNT(DISTINCT m.MerchantID) AS MerchantCount,{0}", Environment.NewLine);
            query.AppendFormat("       MIN(k.LastVerificationDate) AS OldestVerification,{0}", Environment.NewLine);
            query.AppendFormat("       MAX(k.NextReviewDate) AS LatestReviewDue{0}", Environment.NewLine);
            query.AppendFormat("FROM {0} k{1}", kycTable, Environment.NewLine);
            query.AppendFormat("INNER JOIN {0} m ON k.MerchantID = m.MerchantID{1}", merchantTable, Environment.NewLine);
            query.AppendLine("WHERE m.IsActive = 1");
            query.AppendLine("GROUP BY k.VerificationStatus, k.RiskCategory");
            query.AppendLine("ORDER BY k.RiskCategory, k.VerificationStatus;");
            query.AppendLine();

            // Result Set 3: Alerts Summary (for monitoring effectiveness)
            query.AppendLine("-- Transaction Monitoring Alerts Summary");
            query.AppendFormat("SELECT MONTH(ca.AlertDate) AS AlertMonth,{0}", Environment.NewLine);
            query.AppendFormat("       ca.AlertCategory,{0}", Environment.NewLine);
            query.AppendFormat("       COUNT(*) AS TotalAlerts,{0}", Environment.NewLine);
            query.AppendFormat("       AVG(DATEDIFF(hour, ca.AlertDate, ca.ResolutionDate)) AS AvgResolutionHours,{0}", Environment.NewLine);
            query.AppendFormat("       SUM(CASE WHEN ca.IsFalsePositive = 1 THEN 1 ELSE 0 END) AS FalsePositiveCount{0}", Environment.NewLine);
            query.AppendFormat("FROM {0} ca{1}", alertsTable, Environment.NewLine);
            query.AppendLine("WHERE ca.AlertDate BETWEEN @StartDate AND @EndDate");
            query.AppendLine("GROUP BY MONTH(ca.AlertDate), ca.AlertCategory");
            query.AppendLine("ORDER BY AlertMonth, ca.AlertCategory;");
            query.AppendLine();

            // Result Set 4: Threshold Breaches
            query.AppendLine("-- Regulatory Threshold Breaches");
            query.AppendFormat("SELECT t.MerchantID,{0}", Environment.NewLine);
            query.AppendFormat("       m.MerchantName,{0}", Environment.NewLine);
            query.AppendFormat("       'Single Transaction > SGD 20,000' AS ThresholdType,{0}", Environment.NewLine);
            query.AppendFormat("       COUNT(*) AS BreachCount,{0}", Environment.NewLine);
            query.AppendFormat("       SUM(t.TransactionAmount) AS TotalValue{0}", Environment.NewLine);
            query.AppendFormat("FROM {0} t{1}", txnTable, Environment.NewLine);
            query.AppendFormat("INNER JOIN {0} m ON t.MerchantID = m.MerchantID{1}", merchantTable, Environment.NewLine);
            query.AppendLine("WHERE t.TransactionDate BETWEEN @StartDate AND @EndDate");
            query.AppendLine("  AND t.TransactionAmount > 20000");
            query.AppendLine("  AND t.CurrencyCode = 'SGD'");
            query.AppendLine("GROUP BY t.MerchantID, m.MerchantName");
            query.AppendLine("HAVING COUNT(*) > 5");
            query.AppendLine("ORDER BY BreachCount DESC;");

            return query.ToString();
        }

        /// <summary>
        /// Builds the annual audit query.
        /// Returns five result sets for the comprehensive annual report.
        /// </summary>
        public string BuildAnnualAuditQuery(int year)
        {
            string txnTable = _dataAccess.GetLinkedTableName("Transactions");
            string merchantTable = _dataAccess.GetLinkedTableName("Merchants");
            string alertsTable = _dataAccess.GetLinkedTableName("ComplianceAlerts");
            string kycTable = _dataAccess.GetLinkedTableName("KycRecords");
            string strTable = _dataAccess.GetLinkedTableName("SuspiciousTransactionReports");

            StringBuilder query = new StringBuilder();

            // Result Set 1: Annual Transaction Summary
            query.AppendLine("-- Annual Transaction Summary by Month");
            query.AppendFormat("SELECT MONTH(t.TransactionDate) AS TransactionMonth,{0}", Environment.NewLine);
            query.AppendFormat("       COUNT(t.TransactionID) AS TransactionCount,{0}", Environment.NewLine);
            query.AppendFormat("       SUM(t.TransactionAmount) AS TransactionValue,{0}", Environment.NewLine);
            query.AppendFormat("       COUNT(DISTINCT t.MerchantID) AS ActiveMerchants,{0}", Environment.NewLine);
            query.AppendFormat("       SUM(CASE WHEN t.IsInternational = 1 THEN t.TransactionAmount ELSE 0 END) AS InternationalValue{0}", Environment.NewLine);
            query.AppendFormat("FROM {0} t{1}", txnTable, Environment.NewLine);
            query.AppendLine("WHERE t.TransactionDate BETWEEN @StartDate AND @EndDate");
            query.AppendLine("  AND t.TransactionStatus IN ('Completed', 'Settled')");
            query.AppendLine("GROUP BY MONTH(t.TransactionDate)");
            query.AppendLine("ORDER BY TransactionMonth;");
            query.AppendLine();

            // Result Set 2: Merchant Portfolio Summary
            query.AppendLine("-- Merchant Portfolio at Year End");
            query.AppendFormat("SELECT m.MerchantCategory,{0}", Environment.NewLine);
            query.AppendFormat("       m.RiskRating,{0}", Environment.NewLine);
            query.AppendFormat("       COUNT(*) AS MerchantCount,{0}", Environment.NewLine);
            query.AppendFormat("       SUM(CASE WHEN m.IsActive = 1 THEN 1 ELSE 0 END) AS ActiveCount,{0}", Environment.NewLine);
            query.AppendFormat("       SUM(CASE WHEN m.OnboardedDate >= @StartDate THEN 1 ELSE 0 END) AS NewThisYear{0}", Environment.NewLine);
            query.AppendFormat("FROM {0} m{1}", merchantTable, Environment.NewLine);
            query.AppendLine("GROUP BY m.MerchantCategory, m.RiskRating");
            query.AppendLine("ORDER BY m.RiskRating, m.MerchantCategory;");
            query.AppendLine();

            // Result Set 3: Compliance Incidents
            query.AppendLine("-- Compliance Incidents for the Year");
            query.AppendFormat("SELECT ca.AlertCategory,{0}", Environment.NewLine);
            query.AppendFormat("       ca.Severity,{0}", Environment.NewLine);
            query.AppendFormat("       DATEPART(QUARTER, ca.AlertDate) AS IncidentQuarter,{0}", Environment.NewLine);
            query.AppendFormat("       COUNT(*) AS IncidentCount,{0}", Environment.NewLine);
            query.AppendFormat("       SUM(CASE WHEN ca.ResolutionStatus = 'Resolved' THEN 1 ELSE 0 END) AS Resolved,{0}", Environment.NewLine);
            query.AppendFormat("       SUM(CASE WHEN ca.EscalatedToSTR = 1 THEN 1 ELSE 0 END) AS EscalatedCount{0}", Environment.NewLine);
            query.AppendFormat("FROM {0} ca{1}", alertsTable, Environment.NewLine);
            query.AppendLine("WHERE ca.AlertDate BETWEEN @StartDate AND @EndDate");
            query.AppendLine("GROUP BY ca.AlertCategory, ca.Severity, DATEPART(QUARTER, ca.AlertDate)");
            query.AppendLine("ORDER BY IncidentQuarter, ca.Severity;");
            query.AppendLine();

            // Result Set 4: STR Filings
            query.AppendLine("-- Suspicious Transaction Reports Filed");
            query.AppendFormat("SELECT str.FilingDate,{0}", Environment.NewLine);
            query.AppendFormat("       str.ReportCategory,{0}", Environment.NewLine);
            query.AppendFormat("       str.FilingStatus,{0}", Environment.NewLine);
            query.AppendFormat("       str.ReferenceNumber,{0}", Environment.NewLine);
            query.AppendFormat("       m.MerchantName,{0}", Environment.NewLine);
            query.AppendFormat("       str.InvolvedAmount{0}", Environment.NewLine);
            query.AppendFormat("FROM {0} str{1}", strTable, Environment.NewLine);
            query.AppendFormat("LEFT JOIN {0} m ON str.MerchantID = m.MerchantID{1}", merchantTable, Environment.NewLine);
            query.AppendLine("WHERE YEAR(str.FilingDate) = @Year");
            query.AppendLine("ORDER BY str.FilingDate;");
            query.AppendLine();

            // Result Set 5: Regulatory Actions (if any)
            query.AppendLine("-- Regulatory Actions Received");
            query.AppendLine("SELECT ActionDate, ActionType, ActionDescription, ResolutionStatus, ResolutionDate");
            query.AppendFormat("FROM [{0}].[{1}].[dbo].[RegulatoryActions]{2}",
                "PAYGATE_SERVER", "PayGateDB", Environment.NewLine);
            query.AppendLine("WHERE YEAR(ActionDate) = @Year");
            query.AppendLine("ORDER BY ActionDate;");

            return query.ToString();
        }
    }
}
