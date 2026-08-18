-- =============================================
-- trg_MH_Disputes_Audit
-- Audit trigger for dispute status changes
-- Created: 2019-01-25 by D. Kim
-- Fires on UPDATE to track dispute lifecycle
-- =============================================
CREATE TRIGGER [dbo].[trg_MH_Disputes_Audit]
ON [dbo].[MH_Disputes]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Track status changes
    INSERT INTO MH_AuditLog (EntityType, EntityId, [Action], UserId, Details, CreatedDate)
    SELECT 'Dispute', i.DisputeId, 'StatusChanged',
           ISNULL(SUSER_SNAME(), 'SYSTEM'),
           'Dispute ' + CAST(i.DisputeId AS VARCHAR) + ' (Case: ' + ISNULL(i.CaseNumber, 'N/A') + 
           ') status changed from ' + d.Status + ' to ' + i.Status +
           ' | Amount: $' + CAST(i.Amount AS VARCHAR) +
           ' | Merchant: ' + CAST(i.MerchantId AS VARCHAR),
           GETDATE()
    FROM inserted i
    INNER JOIN deleted d ON i.DisputeId = d.DisputeId
    WHERE i.Status != d.Status;

    -- Track response submissions
    INSERT INTO MH_AuditLog (EntityType, EntityId, [Action], UserId, Details, CreatedDate)
    SELECT 'Dispute', i.DisputeId, 'ResponseSubmitted',
           ISNULL(SUSER_SNAME(), 'SYSTEM'),
           'Merchant response submitted for dispute ' + CAST(i.DisputeId AS VARCHAR),
           GETDATE()
    FROM inserted i
    INNER JOIN deleted d ON i.DisputeId = d.DisputeId
    WHERE d.MerchantResponse IS NULL AND i.MerchantResponse IS NOT NULL;

    -- Alert on disputes resolved as Lost (financial impact)
    IF EXISTS (
        SELECT 1 FROM inserted i
        INNER JOIN deleted d ON i.DisputeId = d.DisputeId
        WHERE i.Status = 'Lost' AND d.Status != 'Lost'
    )
    BEGIN
        -- Insert notification for review (checked by monitoring job)
        INSERT INTO MH_AuditLog (EntityType, EntityId, [Action], UserId, Details, CreatedDate)
        SELECT 'Dispute', i.DisputeId, 'DisputeLost',
               'SYSTEM',
               'ALERT: Dispute lost - $' + CAST(i.Amount AS VARCHAR) + 
               ' | Merchant: ' + CAST(i.MerchantId AS VARCHAR) +
               ' | Case: ' + ISNULL(i.CaseNumber, 'N/A'),
               GETDATE()
        FROM inserted i
        INNER JOIN deleted d ON i.DisputeId = d.DisputeId
        WHERE i.Status = 'Lost' AND d.Status != 'Lost';
    END
END
GO
