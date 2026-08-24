CREATE PROCEDURE sp_CreateMerchant
    @MerchantName NVARCHAR(200),
    @LegalName NVARCHAR(300),
    @TaxId VARCHAR(20),
    @BusinessType VARCHAR(50),
    @MccCode VARCHAR(10),
    @AnnualVolume DECIMAL(18,2),
    @ContactName NVARCHAR(200),
    @Email VARCHAR(200),
    @Phone VARCHAR(30),
    @Address NVARCHAR(500),
    @City VARCHAR(100),
    @State VARCHAR(50),
    @Zip VARCHAR(20),
    @BankName NVARCHAR(200),
    @RoutingNumber VARCHAR(9),
    @AccountNumber VARCHAR(20),
    @AccountType VARCHAR(20),
    @RiskTier VARCHAR(20),
    @ChargebackLimit DECIMAL(5,2),
    @RiskNotes NVARCHAR(MAX),
    @CreatedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    -- Check for duplicate
    IF EXISTS (SELECT 1 FROM Merchants WHERE TaxId = @TaxId)
    BEGIN
        RAISERROR('A merchant with this Tax ID already exists.', 16, 1);
        RETURN;
    END

    INSERT INTO Merchants (
        MerchantName, LegalName, TaxId, BusinessType, MccCode, AnnualVolume,
        ContactName, Email, Phone, Address, City, State, Zip,
        BankName, RoutingNumber, AccountNumber, AccountType,
        RiskTier, ChargebackLimit, RiskNotes, Status, CreatedBy, CreatedDate
    )
    VALUES (
        @MerchantName, @LegalName, @TaxId, @BusinessType, @MccCode, @AnnualVolume,
        @ContactName, @Email, @Phone, @Address, @City, @State, @Zip,
        @BankName, @RoutingNumber, @AccountNumber, @AccountType,
        @RiskTier, @ChargebackLimit, @RiskNotes, 'Pending', @CreatedBy, GETDATE()
    );

    DECLARE @NewId INT = SCOPE_IDENTITY();

    -- Log audit
    INSERT INTO AuditTrail (Action, EntityType, EntityId, PerformedBy, Details)
    VALUES ('CREATE_MERCHANT', 'Merchant', @NewId, @CreatedBy, 
            'Created merchant: ' + @MerchantName + ' (' + @BusinessType + ')');

    SELECT @NewId AS MerchantId;
END
GO
