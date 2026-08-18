CREATE PROCEDURE sp_GetUserPermissions
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        up.UserPermissionId,
        up.PermissionName,
        up.GrantedBy,
        up.GrantedDate
    FROM UserPermissions up
    WHERE up.UserId = @UserId
    ORDER BY up.PermissionName;
END
GO
