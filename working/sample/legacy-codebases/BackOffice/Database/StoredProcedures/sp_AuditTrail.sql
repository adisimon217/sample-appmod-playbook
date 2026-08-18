CREATE PROCEDURE sp_AuditTrail
    @Action VARCHAR(100),
    @EntityType VARCHAR(50),
    @EntityId INT,
    @PerformedBy NVARCHAR(100),
    @Details NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO AuditTrail (Action, EntityType, EntityId, PerformedBy, Details, ActionDate)
    VALUES (@Action, @EntityType, @EntityId, @PerformedBy, @Details, GETDATE());
END
GO
