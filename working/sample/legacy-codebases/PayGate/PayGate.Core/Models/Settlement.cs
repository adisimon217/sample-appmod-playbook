using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PayGate.Core.Models
{
    /// <summary>
    /// Represents a settlement batch sent to Visa/Mastercard.
    /// </summary>
    public class Settlement
    {
        [Key]
        public Guid SettlementId { get; set; }

        public Guid BatchId { get; set; }

        [Required]
        [StringLength(20)]
        public string Network { get; set; } // "VISA", "MASTERCARD"

        public DateTime SettlementDate { get; set; }

        public decimal TotalAmount { get; set; }

        public int TransactionCount { get; set; }

        public SettlementStatus Status { get; set; }

        [StringLength(100)]
        public string FileName { get; set; }

        public DateTime? FileGeneratedDate { get; set; }

        public DateTime? FileSentDate { get; set; }

        public DateTime? ResponseReceivedDate { get; set; }

        [StringLength(1000)]
        public string ErrorMessage { get; set; }

        public decimal? ChargebackAmount { get; set; }

        public int? ChargebackCount { get; set; }

        public decimal? NetSettlementAmount { get; set; }

        // Navigation
        public virtual ICollection<Transaction> Transactions { get; set; }
    }

    public enum SettlementStatus
    {
        Pending = 0,
        FileGenerated = 1,
        FileSent = 2,
        Acknowledged = 3,
        Reconciled = 4,
        Completed = 5,
        Failed = 6,
        PartialReconciliation = 7
    }

    /// <summary>
    /// Result from the daily settlement processing.
    /// </summary>
    public class SettlementResult
    {
        public bool Success { get; set; }
        public int VisaCount { get; set; }
        public int MastercardCount { get; set; }
        public decimal TotalSettlementAmount { get; set; }
        public bool VisaFileGenerated { get; set; }
        public bool MastercardFileGenerated { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result from a reconciliation operation.
    /// </summary>
    public class ReconciliationResult
    {
        public bool Success { get; set; }
        public int MatchedCount { get; set; }
        public int UnmatchedCount { get; set; }
        public decimal TotalReconciled { get; set; }
        public List<string> Discrepancies { get; set; } = new List<string>();
    }
}
