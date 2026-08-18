using Dapper;
using FraudWatch.Core.Interfaces;
using FraudWatch.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FraudWatch.Infrastructure.Data;

/// <summary>
/// PostgreSQL-backed repository for fraud alerts using Dapper.
/// </summary>
public class FraudAlertRepository : IFraudAlertRepository
{
    private readonly string _connectionString;
    private readonly ILogger<FraudAlertRepository> _logger;

    public FraudAlertRepository(
        IConfiguration configuration,
        ILogger<FraudAlertRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("FraudDb")
            ?? throw new InvalidOperationException("FraudDb connection string not configured");
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<FraudAlert?> GetByIdAsync(
        string alertId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, transaction_id, merchant_id, customer_id,
                   transaction_amount, currency, severity, status,
                   assigned_analyst, disposition, resolution_notes,
                   triggered_rules, created_at, acknowledged_at, resolved_at
            FROM fraud_alerts
            WHERE id = @AlertId";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<AlertRow>(
            sql, new { AlertId = alertId });

        return row is not null ? MapToFraudAlert(row) : null;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<FraudAlert>> GetAlertsAsync(
        AlertStatus? status = null,
        AlertSeverity? severity = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (status.HasValue)
        {
            conditions.Add("status = @Status");
            parameters.Add("Status", status.Value.ToString());
        }

        if (severity.HasValue)
        {
            conditions.Add("severity = @Severity");
            parameters.Add("Severity", severity.Value.ToString());
        }

        if (fromDate.HasValue)
        {
            conditions.Add("created_at >= @FromDate");
            parameters.Add("FromDate", fromDate.Value);
        }

        if (toDate.HasValue)
        {
            conditions.Add("created_at <= @ToDate");
            parameters.Add("ToDate", toDate.Value);
        }

        var whereClause = conditions.Any()
            ? "WHERE " + string.Join(" AND ", conditions)
            : "";

        var offset = (page - 1) * pageSize;
        parameters.Add("Offset", offset);
        parameters.Add("Limit", pageSize);

        var countSql = $"SELECT COUNT(*) FROM fraud_alerts {whereClause}";
        var dataSql = $@"
            SELECT id, transaction_id, merchant_id, customer_id,
                   transaction_amount, currency, severity, status,
                   assigned_analyst, disposition, resolution_notes,
                   triggered_rules, created_at, acknowledged_at, resolved_at
            FROM fraud_alerts
            {whereClause}
            ORDER BY created_at DESC
            OFFSET @Offset LIMIT @Limit";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var rows = await connection.QueryAsync<AlertRow>(dataSql, parameters);

        var alerts = rows.Select(MapToFraudAlert).ToList();

        return new PagedResult<FraudAlert>(alerts, totalCount, page, pageSize);
    }

    /// <inheritdoc/>
    public async Task CreateAsync(
        FraudAlert alert,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO fraud_alerts
                (id, transaction_id, merchant_id, customer_id,
                 transaction_amount, currency, severity, status,
                 triggered_rules, created_at)
            VALUES
                (@Id, @TransactionId, @MerchantId, @CustomerId,
                 @TransactionAmount, @Currency, @Severity, @Status,
                 @TriggeredRules, @CreatedAt)";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(sql, new
        {
            alert.Id,
            alert.TransactionId,
            alert.MerchantId,
            alert.CustomerId,
            alert.TransactionAmount,
            alert.Currency,
            Severity = alert.Severity.ToString(),
            Status = alert.Status.ToString(),
            TriggeredRules = string.Join(",", alert.TriggeredRules),
            alert.CreatedAt
        });

        _logger.LogDebug("Created fraud alert {AlertId} for transaction {TransactionId}",
            alert.Id, alert.TransactionId);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(
        FraudAlert alert,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE fraud_alerts
            SET status = @Status,
                assigned_analyst = @AssignedAnalyst,
                disposition = @Disposition,
                resolution_notes = @ResolutionNotes,
                acknowledged_at = @AcknowledgedAt,
                resolved_at = @ResolvedAt
            WHERE id = @Id";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(sql, new
        {
            alert.Id,
            Status = alert.Status.ToString(),
            alert.AssignedAnalyst,
            Disposition = alert.Disposition?.ToString(),
            alert.ResolutionNotes,
            alert.AcknowledgedAt,
            alert.ResolvedAt
        });

        _logger.LogDebug("Updated fraud alert {AlertId}, status={Status}", alert.Id, alert.Status);
    }

    /// <inheritdoc/>
    public async Task<AlertStatistics> GetStatisticsAsync(
        DateTimeOffset fromDate,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                COUNT(*) as total_alerts,
                COUNT(*) FILTER (WHERE status = 'Open') as open_alerts,
                COUNT(*) FILTER (WHERE status = 'Acknowledged') as acknowledged_alerts,
                COUNT(*) FILTER (WHERE status = 'Resolved') as resolved_alerts,
                COUNT(*) FILTER (WHERE disposition = 'ConfirmedFraud') as confirmed_fraud,
                COUNT(*) FILTER (WHERE disposition = 'FalsePositive') as false_positives,
                COALESCE(AVG(EXTRACT(EPOCH FROM (resolved_at - created_at)) / 3600)
                    FILTER (WHERE resolved_at IS NOT NULL), 0) as avg_resolution_hours
            FROM fraud_alerts
            WHERE created_at >= @FromDate";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var result = await connection.QuerySingleAsync<StatsRow>(sql, new { FromDate = fromDate });

        return new AlertStatistics(
            TotalAlerts: result.TotalAlerts,
            OpenAlerts: result.OpenAlerts,
            AcknowledgedAlerts: result.AcknowledgedAlerts,
            ResolvedAlerts: result.ResolvedAlerts,
            ConfirmedFraud: result.ConfirmedFraud,
            FalsePositives: result.FalsePositives,
            AverageResolutionTimeHours: result.AvgResolutionHours);
    }

    private static FraudAlert MapToFraudAlert(AlertRow row) => new()
    {
        Id = row.Id,
        TransactionId = row.TransactionId,
        MerchantId = row.MerchantId,
        CustomerId = row.CustomerId,
        TransactionAmount = row.TransactionAmount,
        Currency = row.Currency,
        Severity = Enum.Parse<AlertSeverity>(row.Severity),
        Status = Enum.Parse<AlertStatus>(row.Status),
        AssignedAnalyst = row.AssignedAnalyst,
        Disposition = row.Disposition is not null
            ? Enum.Parse<AlertDisposition>(row.Disposition)
            : null,
        ResolutionNotes = row.ResolutionNotes,
        TriggeredRules = row.TriggeredRules?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            ?? Array.Empty<string>(),
        CreatedAt = row.CreatedAt,
        AcknowledgedAt = row.AcknowledgedAt,
        ResolvedAt = row.ResolvedAt
    };
}

/// <summary>Internal Dapper row mapping for fraud_alerts table.</summary>
internal class AlertRow
{
    public string Id { get; init; } = string.Empty;
    public string TransactionId { get; init; } = string.Empty;
    public string MerchantId { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public decimal TransactionAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public string Severity { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? AssignedAnalyst { get; init; }
    public string? Disposition { get; init; }
    public string? ResolutionNotes { get; init; }
    public string? TriggeredRules { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? AcknowledgedAt { get; init; }
    public DateTimeOffset? ResolvedAt { get; init; }
}

/// <summary>Internal Dapper row mapping for statistics query.</summary>
internal class StatsRow
{
    public int TotalAlerts { get; init; }
    public int OpenAlerts { get; init; }
    public int AcknowledgedAlerts { get; init; }
    public int ResolvedAlerts { get; init; }
    public int ConfirmedFraud { get; init; }
    public int FalsePositives { get; init; }
    public double AvgResolutionHours { get; init; }
}
