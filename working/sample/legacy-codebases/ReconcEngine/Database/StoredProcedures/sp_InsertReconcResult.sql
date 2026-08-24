-- Inserts a reconciliation match result
CREATE PROCEDURE [dbo].[sp_InsertReconcResult]
    @MatchResultId          UNIQUEIDENTIFIER,
    @SettlementRecordId     UNIQUEIDENTIFIER,
    @TransactionId          UNIQUEIDENTIFIER,
    @Status                 VARCHAR(50),
    @SettlementAmount       DECIMAL(18, 4),
    @TransactionAmount      DECIMAL(18, 4),
    @DiscrepancyAmount      DECIMAL(18, 4),
    @MatchedOn              DATETIME2(7),
    @MatchCriteria          VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[ReconcResults] 
        (MatchResultId, SettlementRecordId, TransactionId, Status, 
         SettlementAmount, TransactionAmount, DiscrepancyAmount, MatchedOn, MatchCriteria)
    VALUES 
        (@MatchResultId, @SettlementRecordId, @TransactionId, @Status,
         @SettlementAmount, @TransactionAmount, @DiscrepancyAmount, @MatchedOn, @MatchCriteria);

    -- If unmatched, add to retry queue
    IF @Status = 'Unmatched'
    BEGIN
        INSERT INTO [dbo].[RetryQueue] 
            (ReconcResultId, TransactionId, SettlementRecordId, NextRetryTime, FailureReason)
        VALUES 
            (@MatchResultId, @TransactionId, @SettlementRecordId, 
             DATEADD(MINUTE, 30, SYSUTCDATETIME()), 'Initial match failed');
    END
END
GO
