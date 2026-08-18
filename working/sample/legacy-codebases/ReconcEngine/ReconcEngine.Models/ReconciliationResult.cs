using System;
using System.Collections.Generic;

namespace ReconcEngine.Models
{
    /// <summary>
    /// Represents the overall result of a reconciliation run.
    /// </summary>
    public class ReconciliationResult
    {
        public Guid BatchRunId { get; set; }
        public bool Success { get; set; }
        public int MatchedCount { get; set; }
        public int DiscrepancyCount { get; set; }
        public int FailedCount { get; set; }
        public TimeSpan Duration { get; set; }
        public string ErrorMessage { get; set; }
        public List<MatchResult> MatchResults { get; set; } = new List<MatchResult>();
        public List<DiscrepancyReport> Discrepancies { get; set; } = new List<DiscrepancyReport>();
        public List<SettlementRecord> UnmatchedSettlements { get; set; } = new List<SettlementRecord>();
        public List<Transaction> UnmatchedTransactions { get; set; } = new List<Transaction>();
    }
}
