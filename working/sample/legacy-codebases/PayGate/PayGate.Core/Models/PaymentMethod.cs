using System;
using System.ComponentModel.DataAnnotations;

namespace PayGate.Core.Models
{
    /// <summary>
    /// Represents a stored payment method (tokenized card).
    /// Card numbers are stored encrypted; only BIN and last4 retained in clear text.
    /// </summary>
    public class PaymentMethod
    {
        [Key]
        public Guid PaymentMethodId { get; set; }

        [Required]
        [StringLength(15)]
        public string MerchantId { get; set; }

        [StringLength(100)]
        public string CustomerId { get; set; }

        [Required]
        [StringLength(50)]
        public string Token { get; set; }

        [StringLength(20)]
        public string CardBrand { get; set; } // Visa, Mastercard, Amex, Discover

        [StringLength(6)]
        public string CardBin { get; set; }

        [StringLength(4)]
        public string CardLast4 { get; set; }

        [StringLength(7)]
        public string ExpiryDate { get; set; }

        [StringLength(100)]
        public string CardholderName { get; set; }

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? LastUsedDate { get; set; }

        [StringLength(50)]
        public string IssuingBank { get; set; }

        [StringLength(2)]
        public string IssuingCountry { get; set; }
    }
}
