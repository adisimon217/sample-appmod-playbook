-- Generates a daily reconciliation summary report for a given date
CREATE PROCEDURE [dbo].[sp_GetDailyReconcReport]
    @ReportDate     DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT br.BatchRunId,
           br.StartTime,
           br.EndTime,
           br.Status,
           br.MatchedCount,
           br.DiscrepancyCount,
           br.FailedCount,
           DATEDIFF(SECOND, br.StartTime, br.EndTime) AS DurationSeconds,
           (SELECT COUNT(*) FROM [dbo].[Discrepancies] d 
            WHERE d.BatchRunId = br.BatchRunId AND d.Type = 'AmountMismatch') AS AmountMismatches,
           (SELECT COUNT(*) FROM [dbo].[Discrepancies] d 
            WHERE d.BatchRunId = br.BatchRunId AND d.Type = 'MissingTransaction') AS MissingTransactions,
           (SELECT COUNT(*) FROM [dbo].[Discrepancies] d 
            WHERE d.BatchRunId = br.BatchRunId AND d.Type = 'MissingSettlement') AS MissingSettlements,
           (SELECT COUNT(*) FROM [dbo].[Discrepancies] d 
            WHERE d.BatchRunId = br.BatchRunId AND d.Type = 'DuplicateSettlement') AS DuplicateSettlements,
           (SELECT ISNULL(SUM(ABS(d.DiscrepancyAmount)), 0) FROM [dbo].[Discrepancies] d 
            WHERE d.BatchRunId = br.BatchRunId) AS TotalDiscrepancyAmount
    FROM [dbo].[BatchRuns] br
    WHERE CAST(br.StartTime AS DATE) = @ReportDate
    ORDER BY br.StartTime DESC;
END
GO
