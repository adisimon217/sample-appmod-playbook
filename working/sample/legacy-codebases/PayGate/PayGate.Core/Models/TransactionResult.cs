using System;

namespace PayGate.Core.Models
{
    /// <summary>
    /// Result returned from transaction processing operations.
    /// </summary>
    public class TransactionResult
    {
        public bool Success { get; set; }

        public Guid TransactionId { get; set; }

        public string AuthorizationCode { get; set; }

        public string ErrorCode { get; set; }

        public string ErrorMessage { get; set; }

        public DateTime Timestamp { get; set; }

        public string ResponseCode { get; set; }

        public decimal? ProcessingFee { get; set; }

        public int? LatencyMs { get; set; }
    }

    /// <summary>
    /// Result from merchant validation check.
    /// </summary>
    public class MerchantValidationResult
    {
        public bool IsValid { get; set; }
        public string Reason { get; set; }
        public string MerchantId { get; set; }
        public MerchantStatus? Status { get; set; }
    }

    /// <summary>
    /// Peak hour metrics for administrative reporting.
    /// </summary>
    public class PeakHourMetrics
    {
        public int PeakVolume { get; set; }
        public int PeakHour { get; set; }
        public int TotalVolume { get; set; }
        public double AvgLatencyMs { get; set; }
        public double P99LatencyMs { get; set; }
    }
}
