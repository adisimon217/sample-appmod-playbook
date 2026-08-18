using FraudWatch.Core.Interfaces;
using FraudWatch.Core.Models;
using Microsoft.Extensions.Logging;

namespace FraudWatch.Core.Services;

/// <summary>
/// Evaluates configurable fraud rules against incoming transactions.
/// Rules are cached and support complex compound conditions.
/// </summary>
public class RuleEvaluator : IRuleEvaluator
{
    private readonly ICacheService _cacheService;
    private readonly IFraudAlertRepository _alertRepository;
    private readonly ILogger<RuleEvaluator> _logger;

    private const string RulesCacheKey = "fraud-rules";
    private static readonly TimeSpan RuleCacheDuration = TimeSpan.FromMinutes(15);

    // In-memory store for demonstration (production would use database)
    private static readonly List<FraudRule> _ruleStore = new()
    {
        new FraudRule
        {
            Id = "rule-001",
            Name = "High Amount Single Transaction",
            Description = "Flags transactions exceeding $10,000",
            Category = RuleCategory.Amount,
            Condition = new RuleCondition
            {
                Type = ConditionType.Simple,
                Field = "Amount",
                Operator = ComparisonOperator.GreaterThan,
                Value = "10000"
            },
            RiskWeight = 0.6,
            IsEnabled = true,
            Priority = 1,
            CreatedAt = DateTimeOffset.Parse("2023-01-15T00:00:00Z"),
            UpdatedAt = DateTimeOffset.Parse("2023-06-20T00:00:00Z")
        },
        new FraudRule
        {
            Id = "rule-002",
            Name = "Cross-Border High Risk",
            Description = "Flags transactions from known high-risk countries",
            Category = RuleCategory.Geographic,
            Condition = new RuleCondition
            {
                Type = ConditionType.Simple,
                Field = "Country",
                Operator = ComparisonOperator.In,
                Value = "NG,RO,UA,PH,ID,VN,GH,KE"
            },
            RiskWeight = 0.5,
            IsEnabled = true,
            Priority = 2,
            CreatedAt = DateTimeOffset.Parse("2023-01-15T00:00:00Z"),
            UpdatedAt = DateTimeOffset.Parse("2023-08-10T00:00:00Z")
        },
        new FraudRule
        {
            Id = "rule-003",
            Name = "Velocity Breach",
            Description = "More than 5 transactions within 10 minutes",
            Category = RuleCategory.Velocity,
            Condition = new RuleCondition
            {
                Type = ConditionType.Velocity,
                Field = "TransactionCount",
                Operator = ComparisonOperator.GreaterThan,
                Value = "5",
                TimeWindow = TimeSpan.FromMinutes(10)
            },
            RiskWeight = 0.7,
            IsEnabled = true,
            Priority = 1,
            CreatedAt = DateTimeOffset.Parse("2023-02-01T00:00:00Z"),
            UpdatedAt = DateTimeOffset.Parse("2023-02-01T00:00:00Z")
        },
        new FraudRule
        {
            Id = "rule-004",
            Name = "Late Night High Value",
            Description = "High-value transactions during unusual hours (2AM-5AM)",
            Category = RuleCategory.Behavioral,
            Condition = new RuleCondition
            {
                Type = ConditionType.Compound,
                LogicalOp = LogicalOperator.And,
                Field = "",
                Operator = ComparisonOperator.Equals,
                Value = "",
                SubConditions = new List<RuleCondition>
                {
                    new() { Type = ConditionType.Simple, Field = "Amount", Operator = ComparisonOperator.GreaterThan, Value = "5000" },
                    new() { Type = ConditionType.Simple, Field = "Hour", Operator = ComparisonOperator.Between, Value = "2", SecondaryValue = "5" }
                }
            },
            RiskWeight = 0.4,
            IsEnabled = true,
            Priority = 3,
            CreatedAt = DateTimeOffset.Parse("2023-03-15T00:00:00Z"),
            UpdatedAt = DateTimeOffset.Parse("2023-03-15T00:00:00Z")
        }
    };

    public RuleEvaluator(
        ICacheService cacheService,
        IFraudAlertRepository alertRepository,
        ILogger<RuleEvaluator> logger)
    {
        _cacheService = cacheService;
        _alertRepository = alertRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> EvaluateAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        var rules = await GetActiveRulesAsync(cancellationToken);
        var triggeredRuleIds = new List<string>();

        foreach (var rule in rules.OrderBy(r => r.Priority))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var triggered = await EvaluateSingleRuleAsync(rule, transaction, cancellationToken);

                if (triggered)
                {
                    triggeredRuleIds.Add(rule.Id);
                    _logger.LogDebug(
                        "Rule {RuleId} ({RuleName}) triggered for transaction {TransactionId}",
                        rule.Id, rule.Name, transaction.TransactionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error evaluating rule {RuleId} for transaction {TransactionId}",
                    rule.Id, transaction.TransactionId);
            }
        }

        return triggeredRuleIds;
    }

    /// <inheritdoc/>
    public Task<bool> EvaluateSingleRuleAsync(
        FraudRule rule,
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        var result = EvaluateCondition(rule.Condition, transaction);
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FraudRule>> GetAllRulesAsync(
        CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_ruleStore.AsEnumerable());
    }

    /// <inheritdoc/>
    public async Task<FraudRule?> GetRuleByIdAsync(
        string ruleId,
        CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(
            _ruleStore.FirstOrDefault(r => r.Id == ruleId));
    }

    /// <inheritdoc/>
    public async Task CreateRuleAsync(
        FraudRule rule,
        CancellationToken cancellationToken = default)
    {
        _ruleStore.Add(rule);
        await InvalidateRuleCacheAsync(cancellationToken);
        _logger.LogInformation("Created rule {RuleId}: {RuleName}", rule.Id, rule.Name);
    }

    /// <inheritdoc/>
    public async Task UpdateRuleAsync(
        FraudRule rule,
        CancellationToken cancellationToken = default)
    {
        var index = _ruleStore.FindIndex(r => r.Id == rule.Id);
        if (index >= 0)
        {
            _ruleStore[index] = rule;
            await InvalidateRuleCacheAsync(cancellationToken);
            _logger.LogInformation("Updated rule {RuleId}: {RuleName}", rule.Id, rule.Name);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteRuleAsync(
        string ruleId,
        CancellationToken cancellationToken = default)
    {
        _ruleStore.RemoveAll(r => r.Id == ruleId);
        await InvalidateRuleCacheAsync(cancellationToken);
        _logger.LogInformation("Deleted rule {RuleId}", ruleId);
    }

    private async Task<IReadOnlyList<FraudRule>> GetActiveRulesAsync(
        CancellationToken cancellationToken)
    {
        // Try to get from cache first
        var cached = await _cacheService.GetAsync<List<FraudRule>>(
            RulesCacheKey, cancellationToken);

        if (cached is not null)
        {
            return cached;
        }

        // Load and cache active rules
        var activeRules = _ruleStore.Where(r => r.IsEnabled).ToList();
        await _cacheService.SetAsync(
            RulesCacheKey, activeRules, RuleCacheDuration, cancellationToken);

        return activeRules;
    }

    private bool EvaluateCondition(RuleCondition condition, Transaction transaction)
    {
        return condition.Type switch
        {
            ConditionType.Simple => EvaluateSimpleCondition(condition, transaction),
            ConditionType.Compound => EvaluateCompoundCondition(condition, transaction),
            ConditionType.Velocity => EvaluateVelocityCondition(condition, transaction),
            ConditionType.Aggregate => EvaluateSimpleCondition(condition, transaction),
            ConditionType.Pattern => EvaluateSimpleCondition(condition, transaction),
            _ => false
        };
    }

    private bool EvaluateSimpleCondition(RuleCondition condition, Transaction transaction)
    {
        var fieldValue = GetFieldValue(condition.Field, transaction);

        return condition.Operator switch
        {
            ComparisonOperator.GreaterThan =>
                decimal.TryParse(fieldValue, out var fv) &&
                decimal.TryParse(condition.Value, out var cv) &&
                fv > cv,

            ComparisonOperator.LessThan =>
                decimal.TryParse(fieldValue, out var fvLt) &&
                decimal.TryParse(condition.Value, out var cvLt) &&
                fvLt < cvLt,

            ComparisonOperator.Equals =>
                string.Equals(fieldValue, condition.Value, StringComparison.OrdinalIgnoreCase),

            ComparisonOperator.NotEquals =>
                !string.Equals(fieldValue, condition.Value, StringComparison.OrdinalIgnoreCase),

            ComparisonOperator.In =>
                condition.Value.Split(',').Contains(fieldValue, StringComparer.OrdinalIgnoreCase),

            ComparisonOperator.NotIn =>
                !condition.Value.Split(',').Contains(fieldValue, StringComparer.OrdinalIgnoreCase),

            ComparisonOperator.Between =>
                decimal.TryParse(fieldValue, out var fvBtw) &&
                decimal.TryParse(condition.Value, out var low) &&
                decimal.TryParse(condition.SecondaryValue, out var high) &&
                fvBtw >= low && fvBtw <= high,

            _ => false
        };
    }

    private bool EvaluateCompoundCondition(RuleCondition condition, Transaction transaction)
    {
        if (condition.SubConditions is null || !condition.SubConditions.Any())
        {
            return false;
        }

        return condition.LogicalOp switch
        {
            LogicalOperator.And => condition.SubConditions.All(sc => EvaluateCondition(sc, transaction)),
            LogicalOperator.Or => condition.SubConditions.Any(sc => EvaluateCondition(sc, transaction)),
            _ => false
        };
    }

    private bool EvaluateVelocityCondition(RuleCondition condition, Transaction transaction)
    {
        // Velocity checks are handled by the scoring engine's velocity factor
        // Here we provide a simplified evaluation
        return false;
    }

    private static string GetFieldValue(string field, Transaction transaction) => field switch
    {
        "Amount" => transaction.Amount.ToString(),
        "Currency" => transaction.Currency,
        "Country" => transaction.Country ?? "",
        "Channel" => transaction.Channel.ToString(),
        "MerchantId" => transaction.MerchantId,
        "CustomerId" => transaction.CustomerId,
        "Hour" => transaction.Timestamp.UtcDateTime.Hour.ToString(),
        "CardLast4" => transaction.CardLast4 ?? "",
        "IpAddress" => transaction.IpAddress ?? "",
        _ => ""
    };

    private async Task InvalidateRuleCacheAsync(CancellationToken cancellationToken) =>
        await _cacheService.InvalidateAsync(RulesCacheKey, cancellationToken);
}
