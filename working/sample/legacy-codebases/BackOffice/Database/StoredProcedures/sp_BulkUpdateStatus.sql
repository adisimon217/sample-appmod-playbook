CREATE PROCEDURE sp_BulkUpdateStatus
    @TransactionIds VARCHAR(MAX),  -- Comma-separated list of IDs
    @NewStatus VARCHAR(30),
    @UpdatedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    -- Parse comma-separated IDs into a table variable
    DECLARE @IdTable TABLE (TransactionId BIGINT);

    INSERT INTO @IdTable (TransactionId)
    SELECT CAST(value AS BIGINT)
    FROM STRING_SPLIT(@TransactionIds, ',')
    WHERE RTRIM(value) != '';

    -- Update all matching transactions
    UPDATE t
    SET t.Status = @NewStatus,
        t.Notes = ISNULL(t.Notes, '') + CHAR(13) + 
                  CONVERT(VARCHAR(20), GETDATE(), 120) + ' - Status changed to ' + @NewStatus + ' by ' + @UpdatedBy
    FROM Transactions t
    INNER JOIN @IdTable ids ON t.TransactionId = ids.TransactionId;

    DECLARE @RowCount INT = @@ROWCOUNT;

    -- Audit trail for bulk operation
    INSERT INTO AuditTrail (Action, EntityType, EntityId, PerformedBy, Details, ActionDate)
    VALUES ('BULK_STATUS_UPDATE', 'Transaction', 0, @UpdatedBy, 
            'Updated ' + CAST(@RowCount AS VARCHAR) + ' transactions to status: ' + @NewStatus,
            GETDATE());

    SELECT @RowCount AS RowsAffected;
END
GO
