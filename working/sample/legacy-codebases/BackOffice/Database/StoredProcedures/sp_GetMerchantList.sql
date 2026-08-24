CREATE PROCEDURE sp_GetMerchantList
    @Status VARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        m.MerchantId,
        m.MerchantName,
        m.LegalName,
        m.BusinessType,
        m.MccCode,
        m.Status,
        m.RiskTier,
        m.CreatedDate,
        m.ContactName,
        m.Email,
        COUNT(t.TransactionId) AS TotalTransactions,
        ISNULL(SUM(t.Amount), 0) AS TotalVolume,
        MAX(t.TransactionDate) AS LastTransaction
    FROM Merchants m
    LEFT JOIN Transactions t ON m.MerchantId = t.MerchantId
    WHERE (@Status IS NULL OR m.Status = @Status)
    GROUP BY m.MerchantId, m.MerchantName, m.LegalName, m.BusinessType, 
             m.MccCode, m.Status, m.RiskTier, m.CreatedDate, m.ContactName, m.Email
    ORDER BY m.MerchantName;
END
GO
