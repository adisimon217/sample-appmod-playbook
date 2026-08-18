-- ============================================================================
-- Annual Audit Query
-- ============================================================================
-- Report: MAS PSN02 - Annual Audit Report
-- Frequency: Annual (generated in first week of January for previous year)
-- Data Source: PayGateDB via Linked Server [PAYGATE_SERVER]
--
-- Returns 5 result sets:
--   1. Annual Transaction Summary (month-by-month)
--   2. Merchant Portfolio Summary
--   3. Compliance Incidents by Quarter
--   4. STR Filings for the Year
--   5. Regulatory Actions Received
--
-- WARNING: This query scans full year of data (~150M rows in Transactions).
--          Expected execution time: 8-15 minutes.
--          Schedule during off-peak hours only (Sunday 02:00-05:00 SGT).
--
-- Last Modified: 2021-01-10 (added STR filings section per MAS audit requirement)
-- ============================================================================

DECLARE @Year INT = 2023;
DECLARE @StartDate DATETIME = '2023-01-01 00:00:00';
DECLARE @EndDate DATETIME = '2023-12-31 23:59:59';

-- Result Set 1: Annual Transaction Summary by Month
SELECT
    MONTH(t.TransactionDate) AS TransactionMonth,
    COUNT(t.TransactionID) AS TransactionCount,
    SUM(t.TransactionAmount) AS TransactionValue,
    COUNT(DISTINCT t.MerchantID) AS ActiveMerchants,
    SUM(CASE WHEN t.IsInternational = 1 THEN t.TransactionAmount ELSE 0 END) AS InternationalValue
FROM [PAYGATE_SERVER].[PayGateDB].[dbo].[Transactions] t WITH (NOLOCK)
WHERE t.TransactionDate BETWEEN @StartDate AND @EndDate
  AND t.TransactionStatus IN ('Completed', 'Settled')
GROUP BY MONTH(t.TransactionDate)
ORDER BY TransactionMonth;

-- Result Set 2: Merchant Portfolio Summary at Year End
SELECT
    m.MerchantCategory,
    m.RiskRating,
    COUNT(*) AS MerchantCount,
    SUM(CASE WHEN m.IsActive = 1 THEN 1 ELSE 0 END) AS ActiveCount,
    SUM(CASE WHEN m.OnboardedDate >= @StartDate THEN 1 ELSE 0 END) AS NewThisYear
FROM [PAYGATE_SERVER].[PayGateDB].[dbo].[Merchants] m WITH (NOLOCK)
GROUP BY m.MerchantCategory, m.RiskRating
ORDER BY m.RiskRating, m.MerchantCategory;

-- Result Set 3: Compliance Incidents by Quarter
SELECT
    ca.AlertCategory,
    ca.Severity,
    DATEPART(QUARTER, ca.AlertDate) AS IncidentQuarter,
    COUNT(*) AS IncidentCount,
    SUM(CASE WHEN ca.ResolutionStatus = 'Resolved' THEN 1 ELSE 0 END) AS Resolved,
    SUM(CASE WHEN ca.EscalatedToSTR = 1 THEN 1 ELSE 0 END) AS EscalatedCount
FROM [PAYGATE_SERVER].[PayGateDB].[dbo].[ComplianceAlerts] ca WITH (NOLOCK)
WHERE ca.AlertDate BETWEEN @StartDate AND @EndDate
GROUP BY ca.AlertCategory, ca.Severity, DATEPART(QUARTER, ca.AlertDate)
ORDER BY IncidentQuarter, ca.Severity;

-- Result Set 4: Suspicious Transaction Reports (STR) Filed
SELECT
    str.FilingDate,
    str.ReportCategory,
    str.FilingStatus,
    str.ReferenceNumber,
    m.MerchantName,
    str.InvolvedAmount
FROM [PAYGATE_SERVER].[PayGateDB].[dbo].[SuspiciousTransactionReports] str WITH (NOLOCK)
LEFT JOIN [PAYGATE_SERVER].[PayGateDB].[dbo].[Merchants] m WITH (NOLOCK)
    ON str.MerchantID = m.MerchantID
WHERE YEAR(str.FilingDate) = @Year
ORDER BY str.FilingDate;

-- Result Set 5: Regulatory Actions Received (if any)
SELECT
    ActionDate,
    ActionType,
    ActionDescription,
    ResolutionStatus,
    ResolutionDate
FROM [PAYGATE_SERVER].[PayGateDB].[dbo].[RegulatoryActions] WITH (NOLOCK)
WHERE YEAR(ActionDate) = @Year
ORDER BY ActionDate;
