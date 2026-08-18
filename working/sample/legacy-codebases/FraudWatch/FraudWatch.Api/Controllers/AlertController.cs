using FraudWatch.Core.Interfaces;
using FraudWatch.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace FraudWatch.Api.Controllers;

/// <summary>
/// Manages fraud alerts - creation, acknowledgment, and resolution.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AlertController : ControllerBase
{
    private readonly IFraudAlertRepository _alertRepository;
    private readonly ILogger<AlertController> _logger;

    public AlertController(
        IFraudAlertRepository alertRepository,
        ILogger<AlertController> logger)
    {
        _alertRepository = alertRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets fraud alerts with optional filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<FraudAlert>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<FraudAlert>>> GetAlerts(
        [FromQuery] AlertQueryParameters query,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Querying alerts with status={Status}, severity={Severity}, page={Page}",
            query.Status, query.Severity, query.Page);

        var result = await _alertRepository.GetAlertsAsync(
            status: query.Status,
            severity: query.Severity,
            fromDate: query.FromDate,
            toDate: query.ToDate,
            page: query.Page,
            pageSize: query.PageSize,
            cancellationToken: cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets a specific fraud alert by ID.
    /// </summary>
    [HttpGet("{alertId}")]
    [ProducesResponseType(typeof(FraudAlert), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FraudAlert>> GetAlert(
        string alertId,
        CancellationToken cancellationToken)
    {
        var alert = await _alertRepository.GetByIdAsync(alertId, cancellationToken);

        if (alert is null)
        {
            return NotFound($"Alert {alertId} not found.");
        }

        return Ok(alert);
    }

    /// <summary>
    /// Acknowledges a fraud alert (assigns to analyst).
    /// </summary>
    [HttpPost("{alertId}/acknowledge")]
    [ProducesResponseType(typeof(FraudAlert), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FraudAlert>> AcknowledgeAlert(
        string alertId,
        [FromBody] AcknowledgeAlertRequest request,
        CancellationToken cancellationToken)
    {
        var alert = await _alertRepository.GetByIdAsync(alertId, cancellationToken);

        if (alert is null)
        {
            return NotFound($"Alert {alertId} not found.");
        }

        if (alert.Status != AlertStatus.Open)
        {
            return Conflict($"Alert {alertId} is already in status {alert.Status}.");
        }

        var updatedAlert = alert with
        {
            Status = AlertStatus.Acknowledged,
            AssignedAnalyst = request.AnalystId,
            AcknowledgedAt = DateTimeOffset.UtcNow
        };

        await _alertRepository.UpdateAsync(updatedAlert, cancellationToken);

        _logger.LogInformation("Alert {AlertId} acknowledged by analyst {AnalystId}",
            alertId, request.AnalystId);

        return Ok(updatedAlert);
    }

    /// <summary>
    /// Resolves a fraud alert with a disposition.
    /// </summary>
    [HttpPost("{alertId}/resolve")]
    [ProducesResponseType(typeof(FraudAlert), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FraudAlert>> ResolveAlert(
        string alertId,
        [FromBody] ResolveAlertRequest request,
        CancellationToken cancellationToken)
    {
        var alert = await _alertRepository.GetByIdAsync(alertId, cancellationToken);

        if (alert is null)
        {
            return NotFound($"Alert {alertId} not found.");
        }

        var resolvedAlert = alert with
        {
            Status = AlertStatus.Resolved,
            Disposition = request.Disposition,
            ResolutionNotes = request.Notes,
            ResolvedAt = DateTimeOffset.UtcNow
        };

        await _alertRepository.UpdateAsync(resolvedAlert, cancellationToken);

        _logger.LogInformation(
            "Alert {AlertId} resolved with disposition {Disposition}",
            alertId, request.Disposition);

        return Ok(resolvedAlert);
    }

    /// <summary>
    /// Gets alert statistics for the dashboard.
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(AlertStatistics), StatusCodes.Status200OK)]
    public async Task<ActionResult<AlertStatistics>> GetStatistics(
        [FromQuery] int daysBack = 7,
        CancellationToken cancellationToken = default)
    {
        var stats = await _alertRepository.GetStatisticsAsync(
            DateTimeOffset.UtcNow.AddDays(-daysBack), cancellationToken);

        return Ok(stats);
    }
}

public record AlertQueryParameters
{
    public AlertStatus? Status { get; init; }
    public AlertSeverity? Severity { get; init; }
    public DateTimeOffset? FromDate { get; init; }
    public DateTimeOffset? ToDate { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public record AcknowledgeAlertRequest(string AnalystId);

public record ResolveAlertRequest(
    AlertDisposition Disposition,
    string? Notes);

public record AlertStatistics(
    int TotalAlerts,
    int OpenAlerts,
    int AcknowledgedAlerts,
    int ResolvedAlerts,
    int ConfirmedFraud,
    int FalsePositives,
    double AverageResolutionTimeHours);
