-- =============================================
-- sp_GetMerchantSettlement
-- Gets settlement summary for a specific merchant.
-- =============================================
CREATE PROCEDURE [dbo].[sp_GetMerchantSettlement]
    @MerchantId     VARCHAR(15),
    @StartDate      DATE,
    @EndDate        DATE
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        CAST(t.SettledDate AS DATE) AS SettlementDate,
        t.CardBrand AS Network,
        COUNT(*) AS TransactionCount,
        SUM(t.Amount) AS GrossAmount,
        SUM(ISNULL(t.ProcessingFee, 0)) AS TotalFees,
        SUM(t.Amount) - SUM(ISNULL(t.ProcessingFee, 0)) AS NetAmount
    FROM dbo.Transactions t WITH (NOLOCK)
    WHERE t.MerchantId = @MerchantId
      AND t.Status = 7 -- Settled
      AND CAST(t.SettledDate AS DATE) BETWEEN @StartDate AND @EndDate
    GROUP BY CAST(t.SettledDate AS DATE), t.CardBrand
    ORDER BY SettlementDate DESC, Network;
END
GO
