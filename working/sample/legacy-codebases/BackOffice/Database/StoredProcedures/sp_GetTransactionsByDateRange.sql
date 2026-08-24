CREATE PROCEDURE sp_GetTransactionsByDateRange
    @StartDate DATETIME,
    @EndDate DATETIME,
    @MerchantFilter NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        t.TransactionId,
        m.MerchantName,
        t.CardType,
        RIGHT(t.CardNumber, 4) AS CardLast4,
        t.Amount,
        t.Currency,
        t.TransactionDate,
        t.ProcessedDate,
        t.Status,
        t.ResponseCode,
        t.AuthorizationCode,
        t.IsFlagged,
        t.FlaggedBy,
        t.FlagReason,
        t.EntryMode,
        t.TerminalId
    FROM Transactions t WITH (NOLOCK)
    INNER JOIN Merchants m ON t.MerchantId = m.MerchantId
    WHERE t.TransactionDate BETWEEN @StartDate AND @EndDate
    AND (@MerchantFilter IS NULL OR m.MerchantName LIKE '%' + @MerchantFilter + '%')
    ORDER BY t.TransactionDate DESC;
END
GO
