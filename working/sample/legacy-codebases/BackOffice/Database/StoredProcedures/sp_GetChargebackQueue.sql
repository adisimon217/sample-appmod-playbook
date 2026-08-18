CREATE PROCEDURE sp_GetChargebackQueue
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        c.ChargebackId,
        c.TransactionId,
        m.MerchantName,
        c.Amount,
        c.ChargebackDate,
        c.ReasonCode,
        c.ResponseDueDate,
        c.ResponseSubmitted,
        c.Status,
        DATEDIFF(DAY, GETDATE(), c.ResponseDueDate) AS DaysUntilDue,
        CASE 
            WHEN DATEDIFF(DAY, GETDATE(), c.ResponseDueDate) < 0 THEN 'OVERDUE'
            WHEN DATEDIFF(DAY, GETDATE(), c.ResponseDueDate) <= 3 THEN 'URGENT'
            WHEN DATEDIFF(DAY, GETDATE(), c.ResponseDueDate) <= 7 THEN 'DUE SOON'
            ELSE 'OK'
        END AS Urgency
    FROM Chargebacks c
    INNER JOIN Transactions t ON c.TransactionId = t.TransactionId
    INNER JOIN Merchants m ON t.MerchantId = m.MerchantId
    WHERE c.Status = 'Open' AND c.ResponseSubmitted = 0
    ORDER BY c.ResponseDueDate ASC;
END
GO
