-- Gets items from the retry queue that are eligible for retry
CREATE PROCEDURE [dbo].[sp_GetPendingRetries]
    @MaxRetryAttempts   INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT rq.ReconcResultId,
           rq.TransactionId,
           rq.SettlementRecordId,
           rq.RetryCount,
           rq.LastAttemptTime,
           rq.FailureReason
    FROM [dbo].[RetryQueue] rq
    WHERE rq.Status = 'Pending'
      AND rq.RetryCount < @MaxRetryAttempts
      AND rq.NextRetryTime <= SYSUTCDATETIME()
    ORDER BY rq.NextRetryTime ASC;
END
GO
