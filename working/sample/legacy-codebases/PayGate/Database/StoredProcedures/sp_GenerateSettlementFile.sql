-- =============================================
-- sp_GenerateSettlementFile
-- Gathers data for settlement file generation by network.
-- Returns dataset formatted for export to CSV/fixed-width.
-- =============================================
CREATE PROCEDURE [dbo].[sp_GenerateSettlementFile]
    @SettlementDate DATE,
    @Network        VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get all transactions eligible for settlement
    SELECT 
        t.TransactionId,
        t.MerchantId,
        m.BusinessName AS MerchantName,
        t.Amount,
        t.Currency,
        t.CardBin,
        t.CardLast4,
        t.CardBrand,
        t.AuthorizationCode,
        t.TransactionType,
        t.ProcessedDate,
        t.ProcessingFee,
        m.MccCode,
        m.SettlementSchedule,
        -- Running totals for file trailer
        SUM(t.Amount) OVER () AS BatchTotalAmount,
        COUNT(*) OVER () AS BatchTransactionCount,
        ROW_NUMBER() OVER (ORDER BY t.ProcessedDate) AS SequenceNumber
    FROM dbo.Transactions t WITH (NOLOCK)
    INNER JOIN dbo.Merchants m WITH (NOLOCK) ON t.MerchantId = m.MerchantId
    WHERE t.CardBrand = @Network
      AND t.Status = 2 -- Captured (ready for settlement)
      AND CAST(t.ProcessedDate AS DATE) = @SettlementDate
      AND t.TransactionType IN (1, 2, 3, 4) -- Auth, Capture, Sale, Refund
    ORDER BY t.ProcessedDate;
END
GO
