-- =============================================
-- sp_UpdateTransactionStatus
-- Updates transaction status with timestamp tracking.
-- =============================================
CREATE PROCEDURE [dbo].[sp_UpdateTransactionStatus]
    @TransactionId  UNIQUEIDENTIFIER,
    @NewStatus      INT,
    @ResponseCode   VARCHAR(10) = NULL,
    @ResponseMessage NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE dbo.Transactions
    SET Status = @NewStatus,
        ResponseCode = ISNULL(@ResponseCode, ResponseCode),
        ResponseMessage = ISNULL(@ResponseMessage, ResponseMessage),
        ProcessedDate = CASE WHEN @NewStatus IN (1, 2) AND ProcessedDate IS NULL 
                             THEN SYSUTCDATETIME() ELSE ProcessedDate END,
        SettledDate = CASE WHEN @NewStatus = 7 THEN SYSUTCDATETIME() ELSE SettledDate END
    WHERE TransactionId = @TransactionId;
    
    IF @@ROWCOUNT = 0
        RAISERROR('Transaction %s not found.', 16, 1, @TransactionId);
END
GO
