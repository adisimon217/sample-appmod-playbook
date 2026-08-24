-- =============================================
-- sp_AuditLogInsert
-- Inserts an audit log entry
-- Created: 2018-01-15 by S. Patel
-- =============================================
CREATE PROCEDURE [dbo].[sp_AuditLogInsert]
    @EntityType VARCHAR(50),
    @EntityId INT,
    @Action VARCHAR(50),
    @UserId NVARCHAR(100),
    @Details NVARCHAR(MAX) = NULL,
    @IPAddress VARCHAR(45) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO MH_AuditLog (EntityType, EntityId, [Action], UserId, Details, CreatedDate, IPAddress)
    VALUES (@EntityType, @EntityId, @Action, @UserId, @Details, GETDATE(), @IPAddress);
END
GO
