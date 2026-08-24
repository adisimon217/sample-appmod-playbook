using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MerchantHub.Core.Models
{
    /// <summary>
    /// Merchant entity - maps to MH_Merchants table
    /// </summary>
    public class Merchant
    {
        public int MerchantId { get; set; }

        [Required]
        [StringLength(200)]
        public string BusinessName { get; set; }

        [StringLength(200)]
        public string DBA { get; set; }

        [StringLength(11)]
        public string EIN { get; set; }

        [StringLength(20)]
        public string MerchantNumber { get; set; }

        [Required]
        [StringLength(200)]
        public string Address1 { get; set; }

        [StringLength(200)]
        public string Address2 { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; }

        [Required]
        [StringLength(2)]
        public string State { get; set; }

        [Required]
        [StringLength(10)]
        public string ZipCode { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; }

        [StringLength(500)]
        public string Website { get; set; }

        [StringLength(50)]
        public string Status { get; set; } // Active, Suspended, Closed, PendingReview

        [StringLength(50)]
        public string Tier { get; set; } // Standard, Premium, Enterprise

        [StringLength(10)]
        public string MCC { get; set; } // Merchant Category Code

        public decimal MonthlyVolumeLimit { get; set; }
        public decimal SingleTransactionLimit { get; set; }
        public decimal ProcessingFeeRate { get; set; }

        public DateTime ContractStartDate { get; set; }
        public DateTime? ContractEndDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastProfileUpdate { get; set; }
        public DateTime? SuspendedDate { get; set; }
        public string SuspensionReason { get; set; }
        public string SuspendedBy { get; set; }

        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // Navigation properties (EF6)
        public virtual ICollection<Transaction> Transactions { get; set; }
        public virtual ICollection<Dispute> Disputes { get; set; }
        public virtual ICollection<MerchantUser> Users { get; set; }
    }

    /// <summary>
    /// Merchant user for authentication
    /// </summary>
    public class MerchantUser
    {
        public int UserId { get; set; }
        public int MerchantId { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string MerchantName { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string LastLoginIP { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public string PasswordResetToken { get; set; }
        public DateTime? PasswordResetExpiry { get; set; }
        public DateTime CreatedDate { get; set; }

        public virtual Merchant Merchant { get; set; }
    }
}
