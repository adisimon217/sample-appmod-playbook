CREATE PROCEDURE sp_SearchTransactions
    @SearchTerm NVARCHAR(200),
    @Status VARCHAR(30) = NULL,
    @MinAmount DECIMAL(18,2) = NULL,
    @MaxAmount DECIMAL(18,2) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        t.TransactionId,
        m.MerchantName,
        t.CardType,
        RIGHT(t.CardNumber, 4) AS CardLast4,
        t.Amount,
        t.TransactionDate,
        t.Status,
        t.ResponseCode,
        t.IsFlagged,
        t.FlagReason
    FROM Transactions t WITH (NOLOCK)
    INNER JOIN Merchants m ON t.MerchantId = m.MerchantId
    WHERE (
        CAST(t.TransactionId AS VARCHAR) LIKE @SearchTerm
        OR m.MerchantName LIKE @SearchTerm
        OR t.CardNumber LIKE @SearchTerm
        OR t.AuthorizationCode LIKE @SearchTerm
    )
    AND (@Status IS NULL OR t.Status = @Status)
    AND (@MinAmount IS NULL OR t.Amount >= @MinAmount)
    AND (@MaxAmount IS NULL OR t.Amount <= @MaxAmount)
    ORDER BY t.TransactionDate DESC;
END
GO
