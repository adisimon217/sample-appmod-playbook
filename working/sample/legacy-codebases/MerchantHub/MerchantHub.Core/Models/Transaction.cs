using System;
using System.ComponentModel.DataAnnotations;

namespace MerchantHub.Core.Models
{
    /// <summary>
    /// Transaction entity - maps to MH_Transactions table
    /// Also used for data from PayGateDB reads
    /// </summary>
    public class Transaction
    {
        public long TransactionId { get; set; }
        public int MerchantId { get; set; }

        [StringLength(50)]
        public string ReferenceNumber { get; set; }

        [StringLength(50)]
        public string AuthorizationCode { get; set; }

        public decimal Amount { get; set; }
        public decimal? RefundAmount { get; set; }
        public decimal? Fee { get; set; }
        public decimal? NetAmount { get; set; }

        [StringLength(3)]
        public string Currency { get; set; }

        [StringLength(20)]
        public string Status { get; set; } // Approved, Declined, Pending, Refunded, Chargeback, Voided

        [StringLength(20)]
        public string CardType { get; set; } // Visa, Mastercard, Amex, Discover

        [StringLength(4)]
        public string Last4Digits { get; set; }

        [StringLength(64)]
        public string CardFingerprint { get; set; }

        [StringLength(10)]
        public string EntryMode { get; set; } // Swiped, Keyed, EMV, Contactless, ECommerce

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(100)]
        public string CustomerName { get; set; }

        [StringLength(200)]
        public string CustomerEmail { get; set; }

        [StringLength(50)]
        public string BatchNumber { get; set; }

        public DateTime TransactionDate { get; set; }
        public DateTime? SettlementDate { get; set; }
        public DateTime CreatedDate { get; set; }

        [StringLength(50)]
        public string DeclineReason { get; set; }

        [StringLength(20)]
        public string ResponseCode { get; set; }

        [StringLength(50)]
        public string TerminalId { get; set; }

        // PayGate reference
        public long? PayGateTransactionId { get; set; }

        // Navigation
        public virtual Merchant Merchant { get; set; }
    }

    /// <summary>
    /// Result of a refund operation
    /// </summary>
    public class RefundResult
    {
        public bool Success { get; set; }
        public string ReferenceNumber { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Batch summary for settlement
    /// </summary>
    public class BatchSummary
    {
        public string BatchNumber { get; set; }
        public DateTime BatchDate { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalFees { get; set; }
        public decimal NetAmount { get; set; }
        public string Status { get; set; }
    }
}
