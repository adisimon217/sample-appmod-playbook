using FraudWatch.Core.Interfaces;
using FraudWatch.Core.Models;
using Microsoft.Extensions.Logging;

namespace FraudWatch.Core.Services;

/// <summary>
/// Calculates risk scores using multiple factors including transaction attributes,
/// triggered rules, velocity data, and geographic patterns.
/// </summary>
public class RiskScoringEngine : IRiskScoringEngine
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<RiskScoringEngine> _logger;

    // Scoring weights for different factor categories
    private static readonly Dictionary<string, double> CategoryWeights = new()
    {
        ["amount"] = 0.20,
        ["velocity"] = 0.25,
        ["geographic"] = 0.15,
        ["behavioral"] = 0.20,
        ["rules"] = 0.20
    };

    // Known high-risk countries for enhanced screening
    private static readonly HashSet<string> HighRiskCountries = new(StringComparer.OrdinalIgnoreCase)
    {
        "NG", "RO", "UA", "PH", "ID", "VN", "GH", "KE"
    };

    public RiskScoringEngine(
        ICacheService cacheService,
        ILogger<RiskScoringEngine> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<RiskScore> CalculateRiskScoreAsync(
        Transaction transaction,
        IReadOnlyList<string> triggeredRules,
        CancellationToken cancellationToken = default)
    {
        var factors = new List<RiskFactor>();

        // Factor 1: Amount-based scoring
        var amountFactor = CalculateAmountFactor(transaction);
        factors.Add(amountFactor);

        // Factor 2: Velocity check (transaction frequency)
        var velocityFactor = await CalculateVelocityFactorAsync(
            transaction, cancellationToken);
        factors.Add(velocityFactor);

        // Factor 3: Geographic risk
        var geoFactor = CalculateGeographicFactor(transaction);
        factors.Add(geoFactor);

        // Factor 4: Channel and behavioral analysis
        var behavioralFactor = await CalculateBehavioralFactorAsync(
            transaction, cancellationToken);
        factors.Add(behavioralFactor);

        // Factor 5: Rule-triggered risk contribution
        var ruleFactor = CalculateRuleFactor(triggeredRules);
        factors.Add(ruleFactor);

        // Calculate weighted final score
        var totalScore = factors.Sum(f => f.Contribution);
        var clampedScore = Math.Clamp(totalScore, 0.0, 1.0);

        _logger.LogDebug(
            "Risk score for {TransactionId}: {Score:F3} (amount={Amount:F2}, velocity={Velocity:F2}, geo={Geo:F2}, behavioral={Behavioral:F2}, rules={Rules:F2})",
            transaction.TransactionId, clampedScore,
            amountFactor.Contribution, velocityFactor.Contribution,
            geoFactor.Contribution, behavioralFactor.Contribution,
            ruleFactor.Contribution);

        return RiskScore.Create(clampedScore, factors);
    }

    /// <inheritdoc/>
    public async Task<CustomerRiskProfile?> GetCustomerRiskProfileAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"customer-risk-profile:{customerId}";
        var cached = await _cacheService.GetAsync<CustomerRiskProfile>(cacheKey, cancellationToken);

        if (cached is not null)
        {
            return cached;
        }

        // In production, this would query the database
        // Returning null indicates no profile exists yet
        _logger.LogDebug("No cached risk profile for customer {CustomerId}", customerId);
        return null;
    }

    /// <inheritdoc/>
    public async Task UpdateCustomerRiskProfileAsync(
        string customerId,
        RiskScore latestScore,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"customer-risk-profile:{customerId}";
        var existing = await _cacheService.GetAsync<CustomerRiskProfile>(cacheKey, cancellationToken);

        var updatedProfile = existing is not null
            ? existing with
            {
                BaselineRiskScore = CalculateMovingAverage(existing.BaselineRiskScore, latestScore.Score),
                CurrentRiskLevel = latestScore.Level,
                TotalTransactions = existing.TotalTransactions + 1,
                FlaggedTransactions = latestScore.Score > 0.55
                    ? existing.FlaggedTransactions + 1
                    : existing.FlaggedTransactions,
                LastActivity = DateTimeOffset.UtcNow,
                ProfileUpdatedAt = DateTimeOffset.UtcNow
            }
            : new CustomerRiskProfile(
                CustomerId: customerId,
                BaselineRiskScore: latestScore.Score,
                CurrentRiskLevel: latestScore.Level,
                TotalTransactions: 1,
                FlaggedTransactions: latestScore.Score > 0.55 ? 1 : 0,
                AverageTransactionAmount: 0,
                KnownCountries: Array.Empty<string>(),
                KnownDevices: Array.Empty<string>(),
                LastActivity: DateTimeOffset.UtcNow,
                ProfileUpdatedAt: DateTimeOffset.UtcNow);

        await _cacheService.SetAsync(
            cacheKey, updatedProfile,
            TimeSpan.FromHours(24),
            cancellationToken);
    }

    private RiskFactor CalculateAmountFactor(Transaction transaction)
    {
        var weight = CategoryWeights["amount"];

        // Score based on transaction amount thresholds
        var contribution = transaction.Amount switch
        {
            > 50000m => weight * 1.0,
            > 25000m => weight * 0.8,
            > 10000m => weight * 0.6,
            > 5000m => weight * 0.4,
            > 1000m => weight * 0.2,
            _ => weight * 0.05
        };

        return new RiskFactor(
            Name: "TransactionAmount",
            Description: $"Transaction amount: {transaction.Amount:C} {transaction.Currency}",
            Weight: weight,
            Contribution: contribution);
    }

    private async Task<RiskFactor> CalculateVelocityFactorAsync(
        Transaction transaction,
        CancellationToken cancellationToken)
    {
        var weight = CategoryWeights["velocity"];
        var cacheKey = $"velocity:{transaction.CustomerId}";

        // Check recent transaction count from cache
        var recentCount = await _cacheService.GetAsync<int?>(cacheKey, cancellationToken) ?? 0;

        var contribution = recentCount switch
        {
            > 20 => weight * 1.0,
            > 15 => weight * 0.8,
            > 10 => weight * 0.6,
            > 5 => weight * 0.3,
            _ => weight * 0.05
        };

        // Increment velocity counter
        await _cacheService.SetAsync(
            cacheKey, recentCount + 1,
            TimeSpan.FromMinutes(60),
            cancellationToken);

        return new RiskFactor(
            Name: "TransactionVelocity",
            Description: $"{recentCount} transactions in the last 60 minutes",
            Weight: weight,
            Contribution: contribution);
    }

    private RiskFactor CalculateGeographicFactor(Transaction transaction)
    {
        var weight = CategoryWeights["geographic"];

        var contribution = 0.0;
        var description = "Normal geographic pattern";

        if (transaction.Country is not null && HighRiskCountries.Contains(transaction.Country))
        {
            contribution = weight * 0.7;
            description = $"Transaction from high-risk country: {transaction.Country}";
        }
        else if (transaction.Country is null)
        {
            contribution = weight * 0.3;
            description = "Geographic information unavailable";
        }
        else
        {
            contribution = weight * 0.05;
        }

        return new RiskFactor(
            Name: "GeographicRisk",
            Description: description,
            Weight: weight,
            Contribution: contribution);
    }

    private async Task<RiskFactor> CalculateBehavioralFactorAsync(
        Transaction transaction,
        CancellationToken cancellationToken)
    {
        var weight = CategoryWeights["behavioral"];

        // Check for unusual patterns: time of day, channel switches, etc.
        var contribution = 0.0;
        var descriptions = new List<string>();

        // Late night transactions (UTC 02:00-05:00) are slightly riskier
        var hour = transaction.Timestamp.UtcDateTime.Hour;
        if (hour is >= 2 and <= 5)
        {
            contribution += weight * 0.3;
            descriptions.Add("Late night transaction");
        }

        // Phone/API channel transactions without IP are suspicious
        if (transaction.Channel is TransactionChannel.Phone or TransactionChannel.Api
            && transaction.IpAddress is null)
        {
            contribution += weight * 0.2;
            descriptions.Add("Remote channel without IP tracking");
        }

        // Check for known device/IP patterns from cache
        var knownIpKey = $"known-ip:{transaction.CustomerId}:{transaction.IpAddress}";
        if (transaction.IpAddress is not null)
        {
            var isKnown = await _cacheService.GetAsync<bool?>(knownIpKey, cancellationToken);
            if (isKnown is null or false)
            {
                contribution += weight * 0.2;
                descriptions.Add("New/unknown IP address");
                await _cacheService.SetAsync(knownIpKey, true, TimeSpan.FromDays(30), cancellationToken);
            }
        }

        contribution = Math.Min(contribution, weight);

        return new RiskFactor(
            Name: "BehavioralPattern",
            Description: descriptions.Any()
                ? string.Join("; ", descriptions)
                : "Normal behavioral patterns",
            Weight: weight,
            Contribution: contribution);
    }

    private RiskFactor CalculateRuleFactor(IReadOnlyList<string> triggeredRules)
    {
        var weight = CategoryWeights["rules"];

        var contribution = triggeredRules.Count switch
        {
            > 5 => weight * 1.0,
            > 3 => weight * 0.7,
            > 1 => weight * 0.4,
            1 => weight * 0.2,
            _ => 0.0
        };

        return new RiskFactor(
            Name: "RuleTriggered",
            Description: triggeredRules.Count > 0
                ? $"{triggeredRules.Count} fraud rules triggered"
                : "No fraud rules triggered",
            Weight: weight,
            Contribution: contribution);
    }

    private static double CalculateMovingAverage(double current, double newValue, double alpha = 0.3) =>
        (alpha * newValue) + ((1 - alpha) * current);
}
