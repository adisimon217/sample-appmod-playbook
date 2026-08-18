using System;

namespace ReconcEngine.Models
{
    /// <summary>
    /// Represents a single record from a settlement file (CSV row).
    /// </summary>
    public class SettlementRecord
    {
        public Guid SettlementRecordId { get; set; }
        public string ReferenceNumber { get; set; }
        public string MerchantId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime SettlementDate { get; set; }
        public string ProcessorCode { get; set; }
        public string CardType { get; set; }
        public string BatchNumber { get; set; }
        public string SourceFile { get; set; }
        public DateTime ParsedAt { get; set; }
    }
}
