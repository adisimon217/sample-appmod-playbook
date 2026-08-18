using FraudWatch.Core.Interfaces;
using FraudWatch.Core.Models;
using FraudWatch.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FraudWatch.Tests.Services;

public class RiskScoringEngineTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<RiskScoringEngine>> _loggerMock;
    private readonly RiskScoringEngine _engine;

    public RiskScoringEngineTests()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<RiskScoringEngine>>();

        _engine = new RiskScoringEngine(
            _cacheServiceMock.Object,
            _loggerMock.Object);
    }

    [Theory]
    [InlineData(50, 0.0, 0.30)]
    [InlineData(1500, 0.0, 0.50)]
    [InlineData(6000, 0.0, 0.60)]
    [InlineData(15000, 0.0, 0.75)]
    [InlineData(60000, 0.0, 0.90)]
    public async Task CalculateRiskScoreAsync_AmountBasedScoring_ProducesExpectedRange(
        decimal amount, double expectedMin, double expectedMax)
    {
        // Arrange
        var transaction = CreateTransaction(amount: amount);
        var triggeredRules = Array.Empty<string>() as IReadOnlyList<string>;

        _cacheServiceMock
            .Setup(x => x.GetAsync<int?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)0);

        _cacheServiceMock
            .Setup(x => x.GetAsync<bool?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Known IP

        // Act
        var result = await _engine.CalculateRiskScoreAsync(transaction, triggeredRules);

        // Assert
        Assert.InRange(result.Score, expectedMin, expectedMax);
        Assert.NotEmpty(result.Factors);
    }

    [Fact]
    public async Task CalculateRiskScoreAsync_HighRiskCountry_IncreasesScore()
    {
        // Arrange
        var normalTransaction = CreateTransaction(country: "US");
        var highRiskTransaction = CreateTransaction(country: "NG");
        var triggeredRules = Array.Empty<string>() as IReadOnlyList<string>;

        _cacheServiceMock
            .Setup(x => x.GetAsync<int?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)0);
        _cacheServiceMock
            .Setup(x => x.GetAsync<bool?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var normalResult = await _engine.CalculateRiskScoreAsync(normalTransaction, triggeredRules);
        var highRiskResult = await _engine.CalculateRiskScoreAsync(highRiskTransaction, triggeredRules);

        // Assert
        Assert.True(highRiskResult.Score > normalResult.Score,
            $"High-risk country score ({highRiskResult.Score}) should exceed normal ({normalResult.Score})");
    }

    [Fact]
    public async Task CalculateRiskScoreAsync_MultipleTriggeredRules_IncreasesScore()
    {
        // Arrange
        var transaction = CreateTransaction();
        var noRules = Array.Empty<string>() as IReadOnlyList<string>;
        var manyRules = new List<string> { "rule-1", "rule-2", "rule-3", "rule-4" } as IReadOnlyList<string>;

        _cacheServiceMock
            .Setup(x => x.GetAsync<int?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)0);
        _cacheServiceMock
            .Setup(x => x.GetAsync<bool?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var noRuleResult = await _engine.CalculateRiskScoreAsync(transaction, noRules);
        var manyRuleResult = await _engine.CalculateRiskScoreAsync(transaction, manyRules);

        // Assert
        Assert.True(manyRuleResult.Score > noRuleResult.Score,
            "More triggered rules should increase the risk score");

        var ruleFactor = manyRuleResult.Factors.First(f => f.Name == "RuleTriggered");
        Assert.Contains("4", ruleFactor.Description);
    }

    [Fact]
    public async Task CalculateRiskScoreAsync_HighVelocity_IncreasesScore()
    {
        // Arrange
        var transaction = CreateTransaction();
        var triggeredRules = Array.Empty<string>() as IReadOnlyList<string>;

        // Simulate high velocity (many recent transactions)
        _cacheServiceMock
            .Setup(x => x.GetAsync<int?>(
                It.Is<string>(k => k.StartsWith("velocity:")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(25); // 25 transactions in the window

        _cacheServiceMock
            .Setup(x => x.GetAsync<bool?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _engine.CalculateRiskScoreAsync(transaction, triggeredRules);

        // Assert
        var velocityFactor = result.Factors.First(f => f.Name == "TransactionVelocity");
        Assert.True(velocityFactor.Contribution > 0.15,
            "High velocity should contribute significantly to risk");
    }

    [Fact]
    public async Task CalculateRiskScoreAsync_NewIpAddress_AddsBehavioralRisk()
    {
        // Arrange
        var transaction = CreateTransaction(ipAddress: "10.0.0.99");
        var triggeredRules = Array.Empty<string>() as IReadOnlyList<string>;

        _cacheServiceMock
            .Setup(x => x.GetAsync<int?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)0);

        // New IP - not in cache
        _cacheServiceMock
            .Setup(x => x.GetAsync<bool?>(
                It.Is<string>(k => k.StartsWith("known-ip:")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((bool?)null);

        // Act
        var result = await _engine.CalculateRiskScoreAsync(transaction, triggeredRules);

        // Assert
        var behavioralFactor = result.Factors.First(f => f.Name == "BehavioralPattern");
        Assert.Contains("New/unknown IP", behavioralFactor.Description);
    }

    [Fact]
    public async Task CalculateRiskScoreAsync_ScoreIsClampedBetweenZeroAndOne()
    {
        // Arrange - scenario that could produce very high raw score
        var transaction = CreateTransaction(
            amount: 100000m, country: "NG", ipAddress: null,
            channel: TransactionChannel.Phone);

        var manyRules = Enumerable.Range(1, 10)
            .Select(i => $"rule-{i}").ToList() as IReadOnlyList<string>;

        _cacheServiceMock
            .Setup(x => x.GetAsync<int?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(30); // Very high velocity

        // Act
        var result = await _engine.CalculateRiskScoreAsync(transaction, manyRules);

        // Assert
        Assert.InRange(result.Score, 0.0, 1.0);
    }

    [Fact]
    public async Task GetCustomerRiskProfileAsync_NotInCache_ReturnsNull()
    {
        // Arrange
        _cacheServiceMock
            .Setup(x => x.GetAsync<CustomerRiskProfile>(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerRiskProfile?)null);

        // Act
        var result = await _engine.GetCustomerRiskProfileAsync("CUST-UNKNOWN");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateCustomerRiskProfileAsync_NewCustomer_CreatesProfile()
    {
        // Arrange
        var score = RiskScore.Create(0.3, new List<RiskFactor>());

        _cacheServiceMock
            .Setup(x => x.GetAsync<CustomerRiskProfile>(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerRiskProfile?)null);

        // Act
        await _engine.UpdateCustomerRiskProfileAsync("CUST-NEW", score);

        // Assert
        _cacheServiceMock.Verify(
            x => x.SetAsync(
                It.Is<string>(k => k.Contains("CUST-NEW")),
                It.Is<CustomerRiskProfile>(p => p.CustomerId == "CUST-NEW" && p.TotalTransactions == 1),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Transaction CreateTransaction(
        decimal amount = 500m,
        string? country = "US",
        string? ipAddress = "192.168.1.1",
        TransactionChannel channel = TransactionChannel.Online) =>
        new(
            TransactionId: "TXN-TEST-001",
            MerchantId: "MERCH-001",
            CustomerId: "CUST-001",
            Amount: amount,
            Currency: "USD",
            Timestamp: DateTimeOffset.UtcNow,
            CardLast4: "4242",
            IpAddress: ipAddress,
            Country: country,
            Channel: channel);
}
