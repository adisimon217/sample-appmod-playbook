namespace FraudWatch.Core.Models;

/// <summary>
/// Represents a calculated risk score for a transaction.
/// Score ranges from 0.0 (no risk) to 1.0 (certain fraud).
/// </summary>
public record RiskScore(
    double Score,
    RiskLevel Level,
    IReadOnlyList<RiskFactor> Factors,
    DateTimeOffset CalculatedAt)
{
    /// <summary>
    /// Creates a RiskScore from a raw score value, automatically determining the level.
    /// </summary>
    public static RiskScore Create(double score, IReadOnlyList<RiskFactor> factors) =>
        new(
            Score: Math.Clamp(score, 0.0, 1.0),
            Level: score switch
            {
                >= 0.85 => RiskLevel.Critical,
                >= 0.70 => RiskLevel.High,
                >= 0.55 => RiskLevel.Medium,
                >= 0.30 => RiskLevel.Low,
                _ => RiskLevel.Minimal
            },
            Factors: factors,
            CalculatedAt: DateTimeOffset.UtcNow);
}

/// <summary>
/// A contributing factor to the risk score.
/// </summary>
public record RiskFactor(
    string Name,
    string Description,
    double Weight,
    double Contribution);

/// <summary>
/// Risk level categorization.
/// </summary>
public enum RiskLevel
{
    Minimal,
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Customer risk profile based on historical data analysis.
/// </summary>
public record CustomerRiskProfile(
    string CustomerId,
    double BaselineRiskScore,
    RiskLevel CurrentRiskLevel,
    int TotalTransactions,
    int FlaggedTransactions,
    decimal AverageTransactionAmount,
    IReadOnlyList<string> KnownCountries,
    IReadOnlyList<string> KnownDevices,
    DateTimeOffset LastActivity,
    DateTimeOffset ProfileUpdatedAt);
