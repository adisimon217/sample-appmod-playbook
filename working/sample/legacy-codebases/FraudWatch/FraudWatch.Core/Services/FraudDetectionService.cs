using System.Runtime.CompilerServices;
using FraudWatch.Core.Interfaces;
using FraudWatch.Core.Models;
using Microsoft.Extensions.Logging;

namespace FraudWatch.Core.Services;

/// <summary>
/// Orchestrates the fraud detection pipeline: rule evaluation, risk scoring,
/// alert generation, and decision making.
/// </summary>
public class FraudDetectionService : IFraudDetectionService
{
    private readonly IRuleEvaluator _ruleEvaluator;
    private readonly IRiskScoringEngine _riskScoringEngine;
    private readonly IBankingApiClient _bankingApiClient;
    private readonly IFraudAlertRepository _alertRepository;
    private readonly ILogger<FraudDetectionService> _logger;

    private const double HighRiskThreshold = 0.85;
    private const double MediumRiskThreshold = 0.55;
    private const double ReviewThreshold = 0.40;

    public FraudDetectionService(
        IRuleEvaluator ruleEvaluator,
        IRiskScoringEngine riskScoringEngine,
        IBankingApiClient bankingApiClient,
        IFraudAlertRepository alertRepository,
        ILogger<FraudDetectionService> logger)
    {
        _ruleEvaluator = ruleEvaluator;
        _riskScoringEngine = riskScoringEngine;
        _bankingApiClient = bankingApiClient;
        _alertRepository = alertRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<FraudCheckResult> EvaluateTransactionAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Starting fraud evaluation for transaction {TransactionId}, amount {Amount} {Currency}",
            transaction.TransactionId, transaction.Amount, transaction.Currency);

        // Step 1: Evaluate rules
        var triggeredRules = await _ruleEvaluator.EvaluateAsync(transaction, cancellationToken);

        _logger.LogDebug(
            "Transaction {TransactionId} triggered {RuleCount} rules",
            transaction.TransactionId, triggeredRules.Count);

        // Step 2: Calculate risk score incorporating rule results and external data
        var riskScore = await _riskScoringEngine.CalculateRiskScoreAsync(
            transaction, triggeredRules, cancellationToken);

        // Step 3: Enhance with banking API data for high-risk transactions
        if (riskScore.Score >= ReviewThreshold)
        {
            riskScore = await EnhanceWithBankingDataAsync(
                transaction, riskScore, cancellationToken);
        }

        // Step 4: Make decision based on risk score
        var decision = DetermineDecision(riskScore);

        // Step 5: Generate alert if needed
        string? alertId = null;
        if (decision is FraudDecision.Decline or FraudDecision.Review)
        {
            alertId = await GenerateAlertAsync(
                transaction, riskScore, triggeredRules, cancellationToken);
        }

        // Step 6: Update customer risk profile
        await _riskScoringEngine.UpdateCustomerRiskProfileAsync(
            transaction.CustomerId, riskScore, cancellationToken);

        var result = new FraudCheckResult(
            TransactionId: transaction.TransactionId,
            RiskScore: riskScore,
            Decision: decision,
            TriggeredRules: triggeredRules,
            AlertId: alertId,
            EvaluatedAt: DateTimeOffset.UtcNow);

        _logger.LogInformation(
            "Fraud check complete for {TransactionId}: score={Score:F3}, decision={Decision}, alert={AlertId}",
            transaction.TransactionId, riskScore.Score, decision, alertId ?? "none");

        return result;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<FraudCheckResult> EvaluateBatchAsync(
        IEnumerable<Transaction> transactions,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var tasks = transactions.Select(txn =>
            EvaluateTransactionAsync(txn, cancellationToken));

        // Process in parallel with a degree of parallelism
        var semaphore = new SemaphoreSlim(10);
        var runningTasks = new List<Task<FraudCheckResult>>();

        foreach (var transaction in transactions)
        {
            await semaphore.WaitAsync(cancellationToken);

            var task = Task.Run(async () =>
            {
                try
                {
                    return await EvaluateTransactionAsync(transaction, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken);

            runningTasks.Add(task);
        }

        // Yield results as they complete
        while (runningTasks.Any())
        {
            var completedTask = await Task.WhenAny(runningTasks);
            runningTasks.Remove(completedTask);
            yield return await completedTask;
        }
    }

    private FraudDecision DetermineDecision(RiskScore riskScore) => riskScore.Score switch
    {
        >= HighRiskThreshold => FraudDecision.Decline,
        >= MediumRiskThreshold => FraudDecision.Review,
        >= ReviewThreshold => FraudDecision.Challenge,
        _ => FraudDecision.Approve
    };

    private async Task<RiskScore> EnhanceWithBankingDataAsync(
        Transaction transaction,
        RiskScore currentScore,
        CancellationToken cancellationToken)
    {
        try
        {
            var accountInfo = await _bankingApiClient.GetAccountRiskInfoAsync(
                transaction.CustomerId, cancellationToken);

            if (accountInfo is null)
            {
                return currentScore;
            }

            var enhancedFactors = new List<RiskFactor>(currentScore.Factors);

            // Account age factor
            if (accountInfo.AccountAgeMonths < 3)
            {
                enhancedFactors.Add(new RiskFactor(
                    Name: "NewAccount",
                    Description: $"Account is only {accountInfo.AccountAgeMonths} months old",
                    Weight: 0.15,
                    Contribution: 0.15));
            }

            // Recent chargebacks factor
            if (accountInfo.HasRecentChargebacks)
            {
                var chargebackContribution = Math.Min(accountInfo.ChargebackCount90Days * 0.1, 0.3);
                enhancedFactors.Add(new RiskFactor(
                    Name: "RecentChargebacks",
                    Description: $"{accountInfo.ChargebackCount90Days} chargebacks in last 90 days",
                    Weight: 0.2,
                    Contribution: chargebackContribution));
            }

            // External risk score integration
            if (accountInfo.ExternalRiskScore > 0.5)
            {
                enhancedFactors.Add(new RiskFactor(
                    Name: "ExternalRiskScore",
                    Description: $"Banking API risk score: {accountInfo.ExternalRiskScore:F2}",
                    Weight: 0.25,
                    Contribution: accountInfo.ExternalRiskScore * 0.25));
            }

            // Recalculate total score with banking data
            var totalContribution = enhancedFactors.Sum(f => f.Contribution);
            var adjustedScore = Math.Clamp(totalContribution, 0.0, 1.0);

            return RiskScore.Create(adjustedScore, enhancedFactors);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to enhance risk score with banking data for customer {CustomerId}. Continuing with base score.",
                transaction.CustomerId);
            return currentScore;
        }
    }

    private async Task<string> GenerateAlertAsync(
        Transaction transaction,
        RiskScore riskScore,
        IReadOnlyList<string> triggeredRules,
        CancellationToken cancellationToken)
    {
        var severity = riskScore.Level switch
        {
            RiskLevel.Critical => AlertSeverity.Critical,
            RiskLevel.High => AlertSeverity.High,
            RiskLevel.Medium => AlertSeverity.Medium,
            _ => AlertSeverity.Low
        };

        var alert = new FraudAlert
        {
            Id = Guid.NewGuid().ToString(),
            TransactionId = transaction.TransactionId,
            MerchantId = transaction.MerchantId,
            CustomerId = transaction.CustomerId,
            TransactionAmount = transaction.Amount,
            Currency = transaction.Currency,
            RiskScore = riskScore,
            Severity = severity,
            Status = AlertStatus.Open,
            TriggeredRules = triggeredRules,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _alertRepository.CreateAsync(alert, cancellationToken);

        _logger.LogWarning(
            "Fraud alert {AlertId} generated for transaction {TransactionId}, severity={Severity}",
            alert.Id, transaction.TransactionId, severity);

        return alert.Id;
    }
}
