-- =============================================
-- sp_GetTransactionDetails
-- Gets full transaction details including related transactions (refunds, etc.)
-- Uses CROSS APPLY to get related data.
-- =============================================
CREATE PROCEDURE [dbo].[sp_GetTransactionDetails]
    @TransactionId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Main transaction
    SELECT 
        t.*,
        m.BusinessName AS MerchantName,
        m.Status AS MerchantStatus,
        m.MccCode
    FROM dbo.Transactions t WITH (NOLOCK)
    INNER JOIN dbo.Merchants m WITH (NOLOCK) ON t.MerchantId = m.MerchantId
    WHERE t.TransactionId = @TransactionId;
    
    -- Related transactions (refunds, voids against this transaction)
    SELECT 
        rt.TransactionId AS RelatedTransactionId,
        rt.TransactionType,
        rt.Amount,
        rt.Status,
        rt.CreatedDate,
        rt.AuthorizationCode
    FROM dbo.Transactions rt WITH (NOLOCK)
    WHERE rt.OriginalTransactionId = @TransactionId;
    
    -- Settlement information (if settled)
    SELECT 
        s.SettlementId,
        s.BatchId,
        s.Network,
        s.SettlementDate,
        s.Status AS SettlementStatus
    FROM dbo.Settlements s WITH (NOLOCK)
    CROSS APPLY (
        SELECT TOP 1 t.BatchId
        FROM dbo.Transactions t WITH (NOLOCK)
        WHERE t.TransactionId = @TransactionId
          AND t.BatchId IS NOT NULL
    ) txn
    WHERE s.BatchId = txn.BatchId;
END
GO
