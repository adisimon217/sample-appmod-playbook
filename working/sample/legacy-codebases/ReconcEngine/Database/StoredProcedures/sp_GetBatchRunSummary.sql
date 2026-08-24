-- Gets a summary view of a batch run with aggregated statistics
CREATE PROCEDURE [dbo].[sp_GetBatchRunSummary]
    @BatchRunId     UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT br.BatchRunId,
           CAST(br.StartTime AS DATE) AS RunDate,
           br.Status,
           DATEDIFF(MILLISECOND, br.StartTime, ISNULL(br.EndTime, SYSUTCDATETIME())) AS DurationMs,
           br.MatchedCount,
           br.DiscrepancyCount,
           br.FailedCount,
           ISNULL(SUM(CASE WHEN r.Status = 'Matched' THEN r.SettlementAmount ELSE 0 END), 0) AS TotalSettlementAmount,
           ISNULL(SUM(CASE WHEN r.Status = 'Matched' THEN r.TransactionAmount ELSE 0 END), 0) AS TotalTransactionAmount,
           ISNULL(SUM(ABS(r.DiscrepancyAmount)), 0) AS TotalDiscrepancyAmount
    FROM [dbo].[BatchRuns] br
    LEFT JOIN [dbo].[ReconcResults] r ON br.BatchRunId = r.BatchRunId
    WHERE br.BatchRunId = @BatchRunId
    GROUP BY br.BatchRunId, br.StartTime, br.EndTime, br.Status, 
             br.MatchedCount, br.DiscrepancyCount, br.FailedCount;
END
GO
