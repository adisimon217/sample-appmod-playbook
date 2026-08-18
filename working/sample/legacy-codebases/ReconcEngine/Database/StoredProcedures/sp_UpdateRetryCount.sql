-- Updates the retry count and status after a retry attempt
CREATE PROCEDURE [dbo].[sp_UpdateRetryCount]
    @ReconcResultId     UNIQUEIDENTIFIER,
    @Status             VARCHAR(50),
    @RetryCount         INT,
    @LastRetryTime      DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;

    -- Update the reconciliation result
    UPDATE [dbo].[ReconcResults]
    SET Status = @Status,
        RetryCount = @RetryCount,
        LastRetryTime = @LastRetryTime
    WHERE MatchResultId = @ReconcResultId;

    -- Update or close the retry queue item
    IF @Status IN ('Matched', 'PermanentlyFailed')
    BEGIN
        UPDATE [dbo].[RetryQueue]
        SET Status = @Status,
            RetryCount = @RetryCount,
            LastAttemptTime = @LastRetryTime
        WHERE ReconcResultId = @ReconcResultId;
    END
    ELSE
    BEGIN
        -- Still pending - update retry count and schedule next attempt
        UPDATE [dbo].[RetryQueue]
        SET RetryCount = @RetryCount,
            LastAttemptTime = @LastRetryTime,
            NextRetryTime = DATEADD(MINUTE, 30, @LastRetryTime)
        WHERE ReconcResultId = @ReconcResultId;
    END

    -- Log to match history
    INSERT INTO [dbo].[MatchHistory] (MatchResultId, AttemptNumber, Status, AttemptTime)
    VALUES (@ReconcResultId, @RetryCount, @Status, @LastRetryTime);
END
GO
