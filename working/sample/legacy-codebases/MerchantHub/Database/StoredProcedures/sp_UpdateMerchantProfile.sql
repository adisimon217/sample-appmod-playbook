-- =============================================
-- sp_UpdateMerchantProfile
-- Updates merchant profile with audit trail
-- Created: 2018-05-01 by S. Patel
-- =============================================
CREATE PROCEDURE [dbo].[sp_UpdateMerchantProfile]
    @MerchantId INT,
    @BusinessName NVARCHAR(200),
    @DBA NVARCHAR(200) = NULL,
    @Address1 NVARCHAR(200),
    @Address2 NVARCHAR(200) = NULL,
    @City NVARCHAR(100),
    @State CHAR(2),
    @ZipCode VARCHAR(10),
    @Phone VARCHAR(20) = NULL,
    @Email NVARCHAR(200),
    @Website NVARCHAR(500) = NULL,
    @UpdatedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE MH_Merchants
    SET BusinessName = @BusinessName,
        DBA = @DBA,
        Address1 = @Address1,
        Address2 = @Address2,
        City = @City,
        [State] = @State,
        ZipCode = @ZipCode,
        Phone = @Phone,
        Email = @Email,
        Website = @Website,
        LastProfileUpdate = GETDATE(),
        ModifiedBy = @UpdatedBy,
        ModifiedDate = GETDATE()
    WHERE MerchantId = @MerchantId;

    -- Audit log
    INSERT INTO MH_AuditLog (EntityType, EntityId, [Action], UserId, Details, CreatedDate)
    VALUES ('Merchant', @MerchantId, 'ProfileUpdate', @UpdatedBy,
            'Profile updated: ' + @BusinessName, GETDATE());
END
GO
