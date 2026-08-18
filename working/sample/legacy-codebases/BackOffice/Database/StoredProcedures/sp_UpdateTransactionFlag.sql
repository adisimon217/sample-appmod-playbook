CREATE PROCEDURE sp_UpdateTransactionFlag
    @TransactionId BIGINT,
    @FlaggedBy NVARCHAR(100),
    @FlagReason NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Transactions
    SET IsFlagged = 1,
        FlaggedBy = @FlaggedBy,
        FlagReason = @FlagReason,
        FlaggedDate = GETDATE(),
        Status = 'Flagged'
    WHERE TransactionId = @TransactionId;

    -- Log to audit trail
    INSERT INTO AuditTrail (Action, EntityType, EntityId, PerformedBy, Details, ActionDate)
    VALUES ('FLAG_TRANSACTION', 'Transaction', @TransactionId, @FlaggedBy, 
            'Reason: ' + @FlagReason, GETDATE());
END
GO
