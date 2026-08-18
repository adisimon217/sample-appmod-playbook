using System;
using ReconcEngine.Models.Enums;

namespace ReconcEngine.Models
{
    /// <summary>
    /// Represents a single nightly reconciliation batch run.
    /// </summary>
    public class BatchRun
    {
        public Guid BatchRunId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public BatchRunStatus Status { get; set; }
        public string CorrelationId { get; set; }
        public int MatchedCount { get; set; }
        public int DiscrepancyCount { get; set; }
        public int FailedCount { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Summary view of a batch run with aggregated statistics.
    /// </summary>
    public class BatchRunSummary
    {
        public Guid BatchRunId { get; set; }
        public DateTime RunDate { get; set; }
        public string Status { get; set; }
        public TimeSpan Duration { get; set; }
        public int TotalSettlementRecords { get; set; }
        public int TotalTransactions { get; set; }
        public int MatchedCount { get; set; }
        public int DiscrepancyCount { get; set; }
        public int UnmatchedSettlements { get; set; }
        public int UnmatchedTransactions { get; set; }
        public decimal TotalSettlementAmount { get; set; }
        public decimal TotalTransactionAmount { get; set; }
        public decimal TotalDiscrepancyAmount { get; set; }
    }
}
