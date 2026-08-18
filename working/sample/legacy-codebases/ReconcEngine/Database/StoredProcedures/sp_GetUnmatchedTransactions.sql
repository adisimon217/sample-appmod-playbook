-- Gets transactions from the specified date range that have no matching reconciliation result.
-- Used for investigating missing settlements and generating exception reports.
CREATE PROCEDURE [dbo].[sp_GetUnmatchedTransactions]
    @FromDate   DATETIME2(7),
    @ToDate     DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT r.MatchResultId,
           r.SettlementRecordId,
           r.TransactionId,
           r.Status,
           r.SettlementAmount,
           r.TransactionAmount,
           r.DiscrepancyAmount,
           r.MatchedOn,
           r.MatchCriteria,
           r.RetryCount,
           r.LastRetryTime
    FROM [dbo].[ReconcResults] r
    WHERE r.Status IN ('Unmatched', 'PendingRetry')
      AND r.MatchedOn >= @FromDate
      AND r.MatchedOn < @ToDate
    ORDER BY r.DiscrepancyAmount DESC;
END
GO
