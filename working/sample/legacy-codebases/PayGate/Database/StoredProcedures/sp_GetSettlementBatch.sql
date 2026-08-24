-- =============================================
-- sp_GetSettlementBatch
-- Gets settlement batch details with transaction breakdown.
-- Uses CTE with windowing functions for running totals.
-- =============================================
CREATE PROCEDURE [dbo].[sp_GetSettlementBatch]
    @BatchId    UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Settlement summary
    SELECT 
        s.SettlementId,
        s.BatchId,
        s.Network,
        s.SettlementDate,
        s.TotalAmount,
        s.TransactionCount,
        s.Status,
        s.FileName,
        s.FileGeneratedDate,
        s.FileSentDate,
        s.ResponseReceivedDate,
        s.NetSettlementAmount,
        s.ChargebackAmount,
        s.ChargebackCount
    FROM dbo.Settlements s WITH (NOLOCK)
    WHERE s.BatchId = @BatchId;
    
    -- Transaction details with running total
    ;WITH BatchTransactions AS (
        SELECT 
            t.TransactionId,
            t.MerchantId,
            t.Amount,
            t.CardBrand,
            t.ProcessedDate,
            t.SettledDate,
            t.AuthorizationCode,
            t.ProcessingFee,
            SUM(t.Amount) OVER (ORDER BY t.ProcessedDate ROWS UNBOUNDED PRECEDING) AS RunningTotal,
            ROW_NUMBER() OVER (ORDER BY t.ProcessedDate) AS RowNum
        FROM dbo.Transactions t WITH (NOLOCK)
        WHERE t.BatchId = @BatchId
    )
    SELECT *
    FROM BatchTransactions
    ORDER BY RowNum;
    
    -- Merchant breakdown
    SELECT 
        t.MerchantId,
        m.BusinessName,
        COUNT(*) AS TransactionCount,
        SUM(t.Amount) AS TotalAmount,
        SUM(t.ProcessingFee) AS TotalFees
    FROM dbo.Transactions t WITH (NOLOCK)
    INNER JOIN dbo.Merchants m WITH (NOLOCK) ON t.MerchantId = m.MerchantId
    WHERE t.BatchId = @BatchId
    GROUP BY t.MerchantId, m.BusinessName
    ORDER BY TotalAmount DESC;
END
GO
