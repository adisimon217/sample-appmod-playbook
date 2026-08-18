using System;
using ReconcEngine.Models.Enums;

namespace ReconcEngine.Models
{
    /// <summary>
    /// Represents the result of matching a settlement record to a transaction.
    /// </summary>
    public class MatchResult
    {
        public Guid MatchResultId { get; set; }
        public Guid SettlementRecordId { get; set; }
        public Guid TransactionId { get; set; }
        public MatchStatus Status { get; set; }
        public decimal SettlementAmount { get; set; }
        public decimal TransactionAmount { get; set; }
        public decimal DiscrepancyAmount { get; set; }
        public DateTime MatchedOn { get; set; }
        public string MatchCriteria { get; set; }
        public int RetryCount { get; set; }
        public DateTime? LastRetryTime { get; set; }
    }
}
