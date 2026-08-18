using FraudWatch.Core.Models;

namespace FraudWatch.Core.Interfaces;

/// <summary>
/// Evaluates configurable fraud rules against transactions.
/// </summary>
public interface IRuleEvaluator
{
    /// <summary>
    /// Evaluates all active rules against a transaction, returning IDs of triggered rules.
    /// </summary>
    Task<IReadOnlyList<string>> EvaluateAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a single rule against a transaction.
    /// </summary>
    Task<bool> EvaluateSingleRuleAsync(
        FraudRule rule,
        Transaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configured fraud rules.
    /// </summary>
    Task<IEnumerable<FraudRule>> GetAllRulesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific rule by ID.
    /// </summary>
    Task<FraudRule?> GetRuleByIdAsync(
        string ruleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new fraud rule.
    /// </summary>
    Task CreateRuleAsync(
        FraudRule rule,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing fraud rule.
    /// </summary>
    Task UpdateRuleAsync(
        FraudRule rule,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a fraud rule by ID.
    /// </summary>
    Task DeleteRuleAsync(
        string ruleId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Cache service interface for distributed caching.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task InvalidateAsync(string key, CancellationToken cancellationToken = default);
    Task InvalidateByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for fraud alert persistence.
/// </summary>
public interface IFraudAlertRepository
{
    Task<FraudAlert?> GetByIdAsync(string alertId, CancellationToken cancellationToken = default);

    Task<PagedResult<FraudAlert>> GetAlertsAsync(
        AlertStatus? status = null,
        AlertSeverity? severity = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default);

    Task CreateAsync(FraudAlert alert, CancellationToken cancellationToken = default);
    Task UpdateAsync(FraudAlert alert, CancellationToken cancellationToken = default);

    Task<AlertStatistics> GetStatisticsAsync(
        DateTimeOffset fromDate,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Alert statistics for dashboard display.
/// </summary>
public record AlertStatistics(
    int TotalAlerts,
    int OpenAlerts,
    int AcknowledgedAlerts,
    int ResolvedAlerts,
    int ConfirmedFraud,
    int FalsePositives,
    double AverageResolutionTimeHours);

/// <summary>
/// Banking API client interface for external verification.
/// </summary>
public interface IBankingApiClient
{
    Task<BankingVerificationResult> VerifyTransactionAsync(
        string transactionId,
        string merchantId,
        decimal amount,
        CancellationToken cancellationToken = default);

    Task<AccountRiskInfo?> GetAccountRiskInfoAsync(
        string customerId,
        CancellationToken cancellationToken = default);
}

public record BankingVerificationResult(
    bool IsVerified,
    string Status,
    string? ReasonCode,
    DateTimeOffset VerifiedAt);

public record AccountRiskInfo(
    string CustomerId,
    string AccountStatus,
    int AccountAgeMonths,
    bool HasRecentChargebacks,
    int ChargebackCount90Days,
    double ExternalRiskScore);
