using FraudWatch.Core.Models;

namespace FraudWatch.Core.Interfaces;

/// <summary>
/// Engine that calculates risk scores for transactions based on multiple factors.
/// </summary>
public interface IRiskScoringEngine
{
    /// <summary>
    /// Calculates a risk score for the given transaction.
    /// </summary>
    Task<RiskScore> CalculateRiskScoreAsync(
        Transaction transaction,
        IReadOnlyList<string> triggeredRules,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current risk profile for a customer.
    /// </summary>
    Task<CustomerRiskProfile?> GetCustomerRiskProfileAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the customer risk profile after a fraud check.
    /// </summary>
    Task UpdateCustomerRiskProfileAsync(
        string customerId,
        RiskScore latestScore,
        CancellationToken cancellationToken = default);
}
