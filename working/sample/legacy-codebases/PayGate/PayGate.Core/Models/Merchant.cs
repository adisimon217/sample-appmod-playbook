using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PayGate.Core.Models
{
    /// <summary>
    /// Represents a merchant enrolled in the PayGate payment processing platform.
    /// </summary>
    public class Merchant
    {
        [Key]
        [StringLength(15)]
        public string MerchantId { get; set; }

        [Required]
        [StringLength(200)]
        public string BusinessName { get; set; }

        [StringLength(20)]
        public string TaxId { get; set; }

        [StringLength(200)]
        public string ContactEmail { get; set; }

        [StringLength(20)]
        public string ContactPhone { get; set; }

        public MerchantStatus Status { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? ActivatedDate { get; set; }

        public DateTime? SuspendedDate { get; set; }

        [StringLength(500)]
        public string SuspensionReason { get; set; }

        [StringLength(100)]
        public string SuspendedBy { get; set; }

        [StringLength(10)]
        public string MccCode { get; set; }

        [StringLength(20)]
        public string SettlementSchedule { get; set; }

        public int MaxTransactionsPerHour { get; set; }

        [StringLength(500)]
        public string WebhookUrl { get; set; }

        [StringLength(2000)]
        public string AllowedIpAddresses { get; set; } // Comma-separated

        public decimal? ProcessingFeePercent { get; set; }

        public decimal? MonthlyMinimumFee { get; set; }

        [StringLength(50)]
        public string RiskCategory { get; set; }

        // Navigation
        public virtual ICollection<Transaction> Transactions { get; set; }
    }

    public enum MerchantStatus
    {
        PendingVerification = 0,
        Active = 1,
        Suspended = 2,
        Terminated = 3,
        InReview = 4
    }

    /// <summary>
    /// Merchant transaction volume statistics.
    /// </summary>
    public class MerchantVolume
    {
        public string MerchantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalAmount { get; set; }
        public int SuccessfulTransactions { get; set; }
        public int FailedTransactions { get; set; }
        public decimal AverageTransactionAmount { get; set; }
        public int ChargebackCount { get; set; }
        public decimal ChargebackAmount { get; set; }
    }
}
