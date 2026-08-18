-- =============================================
-- sp_UpdateMerchantStatus
-- Updates merchant status with audit trail.
-- =============================================
CREATE PROCEDURE [dbo].[sp_UpdateMerchantStatus]
    @MerchantId VARCHAR(15),
    @Status     INT,
    @Reason     NVARCHAR(500) = NULL,
    @UpdatedBy  NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @OldStatus INT;
    
    SELECT @OldStatus = Status FROM dbo.Merchants WHERE MerchantId = @MerchantId;
    
    IF @OldStatus IS NULL
    BEGIN
        RAISERROR('Merchant %s not found.', 16, 1, @MerchantId);
        RETURN;
    END
    
    UPDATE dbo.Merchants
    SET Status = @Status,
        SuspendedDate = CASE WHEN @Status = 2 THEN SYSUTCDATETIME() ELSE SuspendedDate END,
        SuspensionReason = CASE WHEN @Status = 2 THEN @Reason ELSE SuspensionReason END,
        SuspendedBy = CASE WHEN @Status = 2 THEN @UpdatedBy ELSE SuspendedBy END,
        ActivatedDate = CASE WHEN @Status = 1 AND @OldStatus != 1 THEN SYSUTCDATETIME() ELSE ActivatedDate END
    WHERE MerchantId = @MerchantId;
    
    -- Audit log entry
    INSERT INTO dbo.AuditLog (TableName, RecordId, Action, ChangedBy, OldValues, NewValues)
    VALUES (
        'Merchants',
        @MerchantId,
        'STATUS_CHANGE',
        ISNULL(@UpdatedBy, SUSER_SNAME()),
        '{"Status":' + CAST(@OldStatus AS VARCHAR) + '}',
        '{"Status":' + CAST(@Status AS VARCHAR) + ',"Reason":"' + ISNULL(@Reason, '') + '"}'
    );
END
GO
