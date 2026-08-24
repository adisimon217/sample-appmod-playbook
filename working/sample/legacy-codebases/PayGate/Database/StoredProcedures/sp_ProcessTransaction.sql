-- =============================================
-- sp_ProcessTransaction
-- Inserts a new transaction record and returns the result.
-- Called by the application layer for real-time transaction processing.
-- Optimized for high throughput (50K/hr target).
-- =============================================
CREATE PROCEDURE [dbo].[sp_ProcessTransaction]
    @TransactionId      UNIQUEIDENTIFIER,
    @MerchantId         VARCHAR(15),
    @Amount             DECIMAL(18,2),
    @Currency           VARCHAR(3),
    @CardNumber         VARCHAR(19),
    @CardExpiry         VARCHAR(7) = NULL,
    @CardholderName     NVARCHAR(100) = NULL,
    @TransactionType    INT,
    @AuthorizationCode  VARCHAR(50) = NULL,
    @IpAddress          VARCHAR(45) = NULL,
    @CardBrand          VARCHAR(50) = NULL,
    @CardBin            VARCHAR(6) = NULL,
    @CardLast4          VARCHAR(4) = NULL,
    @ProcessingFee      DECIMAL(18,2) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    
    DECLARE @ProcessedDate DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @ResponseCode VARCHAR(10) = '00';
    DECLARE @Status INT = 1; -- Authorized
    
    -- Validate merchant is active
    IF NOT EXISTS (SELECT 1 FROM dbo.Merchants WITH (NOLOCK) WHERE MerchantId = @MerchantId AND Status = 1)
    BEGIN
        SET @ResponseCode = '05'; -- Do not honor
        SET @Status = 3; -- Declined
    END
    
    -- Insert transaction
    INSERT INTO dbo.Transactions (
        TransactionId, MerchantId, Amount, Currency, CardNumber,
        CardExpiry, CardholderName, TransactionType, Status,
        CreatedDate, ProcessedDate, AuthorizationCode, ResponseCode,
        IpAddress, ProcessingFee, CardBrand, CardBin, CardLast4
    )
    VALUES (
        @TransactionId, @MerchantId, @Amount, @Currency, @CardNumber,
        @CardExpiry, @CardholderName, @TransactionType, @Status,
        SYSUTCDATETIME(), @ProcessedDate, @AuthorizationCode, @ResponseCode,
        @IpAddress, @ProcessingFee, @CardBrand, @CardBin, @CardLast4
    );
    
    -- Return result
    SELECT 
        @TransactionId AS TransactionId,
        @Status AS Status,
        @ResponseCode AS ResponseCode,
        @AuthorizationCode AS AuthorizationCode,
        @ProcessedDate AS ProcessedDate,
        DATEDIFF(MILLISECOND, SYSUTCDATETIME(), @ProcessedDate) AS LatencyMs;
END
GO
