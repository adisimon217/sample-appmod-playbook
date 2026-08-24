-- =============================================
-- trg_MH_Users_Audit
-- Audit trigger for user table changes
-- Created: 2018-03-10 by S. Patel
-- Fires on INSERT, UPDATE, DELETE to track all user changes
-- =============================================
CREATE TRIGGER [dbo].[trg_MH_Users_Audit]
ON [dbo].[MH_Users]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Action VARCHAR(10);

    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
        SET @Action = 'UPDATE';
    ELSE IF EXISTS (SELECT * FROM inserted)
        SET @Action = 'INSERT';
    ELSE
        SET @Action = 'DELETE';

    -- Log inserts
    IF @Action = 'INSERT'
    BEGIN
        INSERT INTO MH_AuditLog (EntityType, EntityId, [Action], UserId, Details, CreatedDate)
        SELECT 'User', i.UserId, 'Created',
               ISNULL(SUSER_SNAME(), 'SYSTEM'),
               'User created: ' + i.Username + ' (Role: ' + i.Role + ', Merchant: ' + CAST(i.MerchantId AS VARCHAR) + ')',
               GETDATE()
        FROM inserted i;
    END

    -- Log updates
    IF @Action = 'UPDATE'
    BEGIN
        -- Track role changes
        INSERT INTO MH_AuditLog (EntityType, EntityId, [Action], UserId, Details, CreatedDate)
        SELECT 'User', i.UserId, 'RoleChanged',
               ISNULL(SUSER_SNAME(), 'SYSTEM'),
               'Role changed from ' + d.Role + ' to ' + i.Role,
               GETDATE()
        FROM inserted i
        INNER JOIN deleted d ON i.UserId = d.UserId
        WHERE i.Role != d.Role;

        -- Track activation/deactivation
        INSERT INTO MH_AuditLog (EntityType, EntityId, [Action], UserId, Details, CreatedDate)
        SELECT 'User', i.UserId,
               CASE WHEN i.IsActive = 1 THEN 'Activated' ELSE 'Deactivated' END,
               ISNULL(SUSER_SNAME(), 'SYSTEM'),
               'User ' + i.Username + CASE WHEN i.IsActive = 1 THEN ' activated' ELSE ' deactivated' END,
               GETDATE()
        FROM inserted i
        INNER JOIN deleted d ON i.UserId = d.UserId
        WHERE i.IsActive != d.IsActive;

        -- Track password changes
        INSERT INTO MH_AuditLog (EntityType, EntityId, [Action], UserId, Details, CreatedDate)
        SELECT 'User', i.UserId, 'PasswordChanged',
               ISNULL(SUSER_SNAME(), 'SYSTEM'),
               'Password changed for user: ' + i.Username,
               GETDATE()
        FROM inserted i
        INNER JOIN deleted d ON i.UserId = d.UserId
        WHERE i.PasswordHash != d.PasswordHash;
    END

    -- Log deletions
    IF @Action = 'DELETE'
    BEGIN
        INSERT INTO MH_AuditLog (EntityType, EntityId, [Action], UserId, Details, CreatedDate)
        SELECT 'User', d.UserId, 'Deleted',
               ISNULL(SUSER_SNAME(), 'SYSTEM'),
               'User deleted: ' + d.Username + ' (Merchant: ' + CAST(d.MerchantId AS VARCHAR) + ')',
               GETDATE()
        FROM deleted d;
    END
END
GO
