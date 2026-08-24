-- Cleans up old reconciliation records beyond the retention period.
-- Called as part of nightly maintenance.
CREATE PROCEDURE [dbo].[sp_CleanupOldRecords]
    @RetentionDays  INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CutoffDate DATETIME2(7) = DATEADD(DAY, -@RetentionDays, SYSUTCDATETIME());

    -- Delete old match history first (child records)
    DELETE mh
    FROM [dbo].[MatchHistory] mh
    INNER JOIN [dbo].[ReconcResults] r ON mh.MatchResultId = r.MatchResultId
    WHERE r.MatchedOn < @CutoffDate;

    -- Delete old retry queue items
    DELETE FROM [dbo].[RetryQueue]
    WHERE CreatedAt < @CutoffDate
      AND Status IN ('Completed', 'PermanentlyFailed');

    -- Delete old discrepancies (resolved ones only)
    DELETE FROM [dbo].[Discrepancies]
    WHERE DetectedAt < @CutoffDate
      AND IsResolved = 1;

    -- Delete old reconciliation results
    DELETE FROM [dbo].[ReconcResults]
    WHERE MatchedOn < @CutoffDate;

    -- Delete old batch runs
    DELETE FROM [dbo].[BatchRuns]
    WHERE StartTime < @CutoffDate
      AND Status IN ('Completed', 'CompletedNoData', 'Failed');
END
GO
