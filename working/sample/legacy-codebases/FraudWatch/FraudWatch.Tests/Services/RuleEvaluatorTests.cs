using FraudWatch.Core.Interfaces;
using FraudWatch.Core.Models;
using FraudWatch.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FraudWatch.Tests.Services;

public class RuleEvaluatorTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IFraudAlertRepository> _alertRepositoryMock;
    private readonly Mock<ILogger<RuleEvaluator>> _loggerMock;
    private readonly RuleEvaluator _evaluator;

    public RuleEvaluatorTests()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _alertRepositoryMock = new Mock<IFraudAlertRepository>();
        _loggerMock = new Mock<ILogger<RuleEvaluator>>();

        // Return null from cache to force loading from store
        _cacheServiceMock
            .Setup(x => x.GetAsync<List<FraudRule>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<FraudRule>?)null);

        _evaluator = new RuleEvaluator(
            _cacheServiceMock.Object,
            _alertRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task EvaluateAsync_HighAmountTransaction_TriggersAmountRule()
    {
        // Arrange
        var transaction = new Transaction(
            TransactionId: "TXN-001",
            MerchantId: "MERCH-001",
            CustomerId: "CUST-001",
            Amount: 15000m,
            Currency: "USD",
            Timestamp: DateTimeOffset.UtcNow,
            CardLast4: "4242",
            IpAddress: "192.168.1.1",
            Country: "US",
            Channel: TransactionChannel.Online);

        // Act
        var triggeredRules = await _evaluator.EvaluateAsync(transaction);

        // Assert
        Assert.Contains("rule-001", triggeredRules); // High amount rule
    }

    [Fact]
    public async Task EvaluateAsync_HighRiskCountry_TriggersGeoRule()
    {
        // Arrange
        var transaction = new Transaction(
            TransactionId: "TXN-002",
            MerchantId: "MERCH-001",
            CustomerId: "CUST-001",
            Amount: 500m,
            Currency: "USD",
            Timestamp: DateTimeOffset.UtcNow,
            CardLast4: "1234",
            IpAddress: "203.0.113.1",
            Country: "NG",
            Channel: TransactionChannel.Online);

        // Act
        var triggeredRules = await _evaluator.EvaluateAsync(transaction);

        // Assert
        Assert.Contains("rule-002", triggeredRules); // Cross-border high risk rule
    }

    [Fact]
    public async Task EvaluateAsync_NormalTransaction_TriggersNoRules()
    {
        // Arrange
        var transaction = new Transaction(
            TransactionId: "TXN-003",
            MerchantId: "MERCH-001",
            CustomerId: "CUST-001",
            Amount: 50m,
            Currency: "USD",
            Timestamp: DateTimeOffset.UtcNow.Date.AddHours(14), // 2PM - not late night
            CardLast4: "9876",
            IpAddress: "10.0.0.1",
            Country: "US",
            Channel: TransactionChannel.InStore);

        // Act
        var triggeredRules = await _evaluator.EvaluateAsync(transaction);

        // Assert
        Assert.Empty(triggeredRules);
    }

    [Fact]
    public async Task EvaluateSingleRuleAsync_SimpleGreaterThan_EvaluatesCorrectly()
    {
        // Arrange
        var rule = new FraudRule
        {
            Id = "test-rule-1",
            Name = "Test Rule",
            Category = RuleCategory.Amount,
            Condition = new RuleCondition
            {
                Type = ConditionType.Simple,
                Field = "Amount",
                Operator = ComparisonOperator.GreaterThan,
                Value = "5000"
            },
            RiskWeight = 0.5,
            IsEnabled = true
        };

        var highAmountTxn = new Transaction(
            "TXN-1", "M-1", "C-1", 7000m, "USD",
            DateTimeOffset.UtcNow, "1111", "10.0.0.1", "US", TransactionChannel.Online);

        var lowAmountTxn = new Transaction(
            "TXN-2", "M-1", "C-1", 100m, "USD",
            DateTimeOffset.UtcNow, "1111", "10.0.0.1", "US", TransactionChannel.Online);

        // Act
        var highTriggered = await _evaluator.EvaluateSingleRuleAsync(rule, highAmountTxn);
        var lowTriggered = await _evaluator.EvaluateSingleRuleAsync(rule, lowAmountTxn);

        // Assert
        Assert.True(highTriggered);
        Assert.False(lowTriggered);
    }

    [Fact]
    public async Task EvaluateSingleRuleAsync_InOperator_MatchesValueInList()
    {
        // Arrange
        var rule = new FraudRule
        {
            Id = "test-rule-geo",
            Name = "Geo Rule",
            Category = RuleCategory.Geographic,
            Condition = new RuleCondition
            {
                Type = ConditionType.Simple,
                Field = "Country",
                Operator = ComparisonOperator.In,
                Value = "NG,RO,UA"
            },
            RiskWeight = 0.5,
            IsEnabled = true
        };

        var matchTxn = new Transaction(
            "TXN-1", "M-1", "C-1", 100m, "USD",
            DateTimeOffset.UtcNow, "1111", "10.0.0.1", "RO", TransactionChannel.Online);

        var noMatchTxn = new Transaction(
            "TXN-2", "M-1", "C-1", 100m, "USD",
            DateTimeOffset.UtcNow, "1111", "10.0.0.1", "US", TransactionChannel.Online);

        // Act
        var matchResult = await _evaluator.EvaluateSingleRuleAsync(rule, matchTxn);
        var noMatchResult = await _evaluator.EvaluateSingleRuleAsync(rule, noMatchTxn);

        // Assert
        Assert.True(matchResult);
        Assert.False(noMatchResult);
    }

    [Fact]
    public async Task EvaluateSingleRuleAsync_CompoundAndCondition_RequiresBothTrue()
    {
        // Arrange
        var rule = new FraudRule
        {
            Id = "test-compound",
            Name = "Compound Rule",
            Category = RuleCategory.Behavioral,
            Condition = new RuleCondition
            {
                Type = ConditionType.Compound,
                Field = "",
                Operator = ComparisonOperator.Equals,
                Value = "",
                LogicalOp = LogicalOperator.And,
                SubConditions = new List<RuleCondition>
                {
                    new() { Type = ConditionType.Simple, Field = "Amount", Operator = ComparisonOperator.GreaterThan, Value = "5000" },
                    new() { Type = ConditionType.Simple, Field = "Country", Operator = ComparisonOperator.In, Value = "NG,RO" }
                }
            },
            RiskWeight = 0.8,
            IsEnabled = true
        };

        // Both conditions met
        var bothTxn = new Transaction(
            "TXN-1", "M-1", "C-1", 7000m, "USD",
            DateTimeOffset.UtcNow, "1111", "10.0.0.1", "NG", TransactionChannel.Online);

        // Only amount condition met
        var onlyAmountTxn = new Transaction(
            "TXN-2", "M-1", "C-1", 7000m, "USD",
            DateTimeOffset.UtcNow, "1111", "10.0.0.1", "US", TransactionChannel.Online);

        // Act
        var bothResult = await _evaluator.EvaluateSingleRuleAsync(rule, bothTxn);
        var onlyAmountResult = await _evaluator.EvaluateSingleRuleAsync(rule, onlyAmountTxn);

        // Assert
        Assert.True(bothResult);
        Assert.False(onlyAmountResult);
    }

    [Fact]
    public async Task GetAllRulesAsync_ReturnsPreConfiguredRules()
    {
        // Act
        var rules = await _evaluator.GetAllRulesAsync();

        // Assert
        Assert.NotEmpty(rules);
        Assert.All(rules, rule =>
        {
            Assert.NotEmpty(rule.Id);
            Assert.NotEmpty(rule.Name);
            Assert.True(rule.RiskWeight > 0);
        });
    }

    [Fact]
    public async Task CreateRuleAsync_AddsNewRule()
    {
        // Arrange
        var newRule = new FraudRule
        {
            Id = "new-test-rule",
            Name = "Test Created Rule",
            Category = RuleCategory.Blacklist,
            Condition = new RuleCondition
            {
                Type = ConditionType.Simple,
                Field = "CardLast4",
                Operator = ComparisonOperator.In,
                Value = "0000,9999"
            },
            RiskWeight = 0.9,
            IsEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Act
        await _evaluator.CreateRuleAsync(newRule);
        var retrieved = await _evaluator.GetRuleByIdAsync("new-test-rule");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Test Created Rule", retrieved.Name);

        // Cleanup
        await _evaluator.DeleteRuleAsync("new-test-rule");
    }
}
