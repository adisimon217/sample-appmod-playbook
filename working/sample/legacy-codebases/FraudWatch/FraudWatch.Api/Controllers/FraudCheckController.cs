using FraudWatch.Core.Interfaces;
using FraudWatch.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace FraudWatch.Api.Controllers;

/// <summary>
/// Real-time fraud check endpoint for payment transaction screening.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FraudCheckController : ControllerBase
{
    private readonly IFraudDetectionService _fraudDetectionService;
    private readonly IRiskScoringEngine _riskScoringEngine;
    private readonly ILogger<FraudCheckController> _logger;

    public FraudCheckController(
        IFraudDetectionService fraudDetectionService,
        IRiskScoringEngine riskScoringEngine,
        ILogger<FraudCheckController> logger)
    {
        _fraudDetectionService = fraudDetectionService;
        _riskScoringEngine = riskScoringEngine;
        _logger = logger;
    }

    /// <summary>
    /// Performs real-time fraud check on a transaction.
    /// Returns risk score and recommendation (approve/decline/review).
    /// </summary>
    [HttpPost("evaluate")]
    [ProducesResponseType(typeof(FraudCheckResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<FraudCheckResult>> EvaluateTransaction(
        [FromBody] TransactionCheckRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            _logger.LogWarning("Invalid transaction amount: {Amount} for merchant {MerchantId}",
                request.Amount, request.MerchantId);
            return BadRequest("Transaction amount must be positive.");
        }

        _logger.LogInformation(
            "Evaluating transaction {TransactionId} for merchant {MerchantId}, amount {Amount} {Currency}",
            request.TransactionId, request.MerchantId, request.Amount, request.Currency);

        try
        {
            var transaction = new Transaction(
                TransactionId: request.TransactionId,
                MerchantId: request.MerchantId,
                CustomerId: request.CustomerId,
                Amount: request.Amount,
                Currency: request.Currency,
                Timestamp: request.Timestamp ?? DateTimeOffset.UtcNow,
                CardLast4: request.CardLast4,
                IpAddress: request.IpAddress,
                Country: request.Country,
                Channel: request.Channel ?? TransactionChannel.Online);

            var result = await _fraudDetectionService.EvaluateTransactionAsync(
                transaction, cancellationToken);

            _logger.LogInformation(
                "Transaction {TransactionId} scored {Score:F3}, decision: {Decision}",
                request.TransactionId, result.RiskScore.Score, result.Decision);

            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Fraud check cancelled for transaction {TransactionId}",
                request.TransactionId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                "Fraud check timed out. Please retry.");
        }
    }

    /// <summary>
    /// Batch fraud check for multiple transactions.
    /// </summary>
    [HttpPost("evaluate/batch")]
    [ProducesResponseType(typeof(IEnumerable<FraudCheckResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<FraudCheckResult>>> EvaluateBatch(
        [FromBody] BatchTransactionCheckRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Transactions is null || !request.Transactions.Any())
        {
            return BadRequest("At least one transaction is required.");
        }

        if (request.Transactions.Count() > 100)
        {
            return BadRequest("Maximum 100 transactions per batch.");
        }

        _logger.LogInformation("Processing batch fraud check for {Count} transactions",
            request.Transactions.Count());

        var results = new List<FraudCheckResult>();

        await foreach (var result in _fraudDetectionService.EvaluateBatchAsync(
            request.Transactions.Select(t => new Transaction(
                TransactionId: t.TransactionId,
                MerchantId: t.MerchantId,
                CustomerId: t.CustomerId,
                Amount: t.Amount,
                Currency: t.Currency,
                Timestamp: t.Timestamp ?? DateTimeOffset.UtcNow,
                CardLast4: t.CardLast4,
                IpAddress: t.IpAddress,
                Country: t.Country,
                Channel: t.Channel ?? TransactionChannel.Online)),
            cancellationToken))
        {
            results.Add(result);
        }

        return Ok(results);
    }

    /// <summary>
    /// Gets the current risk score for a customer based on historical data.
    /// </summary>
    [HttpGet("risk-profile/{customerId}")]
    [ProducesResponseType(typeof(CustomerRiskProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerRiskProfile>> GetCustomerRiskProfile(
        string customerId,
        CancellationToken cancellationToken)
    {
        var profile = await _riskScoringEngine.GetCustomerRiskProfileAsync(
            customerId, cancellationToken);

        if (profile is null)
        {
            return NotFound($"No risk profile found for customer {customerId}");
        }

        return Ok(profile);
    }
}

/// <summary>
/// Request model for single transaction fraud check.
/// </summary>
public record TransactionCheckRequest(
    string TransactionId,
    string MerchantId,
    string CustomerId,
    decimal Amount,
    string Currency,
    string? CardLast4,
    string? IpAddress,
    string? Country,
    TransactionChannel? Channel = TransactionChannel.Online,
    DateTimeOffset? Timestamp = null);

/// <summary>
/// Request model for batch transaction fraud check.
/// </summary>
public record BatchTransactionCheckRequest(
    IEnumerable<TransactionCheckRequest> Transactions);
