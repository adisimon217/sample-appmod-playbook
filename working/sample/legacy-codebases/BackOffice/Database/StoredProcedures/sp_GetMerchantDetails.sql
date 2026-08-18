CREATE PROCEDURE sp_GetMerchantDetails
    @MerchantId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        m.*,
        (SELECT COUNT(*) FROM Transactions WHERE MerchantId = m.MerchantId) AS TotalTransactions,
        (SELECT ISNULL(SUM(Amount), 0) FROM Transactions WHERE MerchantId = m.MerchantId) AS TotalVolume,
        (SELECT COUNT(*) FROM Transactions WHERE MerchantId = m.MerchantId AND Status = 'Declined') AS DeclinedCount,
        (SELECT COUNT(*) FROM Chargebacks cb 
         INNER JOIN Transactions t ON cb.TransactionId = t.TransactionId 
         WHERE t.MerchantId = m.MerchantId) AS ChargebackCount,
        (SELECT MAX(TransactionDate) FROM Transactions WHERE MerchantId = m.MerchantId) AS LastTransaction
    FROM Merchants m
    WHERE m.MerchantId = @MerchantId;
END
GO
