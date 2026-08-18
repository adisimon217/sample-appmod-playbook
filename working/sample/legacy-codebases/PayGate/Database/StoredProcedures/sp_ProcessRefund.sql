-- =============================================
-- sp_ProcessRefund
-- Processes a refund against a previously captured/settled transaction.
-- Validates the original transaction and creates a refund record.
-- =============================================
CREATE PROCEDURE [dbo].[sp_ProcessRefund]
    @OriginalTransactionId  UNIQUEIDENTIFIER,
    @RefundAmount           DECIMAL(18,2),
    @Reason                 NVARCHAR(500) = NULL,
    @InitiatedBy            NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    
    DECLARE @RefundId UNIQUEIDENTIFIER = NEWID();
    DECLARE @OriginalAmount DECIMAL(18,2);
    DECLARE @OriginalStatus INT;
    DECLARE @MerchantId VARCHAR(15);
    DECLARE @CardNumber VARCHAR(19);
    DECLARE @CardBrand VARCHAR(50);
    DECLARE @TotalRefunded DECIMAL(18,2);
    
    -- Get original transaction
    SELECT 
        @OriginalAmount = Amount,
        @OriginalStatus = Status,
        @MerchantId = MerchantId,
        @CardNumber = CardNumber,
        @CardBrand = CardBrand
    FROM dbo.Transactions WITH (NOLOCK)
    WHERE TransactionId = @OriginalTransactionId;
    
    IF @OriginalAmount IS NULL
    BEGIN
        RAISERROR('Original transaction not found.', 16, 1);
        RETURN;
    END
    
    IF @OriginalStatus NOT IN (2, 7) -- Captured or Settled
    BEGIN
        RAISERROR('Cannot refund transaction in status %d. Must be Captured or Settled.', 16, 1, @OriginalStatus);
        RETURN;
    END
    
    -- Check total refunds don't exceed original
    SELECT @TotalRefunded = ISNULL(SUM(ABS(Amount)), 0)
    FROM dbo.Transactions WITH (NOLOCK)
    WHERE OriginalTransactionId = @OriginalTransactionId
      AND TransactionType = 4 -- Refund
      AND Status NOT IN (4, 5); -- Not failed or voided
    
    IF (@TotalRefunded + @RefundAmount) > @OriginalAmount
    BEGIN
        RAISERROR('Total refunds ($%s + $%s) would exceed original amount ($%s).', 16, 1, 
            @TotalRefunded, @RefundAmount, @OriginalAmount);
        RETURN;
    END
    
    BEGIN TRANSACTION;
    
    -- Create refund transaction
    INSERT INTO dbo.Transactions (
        TransactionId, MerchantId, Amount, Currency, CardNumber,
        TransactionType, Status, CreatedDate, ProcessedDate,
        OriginalTransactionId, ResponseCode, ResponseMessage,
        CardBrand, AuthorizationCode
    )
    VALUES (
        @RefundId, @MerchantId, -@RefundAmount, 'USD', @CardNumber,
        4, -- Refund type
        1, -- Authorized
        SYSUTCDATETIME(), SYSUTCDATETIME(),
        @OriginalTransactionId, '00', 'Refund: ' + ISNULL(@Reason, 'No reason provided'),
        @CardBrand, LEFT(REPLACE(NEWID(), '-', ''), 6)
    );
    
    -- Update original transaction status if fully refunded
    IF (@TotalRefunded + @RefundAmount) >= @OriginalAmount
    BEGIN
        UPDATE dbo.Transactions
        SET Status = 6 -- Refunded
        WHERE TransactionId = @OriginalTransactionId;
    END
    
    COMMIT TRANSACTION;
    
    -- Return refund details
    SELECT 
        @RefundId AS RefundTransactionId,
        @RefundAmount AS RefundAmount,
        @OriginalTransactionId AS OriginalTransactionId,
        '00' AS ResponseCode,
        'Refund processed successfully' AS ResponseMessage;
END
GO
