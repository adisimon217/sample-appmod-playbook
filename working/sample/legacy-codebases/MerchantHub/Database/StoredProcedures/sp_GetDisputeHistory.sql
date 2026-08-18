-- =============================================
-- sp_GetDisputeHistory
-- Returns full dispute history with related transaction info
-- Created: 2019-03-01 by D. Kim
-- =============================================
CREATE PROCEDURE [dbo].[sp_GetDisputeHistory]
    @DisputeId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Dispute details with transaction info
    SELECT 
        d.DisputeId, d.CaseNumber, d.ReasonCode, d.ReasonDescription,
        d.Amount, d.Status, d.FiledDate, d.ResponseDeadline,
        d.RespondedDate, d.ResolvedDate, d.MerchantResponse, d.Resolution,
        t.TransactionId, t.TransactionDate, t.Amount as TransactionAmount,
        t.CardType, t.Last4Digits, t.CustomerName,
        m.BusinessName as MerchantName, m.MerchantNumber
    FROM MH_Disputes d
    INNER JOIN MH_Transactions t ON d.TransactionId = t.TransactionId
    INNER JOIN MH_Merchants m ON d.MerchantId = m.MerchantId
    WHERE d.DisputeId = @DisputeId;

    -- History timeline
    SELECT 
        HistoryId, [Action], Details, PerformedBy, ActionDate
    FROM MH_DisputeHistory
    WHERE DisputeId = @DisputeId
    ORDER BY ActionDate DESC;

    -- Related documents
    SELECT 
        DocumentId, FileName, DocumentType, FileSize, UploadedDate, UploadedBy
    FROM MH_DisputeDocuments
    WHERE DisputeId = @DisputeId
    ORDER BY UploadedDate DESC;
END
GO
