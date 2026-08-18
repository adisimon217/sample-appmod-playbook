using FraudWatch.Core.Models;

namespace FraudWatch.Core.Interfaces;

/// <summary>
/// Core fraud detection service that orchestrates transaction evaluation.
/// </summary>
public interface IFraudDetectionService
{
    /// <summary>
    /// Evaluates a single transaction for fraud risk.
    /// </summary>
    Task<FraudCheckResult> EvaluateTransactionAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a batch of transactions, yielding results as they complete.
    /// </summary>
    IAsyncEnumerable<FraudCheckResult> EvaluateBatchAsync(
        IEnumerable<Transaction> transactions,
        CancellationToken cancellationToken = default);
}
