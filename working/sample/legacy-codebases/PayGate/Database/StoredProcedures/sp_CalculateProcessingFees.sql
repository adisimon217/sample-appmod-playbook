-- =============================================
-- sp_CalculateProcessingFees
-- Calculates and applies processing fees for a date range.
-- Uses CROSS APPLY to get merchant-specific fee rates.
-- =============================================
CREATE PROCEDURE [dbo].[sp_CalculateProcessingFees]
    @StartDate  DATE,
    @EndDate    DATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Update processing fees for transactions that don't have one yet
    UPDATE t
    SET t.ProcessingFee = feeCalc.CalculatedFee
    FROM dbo.Transactions t
    CROSS APPLY (
        SELECT 
            CASE 
                -- Use merchant-specific rate if configured
                WHEN m.ProcessingFeePercent IS NOT NULL 
                THEN ROUND(t.Amount * m.ProcessingFeePercent + 0.30, 2)
                -- Otherwise use network-based rates
                WHEN t.CardBrand = 'Visa' THEN ROUND(t.Amount * 0.0195 + 0.30, 2)
                WHEN t.CardBrand = 'Mastercard' THEN ROUND(t.Amount * 0.0200 + 0.30, 2)
                WHEN t.CardBrand = 'Amex' THEN ROUND(t.Amount * 0.0295 + 0.35, 2)
                ELSE ROUND(t.Amount * 0.0250 + 0.30, 2)
            END AS CalculatedFee
        FROM dbo.Merchants m
        WHERE m.MerchantId = t.MerchantId
    ) feeCalc
    WHERE CAST(t.CreatedDate AS DATE) BETWEEN @StartDate AND @EndDate
      AND t.ProcessingFee IS NULL
      AND t.Status IN (1, 2, 7); -- Authorized, Captured, Settled
    
    -- Return fee summary
    SELECT 
        t.CardBrand,
        COUNT(*) AS TransactionCount,
        SUM(t.Amount) AS TotalVolume,
        SUM(t.ProcessingFee) AS TotalFees,
        AVG(t.ProcessingFee) AS AvgFee,
        CAST(SUM(t.ProcessingFee) / NULLIF(SUM(t.Amount), 0) * 100 AS DECIMAL(5,3)) AS EffectiveRate
    FROM dbo.Transactions t WITH (NOLOCK)
    WHERE CAST(t.CreatedDate AS DATE) BETWEEN @StartDate AND @EndDate
      AND t.ProcessingFee IS NOT NULL
    GROUP BY t.CardBrand
    ORDER BY TotalVolume DESC;
END
GO
