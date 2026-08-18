using System;
using ReconcEngine.Models.Enums;

namespace ReconcEngine.Models
{
    /// <summary>
    /// Represents a detected discrepancy between settlement and transaction data.
    /// </summary>
    public class DiscrepancyReport
    {
        public Guid DiscrepancyId { get; set; }
        public Guid BatchRunId { get; set; }
        public DiscrepancyType Type { get; set; }
        public Guid SettlementRecordId { get; set; }
        public Guid TransactionId { get; set; }
        public decimal SettlementAmount { get; set; }
        public decimal TransactionAmount { get; set; }
        public decimal DiscrepancyAmount { get; set; }
        public DateTime DetectedAt { get; set; }
        public string Description { get; set; }
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string ResolvedBy { get; set; }
    }
}
