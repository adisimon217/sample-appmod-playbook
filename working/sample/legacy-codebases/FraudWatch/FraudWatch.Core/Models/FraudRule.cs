namespace FraudWatch.Core.Models;

/// <summary>
/// Configurable fraud detection rule.
/// Rules define conditions under which a transaction is flagged.
/// </summary>
public record FraudRule
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public RuleCategory Category { get; init; }
    public RuleCondition Condition { get; init; } = null!;
    public double RiskWeight { get; init; }
    public bool IsEnabled { get; init; } = true;
    public int Priority { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// Category of fraud rule for organization and filtering.
/// </summary>
public enum RuleCategory
{
    Velocity,
    Amount,
    Geographic,
    Device,
    Behavioral,
    Pattern,
    Blacklist
}

/// <summary>
/// Rule condition that defines when a rule triggers.
/// </summary>
public record RuleCondition
{
    public ConditionType Type { get; init; }
    public string Field { get; init; } = string.Empty;
    public ComparisonOperator Operator { get; init; }
    public string Value { get; init; } = string.Empty;
    public string? SecondaryValue { get; init; }
    public TimeSpan? TimeWindow { get; init; }
    public IReadOnlyList<RuleCondition>? SubConditions { get; init; }
    public LogicalOperator? LogicalOp { get; init; }
}

public enum ConditionType
{
    Simple,
    Aggregate,
    Compound,
    Velocity,
    Pattern
}

public enum ComparisonOperator
{
    GreaterThan,
    LessThan,
    Equals,
    NotEquals,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Contains,
    In,
    NotIn,
    Between
}

public enum LogicalOperator
{
    And,
    Or
}

/// <summary>
/// Result of evaluating a transaction against the fraud detection engine.
/// </summary>
public record FraudCheckResult(
    string TransactionId,
    RiskScore RiskScore,
    FraudDecision Decision,
    IReadOnlyList<string> TriggeredRules,
    string? AlertId,
    DateTimeOffset EvaluatedAt);

/// <summary>
/// Decision made by the fraud detection engine.
/// </summary>
public enum FraudDecision
{
    Approve,
    Decline,
    Review,
    Challenge
}
