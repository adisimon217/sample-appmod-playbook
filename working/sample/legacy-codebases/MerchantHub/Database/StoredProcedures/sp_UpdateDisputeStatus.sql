-- =============================================
-- sp_UpdateDisputeStatus
-- Updates dispute status and records history
-- Created: 2019-01-20 by D. Kim
-- =============================================
CREATE PROCEDURE [dbo].[sp_UpdateDisputeStatus]
    @DisputeId INT,
    @NewStatus VARCHAR(20),
    @Resolution NVARCHAR(500) = NULL,
    @UpdatedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @OldStatus VARCHAR(20);
    DECLARE @MerchantId INT;

    SELECT @OldStatus = Status, @MerchantId = MerchantId
    FROM MH_Disputes
    WHERE DisputeId = @DisputeId;

    IF @OldStatus IS NULL
    BEGIN
        RAISERROR('Dispute not found.', 16, 1);
        RETURN;
    END

    BEGIN TRANSACTION;

    -- Update dispute
    UPDATE MH_Disputes
    SET Status = @NewStatus,
        Resolution = CASE WHEN @NewStatus IN ('Won', 'Lost', 'Expired') THEN @Resolution ELSE Resolution END,
        ResolvedDate = CASE WHEN @NewStatus IN ('Won', 'Lost', 'Expired') THEN GETDATE() ELSE ResolvedDate END
    WHERE DisputeId = @DisputeId;

    -- Record history
    INSERT INTO MH_DisputeHistory (DisputeId, [Action], Details, PerformedBy, ActionDate)
    VALUES (@DisputeId, 'StatusChange',
            'Status changed from ' + @OldStatus + ' to ' + @NewStatus + 
            CASE WHEN @Resolution IS NOT NULL THEN '. Resolution: ' + @Resolution ELSE '' END,
            @UpdatedBy, GETDATE());

    -- Audit log
    INSERT INTO MH_AuditLog (EntityType, EntityId, [Action], UserId, Details, CreatedDate)
    VALUES ('Dispute', @DisputeId, 'StatusUpdate', @UpdatedBy,
            'Dispute ' + CAST(@DisputeId AS VARCHAR) + ' status: ' + @OldStatus + ' -> ' + @NewStatus,
            GETDATE());

    COMMIT TRANSACTION;
END
GO
