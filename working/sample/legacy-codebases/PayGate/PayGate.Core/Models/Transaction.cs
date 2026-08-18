using System;
using System.ComponentModel.DataAnnotations;

namespace PayGate.Core.Models
{
    /// <summary>
    /// Represents a payment transaction in the PayGate system.
    /// Maps to dbo.Transactions table in PaymentsDB.
    /// </summary>
    public class Transaction
    {
        [Key]
        public Guid TransactionId { get; set; }

        [Required]
        [StringLength(15)]
        public string MerchantId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(3)]
        public string Currency { get; set; }

        [Required]
        [StringLength(19)]
        public string CardNumber { get; set; }

        [StringLength(7)]
        public string CardExpiry { get; set; }

        [StringLength(4)]
        public string Cvv { get; set; }

        [StringLength(100)]
        public string CardholderName { get; set; }

        public TransactionType TransactionType { get; set; }

        public TransactionStatus Status { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? ProcessedDate { get; set; }

        public DateTime? SettledDate { get; set; }

        [StringLength(50)]
        public string AuthorizationCode { get; set; }

        [StringLength(10)]
        public string ResponseCode { get; set; }

        [StringLength(500)]
        public string ResponseMessage { get; set; }

        [StringLength(45)]
        public string IpAddress { get; set; }

        [StringLength(500)]
        public string UserAgent { get; set; }

        public Guid? BatchId { get; set; }

        public Guid? OriginalTransactionId { get; set; }

        public decimal? ProcessingFee { get; set; }

        [StringLength(50)]
        public string CardBrand { get; set; }

        [StringLength(6)]
        public string CardBin { get; set; }

        [StringLength(4)]
        public string CardLast4 { get; set; }

        public int? LatencyMs { get; set; }

        // Navigation properties
        public virtual Merchant Merchant { get; set; }
        public virtual Settlement Settlement { get; set; }
    }

    public enum TransactionType
    {
        Authorization = 1,
        Capture = 2,
        Sale = 3,  // Auth + Capture in one step
        Refund = 4,
        Void = 5,
        Reversal = 6
    }

    public enum TransactionStatus
    {
        Pending = 0,
        Authorized = 1,
        Captured = 2,
        Declined = 3,
        Failed = 4,
        Voided = 5,
        Refunded = 6,
        Settled = 7,
        Chargeback = 8,
        Expired = 9
    }
}
