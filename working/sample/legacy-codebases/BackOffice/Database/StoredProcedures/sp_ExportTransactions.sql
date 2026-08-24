CREATE PROCEDURE sp_ExportTransactions
    @DateFrom DATE,
    @DateTo DATE,
    @MerchantId INT = NULL,
    @Status VARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        t.TransactionId,
        m.MerchantName,
        m.MerchantId,
        t.CardType,
        RIGHT(t.CardNumber, 4) AS CardLast4,
        t.Amount,
        t.Currency,
        t.TransactionDate,
        t.ProcessedDate,
        t.Status,
        t.ResponseCode,
        t.AuthorizationCode,
        t.EntryMode,
        t.TerminalId,
        t.IsFlagged,
        t.FlagReason,
        t.BatchId
    FROM Transactions t
    INNER JOIN Merchants m ON t.MerchantId = m.MerchantId
    WHERE CAST(t.TransactionDate AS DATE) BETWEEN @DateFrom AND @DateTo
    AND (@MerchantId IS NULL OR t.MerchantId = @MerchantId)
    AND (@Status IS NULL OR t.Status = @Status)
    ORDER BY t.TransactionDate DESC;
END
GO
