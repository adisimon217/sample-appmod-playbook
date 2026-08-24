-- =============================================
-- sp_GetChargebackReport
-- Generates chargeback report by merchant for compliance monitoring.
-- Merchants exceeding 1% chargeback rate are flagged for review.
-- Uses CROSS APPLY and windowing functions.
-- =============================================
CREATE PROCEDURE [dbo].[sp_GetChargebackReport]
    @StartDate  DATE,
    @EndDate    DATE
AS
BEGIN
    SET NOCOUNT ON;
    
    ;WITH MerchantChargebacks AS (
        SELECT 
            t.MerchantId,
            COUNT(*) AS ChargebackCount,
            SUM(ABS(t.Amount)) AS ChargebackAmount
        FROM dbo.Transactions t WITH (NOLOCK)
        WHERE CAST(t.CreatedDate AS DATE) BETWEEN @StartDate AND @EndDate
          AND t.Status = 8 -- Chargeback
        GROUP BY t.MerchantId
    ),
    MerchantVolume AS (
        SELECT 
            t.MerchantId,
            COUNT(*) AS TransactionCount,
            SUM(t.Amount) AS TotalVolume
        FROM dbo.Transactions t WITH (NOLOCK)
        WHERE CAST(t.CreatedDate AS DATE) BETWEEN @StartDate AND @EndDate
          AND t.TransactionType IN (1, 2, 3) -- Not refunds
          AND t.Status NOT IN (4, 5) -- Not failed/voided
        GROUP BY t.MerchantId
    )
    SELECT 
        mc.MerchantId,
        m.BusinessName,
        m.MccCode,
        mc.ChargebackCount,
        mc.ChargebackAmount,
        mv.TransactionCount,
        mv.TotalVolume,
        CAST(mc.ChargebackCount AS FLOAT) / NULLIF(mv.TransactionCount, 0) * 100 AS ChargebackRate,
        CASE 
            WHEN CAST(mc.ChargebackCount AS FLOAT) / NULLIF(mv.TransactionCount, 0) > 0.01 
            THEN 'HIGH RISK - Exceeds 1% threshold'
            WHEN CAST(mc.ChargebackCount AS FLOAT) / NULLIF(mv.TransactionCount, 0) > 0.005 
            THEN 'WARNING - Approaching threshold'
            ELSE 'Normal'
        END AS RiskFlag
    FROM MerchantChargebacks mc
    INNER JOIN dbo.Merchants m WITH (NOLOCK) ON mc.MerchantId = m.MerchantId
    LEFT JOIN MerchantVolume mv ON mc.MerchantId = mv.MerchantId
    ORDER BY ChargebackRate DESC;
END
GO
