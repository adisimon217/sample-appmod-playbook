using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MerchantHub.Core.Models
{
    /// <summary>
    /// Dispute/Chargeback entity - maps to MH_Disputes table
    /// </summary>
    public class Dispute
    {
        public int DisputeId { get; set; }
        public int MerchantId { get; set; }
        public long TransactionId { get; set; }

        [StringLength(50)]
        public string CaseNumber { get; set; }

        [StringLength(50)]
        public string ReasonCode { get; set; }

        [StringLength(200)]
        public string ReasonDescription { get; set; }

        public decimal Amount { get; set; }

        [StringLength(20)]
        public string Status { get; set; } // Open, UnderReview, Responded, Won, Lost, Expired

        [StringLength(20)]
        public string CardType { get; set; }

        [StringLength(4)]
        public string Last4Digits { get; set; }

        public DateTime FiledDate { get; set; }
        public DateTime ResponseDeadline { get; set; }
        public DateTime? RespondedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }

        [StringLength(2000)]
        public string MerchantResponse { get; set; }

        [StringLength(500)]
        public string Resolution { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }

        // Navigation
        public virtual Merchant Merchant { get; set; }
        public virtual Transaction Transaction { get; set; }
    }

    /// <summary>
    /// Dispute history entry
    /// </summary>
    public class DisputeHistoryEntry
    {
        public int HistoryId { get; set; }
        public int DisputeId { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
        public string PerformedBy { get; set; }
        public DateTime ActionDate { get; set; }
    }

    /// <summary>
    /// Document attached to a dispute
    /// </summary>
    public class DisputeDocument
    {
        public int DocumentId { get; set; }
        public int DisputeId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string DocumentType { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadedDate { get; set; }
        public string UploadedBy { get; set; }
    }
}
