using FraudWatch.Core.Interfaces;
using FraudWatch.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace FraudWatch.Api.Controllers;

/// <summary>
/// CRUD operations for fraud detection rules.
/// Rules are configurable conditions that flag transactions for review.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RuleController : ControllerBase
{
    private readonly IRuleEvaluator _ruleEvaluator;
    private readonly ICacheService _cacheService;
    private readonly ILogger<RuleController> _logger;

    public RuleController(
        IRuleEvaluator ruleEvaluator,
        ICacheService cacheService,
        ILogger<RuleController> logger)
    {
        _ruleEvaluator = ruleEvaluator;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all active fraud detection rules.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FraudRule>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FraudRule>>> GetRules(
        [FromQuery] bool includeDisabled = false,
        CancellationToken cancellationToken = default)
    {
        var rules = await _ruleEvaluator.GetAllRulesAsync(cancellationToken);

        if (!includeDisabled)
        {
            rules = rules.Where(r => r.IsEnabled);
        }

        return Ok(rules);
    }

    /// <summary>
    /// Gets a specific rule by ID.
    /// </summary>
    [HttpGet("{ruleId}")]
    [ProducesResponseType(typeof(FraudRule), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FraudRule>> GetRule(
        string ruleId,
        CancellationToken cancellationToken)
    {
        var rule = await _ruleEvaluator.GetRuleByIdAsync(ruleId, cancellationToken);

        if (rule is null)
        {
            return NotFound($"Rule {ruleId} not found.");
        }

        return Ok(rule);
    }

    /// <summary>
    /// Creates a new fraud detection rule.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(FraudRule), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FraudRule>> CreateRule(
        [FromBody] CreateRuleRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Rule name is required.");
        }

        var rule = new FraudRule
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            Condition = request.Condition,
            RiskWeight = request.RiskWeight,
            IsEnabled = request.IsEnabled,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _ruleEvaluator.CreateRuleAsync(rule, cancellationToken);
        await _cacheService.InvalidateAsync("fraud-rules", cancellationToken);

        _logger.LogInformation("Created fraud rule {RuleId}: {RuleName}", rule.Id, rule.Name);

        return CreatedAtAction(nameof(GetRule), new { ruleId = rule.Id }, rule);
    }

    /// <summary>
    /// Updates an existing fraud detection rule.
    /// </summary>
    [HttpPut("{ruleId}")]
    [ProducesResponseType(typeof(FraudRule), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FraudRule>> UpdateRule(
        string ruleId,
        [FromBody] UpdateRuleRequest request,
        CancellationToken cancellationToken)
    {
        var existingRule = await _ruleEvaluator.GetRuleByIdAsync(ruleId, cancellationToken);

        if (existingRule is null)
        {
            return NotFound($"Rule {ruleId} not found.");
        }

        var updatedRule = existingRule with
        {
            Name = request.Name ?? existingRule.Name,
            Description = request.Description ?? existingRule.Description,
            Condition = request.Condition ?? existingRule.Condition,
            RiskWeight = request.RiskWeight ?? existingRule.RiskWeight,
            IsEnabled = request.IsEnabled ?? existingRule.IsEnabled,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _ruleEvaluator.UpdateRuleAsync(updatedRule, cancellationToken);
        await _cacheService.InvalidateAsync("fraud-rules", cancellationToken);

        _logger.LogInformation("Updated fraud rule {RuleId}", ruleId);

        return Ok(updatedRule);
    }

    /// <summary>
    /// Deletes a fraud detection rule.
    /// </summary>
    [HttpDelete("{ruleId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRule(
        string ruleId,
        CancellationToken cancellationToken)
    {
        var existingRule = await _ruleEvaluator.GetRuleByIdAsync(ruleId, cancellationToken);

        if (existingRule is null)
        {
            return NotFound($"Rule {ruleId} not found.");
        }

        await _ruleEvaluator.DeleteRuleAsync(ruleId, cancellationToken);
        await _cacheService.InvalidateAsync("fraud-rules", cancellationToken);

        _logger.LogInformation("Deleted fraud rule {RuleId}: {RuleName}", ruleId, existingRule.Name);

        return NoContent();
    }

    /// <summary>
    /// Tests a rule against a sample transaction without persisting.
    /// </summary>
    [HttpPost("{ruleId}/test")]
    [ProducesResponseType(typeof(RuleTestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RuleTestResult>> TestRule(
        string ruleId,
        [FromBody] TransactionCheckRequest transaction,
        CancellationToken cancellationToken)
    {
        var rule = await _ruleEvaluator.GetRuleByIdAsync(ruleId, cancellationToken);

        if (rule is null)
        {
            return NotFound($"Rule {ruleId} not found.");
        }

        var txn = new Transaction(
            TransactionId: transaction.TransactionId,
            MerchantId: transaction.MerchantId,
            CustomerId: transaction.CustomerId,
            Amount: transaction.Amount,
            Currency: transaction.Currency,
            Timestamp: transaction.Timestamp ?? DateTimeOffset.UtcNow,
            CardLast4: transaction.CardLast4,
            IpAddress: transaction.IpAddress,
            Country: transaction.Country,
            Channel: transaction.Channel ?? TransactionChannel.Online);

        var triggered = await _ruleEvaluator.EvaluateSingleRuleAsync(rule, txn, cancellationToken);

        return Ok(new RuleTestResult(
            RuleId: ruleId,
            RuleName: rule.Name,
            Triggered: triggered,
            RiskWeight: rule.RiskWeight));
    }
}

public record CreateRuleRequest(
    string Name,
    string? Description,
    RuleCategory Category,
    RuleCondition Condition,
    double RiskWeight,
    bool IsEnabled = true);

public record UpdateRuleRequest(
    string? Name,
    string? Description,
    RuleCondition? Condition,
    double? RiskWeight,
    bool? IsEnabled);

public record RuleTestResult(
    string RuleId,
    string RuleName,
    bool Triggered,
    double RiskWeight);
