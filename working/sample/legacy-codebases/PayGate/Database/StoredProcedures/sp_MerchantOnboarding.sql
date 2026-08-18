-- =============================================
-- sp_MerchantOnboarding
-- Creates a new merchant with initial configuration.
-- Generates API key and sets up default limits.
-- =============================================
CREATE PROCEDURE [dbo].[sp_MerchantOnboarding]
    @MerchantId             VARCHAR(15),
    @BusinessName           NVARCHAR(200),
    @TaxId                  VARCHAR(20) = NULL,
    @ContactEmail           NVARCHAR(200) = NULL,
    @ContactPhone           VARCHAR(20) = NULL,
    @MccCode                VARCHAR(10) = NULL,
    @SettlementSchedule     VARCHAR(20) = 'T+1',
    @MaxTransactionsPerHour INT = 1000
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    
    -- Check for duplicate
    IF EXISTS (SELECT 1 FROM dbo.Merchants WHERE MerchantId = @MerchantId)
    BEGIN
        RAISERROR('Merchant ID %s already exists.', 16, 1, @MerchantId);
        RETURN;
    END
    
    BEGIN TRANSACTION;
    
    -- Create merchant record
    INSERT INTO dbo.Merchants (
        MerchantId, BusinessName, TaxId, ContactEmail, ContactPhone,
        Status, CreatedDate, MccCode, SettlementSchedule, MaxTransactionsPerHour,
        RiskCategory
    )
    VALUES (
        @MerchantId, @BusinessName, @TaxId, @ContactEmail, @ContactPhone,
        0, -- PendingVerification
        SYSUTCDATETIME(), @MccCode, @SettlementSchedule, @MaxTransactionsPerHour,
        'Standard'
    );
    
    -- Generate initial API key
    DECLARE @ApiKeyId UNIQUEIDENTIFIER = NEWID();
    DECLARE @RawKey VARCHAR(64) = REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', '') 
                                 + REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', '');
    DECLARE @KeyPrefix VARCHAR(8) = LEFT(@RawKey, 8);
    
    INSERT INTO dbo.ApiKeys (ApiKeyId, MerchantId, ApiKey, KeyPrefix, IsActive, CreatedDate, Description)
    VALUES (@ApiKeyId, @MerchantId, @RawKey, @KeyPrefix, 1, SYSUTCDATETIME(), 'Initial onboarding key');
    
    -- Audit log
    INSERT INTO dbo.AuditLog (TableName, RecordId, Action, ChangedBy, NewValues)
    VALUES (
        'Merchants',
        @MerchantId,
        'ONBOARD',
        SUSER_SNAME(),
        '{"BusinessName":"' + @BusinessName + '","MccCode":"' + ISNULL(@MccCode, '') + '"}'
    );
    
    COMMIT TRANSACTION;
    
    -- Return created merchant info
    SELECT 
        @MerchantId AS MerchantId,
        @BusinessName AS BusinessName,
        0 AS Status,
        @KeyPrefix + '...' AS ApiKeyPrefix,
        'Merchant created successfully. API key generated.' AS Message;
END
GO
