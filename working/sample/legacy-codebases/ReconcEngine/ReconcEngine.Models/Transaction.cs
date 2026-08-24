using System;

namespace ReconcEngine.Models
{
    /// <summary>
    /// Represents a payment transaction from PayGateDB.
    /// </summary>
    public class Transaction
    {
        public Guid TransactionId { get; set; }
        public string MerchantId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public DateTime TransactionDate { get; set; }
        public string ReferenceNumber { get; set; }
        public string Status { get; set; }
        public string ProcessorResponse { get; set; }
        public string CardType { get; set; }
        public string BatchNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
