namespace FraudWatch.Core.Models;

/// <summary>
/// Represents a fraud alert generated when a transaction exceeds risk thresholds.
/// </summary>
public record FraudAlert
{
    public string Id { get; init; } = string.Empty;
    public string TransactionId { get; init; } = string.Empty;
    public string MerchantId { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public decimal TransactionAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public RiskScore RiskScore { get; init; } = null!;
    public AlertSeverity Severity { get; init; }
    public AlertStatus Status { get; init; } = AlertStatus.Open;
    public string? AssignedAnalyst { get; init; }
    public AlertDisposition? Disposition { get; init; }
    public string? ResolutionNotes { get; init; }
    public IReadOnlyList<string> TriggeredRules { get; init; } = Array.Empty<string>();
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? AcknowledgedAt { get; init; }
    public DateTimeOffset? ResolvedAt { get; init; }
}

/// <summary>
/// Alert severity level.
/// </summary>
public enum AlertSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Current status of a fraud alert.
/// </summary>
public enum AlertStatus
{
    Open,
    Acknowledged,
    Resolved,
    Escalated
}

/// <summary>
/// Final disposition of a resolved alert.
/// </summary>
public enum AlertDisposition
{
    ConfirmedFraud,
    SuspiciousActivity,
    FalsePositive,
    CustomerVerified,
    InsufficientEvidence
}

/// <summary>
/// Paged result wrapper for alert queries.
/// </summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
