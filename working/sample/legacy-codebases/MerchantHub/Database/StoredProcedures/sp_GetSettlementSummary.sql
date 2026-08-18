-- =============================================
-- sp_GetSettlementSummary
-- Returns daily settlement summary for a merchant
-- Created: 2018-04-15 by S. Patel
-- =============================================
CREATE PROCEDURE [dbo].[sp_GetSettlementSummary]
    @MerchantId INT,
    @StartDate DATETIME2,
    @EndDate DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        CAST(t.TransactionDate AS DATE) as SettlementDate,
        t.BatchNumber,
        COUNT(*) as TransactionCount,
        SUM(t.Amount) as GrossAmount,
        SUM(ISNULL(t.Fee, 0)) as TotalFees,
        SUM(t.Amount - ISNULL(t.Fee, 0)) as NetAmount,
        SUM(CASE WHEN t.Status = 'Refunded' THEN t.RefundAmount ELSE 0 END) as RefundAmount,
        SUM(CASE WHEN t.Status = 'Chargeback' THEN t.Amount ELSE 0 END) as ChargebackAmount
    FROM MH_Transactions t WITH (NOLOCK)
    WHERE t.MerchantId = @MerchantId
      AND t.TransactionDate >= @StartDate
      AND t.TransactionDate < @EndDate
      AND t.Status IN ('Approved', 'Refunded', 'Chargeback')
    GROUP BY CAST(t.TransactionDate AS DATE), t.BatchNumber
    ORDER BY SettlementDate DESC, BatchNumber;
END
GO
