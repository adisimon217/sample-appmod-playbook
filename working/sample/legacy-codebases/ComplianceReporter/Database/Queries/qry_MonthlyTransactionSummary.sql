-- ============================================================================
-- Monthly Transaction Summary Query
-- ============================================================================
-- Report: MAS PSN02 - Form 1A (Monthly Transaction Summary)
-- Frequency: Monthly (generated first Sunday after month end)
-- Data Source: PayGateDB via Linked Server [PAYGATE_SERVER]
--
-- Performance Notes:
--   - TransactionDate is indexed (IX_Transactions_TransactionDate)
--   - Query typically completes in 45-90 seconds for ~12M monthly rows
--   - DO NOT remove the TransactionStatus filter - it uses a filtered index
--
-- Last Modified: 2019-03-15 (added high-value transaction flag)
-- ============================================================================

DECLARE @Year INT = 2024;
DECLARE @Month INT = 1;

-- Result Set 1: Transaction Summary by Payment Type
SELECT
    pt.PaymentTypeName,
    COUNT(t.TransactionID) AS TransactionCount,
    SUM(t.TransactionAmount) AS TransactionValue,
    t.CurrencyCode,
    CASE
        WHEN t.IsInternational = 1 THEN 'International'
        ELSE 'Domestic'
    END AS TransactionType
FROM [PAYGATE_SERVER].[PayGateDB].[dbo].[Transactions] t WITH (NOLOCK)
INNER JOIN [PAYGATE_SERVER].[PayGateDB].[dbo].[PaymentTypes] pt
    ON t.PaymentTypeID = pt.PaymentTypeID
WHERE YEAR(t.TransactionDate) = @Year
  AND MONTH(t.TransactionDate) = @Month
  AND t.TransactionStatus IN ('Completed', 'Settled')
GROUP BY
    pt.PaymentTypeName,
    t.CurrencyCode,
    t.IsInternational
ORDER BY TransactionValue DESC;

-- Result Set 2: Cross-Border Transaction Breakdown (MAS Form 1B)
SELECT
    t.OriginCountry,
    t.DestinationCountry,
    t.CurrencyCode,
    COUNT(t.TransactionID) AS TransactionCount,
    SUM(t.TransactionAmount) AS TransactionValue,
    SUM(CASE WHEN t.TransactionAmount > 20000 THEN 1 ELSE 0 END) AS HighValueCount
FROM [PAYGATE_SERVER].[PayGateDB].[dbo].[Transactions] t WITH (NOLOCK)
WHERE YEAR(t.TransactionDate) = @Year
  AND MONTH(t.TransactionDate) = @Month
  AND t.IsInternational = 1
  AND t.TransactionStatus IN ('Completed', 'Settled')
GROUP BY
    t.OriginCountry,
    t.DestinationCountry,
    t.CurrencyCode
ORDER BY TransactionValue DESC;
