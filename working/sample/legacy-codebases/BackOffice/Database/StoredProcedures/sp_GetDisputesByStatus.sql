CREATE PROCEDURE sp_GetDisputesByStatus
    @Status VARCHAR(30),
    @AssignedTo NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        d.DisputeId,
        d.TransactionId,
        m.MerchantName,
        d.CardholderName,
        d.Amount,
        d.DisputeReason,
        d.Status,
        d.AssignedTo,
        d.CreatedDate,
        d.DaysOpen,
        d.EscalatedBy,
        d.EscalatedDate,
        CASE 
            WHEN d.DaysOpen > 30 THEN 'Critical'
            WHEN d.DaysOpen > 14 THEN 'High'
            WHEN d.DaysOpen > 7 THEN 'Medium'
            ELSE 'Low'
        END AS Priority
    FROM Disputes d
    INNER JOIN Transactions t ON d.TransactionId = t.TransactionId
    INNER JOIN Merchants m ON t.MerchantId = m.MerchantId
    WHERE (@Status = 'AllOpen' AND d.Status NOT IN ('Closed', 'Resolved'))
       OR (@Status != 'AllOpen' AND d.Status = @Status)
    AND (@AssignedTo IS NULL OR d.AssignedTo = @AssignedTo)
    ORDER BY 
        CASE d.Status 
            WHEN 'Escalated' THEN 1 
            WHEN 'New' THEN 2 
            WHEN 'UnderReview' THEN 3 
            ELSE 4 
        END,
        d.CreatedDate ASC;
END
GO
