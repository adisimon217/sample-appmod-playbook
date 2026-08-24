using System;
using System.Collections.Generic;
using System.Configuration;
using PayGate.Core.Models;

namespace PayGate.Core.Services
{
    /// <summary>
    /// Determines transaction routing based on card type, merchant configuration,
    /// and network availability. Supports failover between primary and backup
    /// processing endpoints.
    /// </summary>
    public class TransactionRoutingService
    {
        private static readonly Dictionary<string, NetworkEndpoint> _endpoints = new Dictionary<string, NetworkEndpoint>
        {
            { "Visa", new NetworkEndpoint { Primary = "https://api.visa.com/v1/auth", Backup = "https://api-backup.visa.com/v1/auth", TimeoutMs = 5000 } },
            { "Mastercard", new NetworkEndpoint { Primary = "https://api.mastercard.com/v1/auth", Backup = "https://api-dr.mastercard.com/v1/auth", TimeoutMs = 5000 } },
            { "Amex", new NetworkEndpoint { Primary = "https://api.amex.com/v1/auth", Backup = null, TimeoutMs = 8000 } },
            { "Discover", new NetworkEndpoint { Primary = "https://api.discover.com/v1/auth", Backup = null, TimeoutMs = 8000 } }
        };

        /// <summary>
        /// Get the appropriate network endpoint for a transaction.
        /// </summary>
        public NetworkEndpoint GetEndpoint(Transaction transaction)
        {
            if (_endpoints.TryGetValue(transaction.CardBrand, out var endpoint))
            {
                return endpoint;
            }

            throw new InvalidOperationException($"Unsupported card brand: {transaction.CardBrand}");
        }

        /// <summary>
        /// Determine if a transaction should be routed through the high-priority queue
        /// based on amount and merchant tier.
        /// </summary>
        public TransactionPriority DetermineRoutingPriority(Transaction transaction, string merchantTier)
        {
            // High-value transactions get priority routing
            if (transaction.Amount >= 10000m)
                return TransactionPriority.High;

            // Enterprise merchants get priority
            if (merchantTier == "Enterprise" || merchantTier == "Premium")
                return TransactionPriority.High;

            // Refunds get priority to improve customer experience
            if (transaction.TransactionType == TransactionType.Refund)
                return TransactionPriority.High;

            return TransactionPriority.Normal;
        }

        /// <summary>
        /// Check if the card BIN is from a known high-risk issuer/region.
        /// </summary>
        public RiskAssessment AssessBinRisk(string bin)
        {
            if (string.IsNullOrEmpty(bin) || bin.Length < 6)
                return new RiskAssessment { Level = RiskLevel.Unknown };

            // Example: Certain BIN ranges flagged for additional verification
            var binPrefix = bin.Substring(0, 4);

            // Prepaid card BINs - higher fraud risk
            if (binPrefix.StartsWith("4026") || binPrefix.StartsWith("4508"))
            {
                return new RiskAssessment
                {
                    Level = RiskLevel.Elevated,
                    Reason = "Prepaid card - elevated fraud risk",
                    RequiresAdditionalVerification = true
                };
            }

            return new RiskAssessment { Level = RiskLevel.Normal };
        }
    }

    public class NetworkEndpoint
    {
        public string Primary { get; set; }
        public string Backup { get; set; }
        public int TimeoutMs { get; set; }
    }

    public enum TransactionPriority
    {
        Normal = 0,
        High = 1,
        Critical = 2
    }

    public class RiskAssessment
    {
        public RiskLevel Level { get; set; }
        public string Reason { get; set; }
        public bool RequiresAdditionalVerification { get; set; }
    }

    public enum RiskLevel
    {
        Normal = 0,
        Elevated = 1,
        High = 2,
        Critical = 3,
        Unknown = 99
    }
}
