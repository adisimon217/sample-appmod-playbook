-- ============================================================================
-- Quarterly Compliance Query
-- ============================================================================
-- Report: MAS PSN02 - Quarterly Compliance Report
-- Frequency: Quarterly (generated first Sunday after quarter end)
-- Data Source: PayGateDB via Linked Server [PAYGATE_SERVER]
--
-- Returns 4 result sets:
--   1. Compliance Checks Summary (AML/CFT alerts)
--   2. KYC Verification Status
--   3. Transaction Monitoring Alerts
--   4. Regulatory Threshold Breaches
--
-- Performance Notes:
--   - ComplianceAlerts.AlertDate is indexed
--   - KycRecords joined to Merchants (both indexed on MerchantID)
--   - Average execution time: 2-4 minutes per quarter
--
-- Last Modified: 2020-08-22 (added threshold breach section per MAS directive)
-- ============================================================================

DECLARE @StartDate DATETIME = '2024-01-01';
DECLARE @EndDate DATETIME = '2024-03-31 23:59:59';
DECLARE @Year INT = 2024;
DECLARE @Quarter INT = 1;

-- Result Set 1: AML/CFT Compliance Checks Summary
SELECT
    ca.AlertCategory,
    ca.Severity,
    COUNT(*) AS AlertCount,
    SUM(CASE WHEN ca.ResolutionStatus = 'Resolved' THEN 1 ELSE 0 END) AS ResolvedCount,
    SUM(CASE WHEN ca.ResolutionStatus = 'Pending' THEN 1 ELSE 0 END) AS PendingCount,
    SUM(CASE WHEN ca.EscalatedToSTR = 1 THEN 1 ELSE 0 END) AS EscalatedToStrCount
FROM [PAYGATE_SERVER].[PayGateDB].[dbo].[ComplianceAlerts] ca WITH (NOLOCK)
WHERE ca.AlertDate BETWEEN @StartDate AND @EndDate
GROUP BY ca.AlertCategory, ca.Severity
ORDER BY ca.Severity, AlertCount DESC;

-- Result Set 2: KYC Verification Status of Merchant Portfolio
SELECT
    k.VerificationStatus,
    k.RiskCategory,
    COUNT(DISTINCT m.MerchantID) AS MerchantCount,
    MIN(k.LastVerificationDate) AS OldestVerification,
    MAX(k.NextReviewDate) AS LatestReviewDue
FROM [PAYGATE_SERVER].[PayGateDB].[dbo].[KycRecords] k WITH (NOLOCK)
INNER JOIN [PAYGATE_SERVER].[PayGateDB].[dbo].[Merchants] m WITH (NOLOCK)
    ON k.MerchantID = m.MerchantID
WHERE m.IsActive = 1
GROUP BY k.VerificationStatus, k.RiskCategory
ORDER BY k.RiskCategory, k.VerificationStatus;

-- Result Set 3: Transaction Monitoring Alerts (effectiveness metrics)
SELECT
    MONTH(ca.AlertDate) AS AlertMonth,
    ca.AlertCategory,
    COUNT(*) AS TotalAlerts,
    AVG(DATEDIFF(hour, ca.AlertDate, ca.ResolutionDate)) AS AvgResolutionHours,
    SUM(CASE WHEN ca.IsFalsePositive = 1 THEN 1 ELSE 0 END) AS FalsePositiveCount
FROM [PAYGATE_SERVER].[PayGateDB].[dbo].[ComplianceAlerts] ca WITH (NOLOCK)
WHERE ca.AlertDate BETWEEN @StartDate AND @EndDate
GROUP BY MONTH(ca.AlertDate), ca.AlertCategory
ORDER BY AlertMonth, ca.AlertCategory;

-- Result Set 4: Regulatory Threshold Breaches
-- MAS threshold: Single transaction > SGD 20,000 requires enhanced monitoring
SELECT
    t.MerchantID,
    m.MerchantName,
    'Single Transaction > SGD 20,000' AS ThresholdType,
    COUNT(*) AS BreachCount,
    SUM(t.TransactionAmount) AS TotalValue
FROM [PAYGATE_SERVER].[PayGateDB].[dbo].[Transactions] t WITH (NOLOCK)
INNER JOIN [PAYGATE_SERVER].[PayGateDB].[dbo].[Merchants] m WITH (NOLOCK)
    ON t.MerchantID = m.MerchantID
WHERE t.TransactionDate BETWEEN @StartDate AND @EndDate
  AND t.TransactionAmount > 20000
  AND t.CurrencyCode = 'SGD'
GROUP BY t.MerchantID, m.MerchantName
HAVING COUNT(*) > 5  -- Only report merchants with repeated high-value transactions
ORDER BY BreachCount DESC;
