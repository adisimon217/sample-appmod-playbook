using FraudWatch.Core.Interfaces;
using FraudWatch.Core.Models;
using FraudWatch.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FraudWatch.Tests.Services;

public class FraudDetectionServiceTests
{
    private readonly Mock<IRuleEvaluator> _ruleEvaluatorMock;
    private readonly Mock<IRiskScoringEngine> _riskScoringEngineMock;
    private readonly Mock<IBankingApiClient> _bankingApiClientMock;
    private readonly Mock<IFraudAlertRepository> _alertRepositoryMock;
    private readonly Mock<ILogger<FraudDetectionService>> _loggerMock;
    private readonly FraudDetectionService _service;

    public FraudDetectionServiceTests()
    {
        _ruleEvaluatorMock = new Mock<IRuleEvaluator>();
        _riskScoringEngineMock = new Mock<IRiskScoringEngine>();
        _bankingApiClientMock = new Mock<IBankingApiClient>();
        _alertRepositoryMock = new Mock<IFraudAlertRepository>();
        _loggerMock = new Mock<ILogger<FraudDetectionService>>();

        _service = new FraudDetectionService(
            _ruleEvaluatorMock.Object,
            _riskScoringEngineMock.Object,
            _bankingApiClientMock.Object,
            _alertRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task EvaluateTransactionAsync_LowRisk_ReturnsApproveDecision()
    {
        // Arrange
        var transaction = CreateTestTransaction(amount: 50.00m);
        var triggeredRules = Array.Empty<string>() as IReadOnlyList<string>;
        var lowRiskScore = RiskScore.Create(0.10, new List<RiskFactor>
        {
            new("TransactionAmount", "Low amount", 0.2, 0.01)
        });

        _ruleEvaluatorMock
            .Setup(x => x.EvaluateAsync(transaction, It.IsAny<CancellationToken>()))
            .ReturnsAsync(triggeredRules);

        _riskScoringEngineMock
            .Setup(x => x.CalculateRiskScoreAsync(
                transaction, triggeredRules, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lowRiskScore);

        // Act
        var result = await _service.EvaluateTransactionAsync(transaction);

        // Assert
        Assert.Equal(FraudDecision.Approve, result.Decision);
        Assert.Equal(transaction.TransactionId, result.TransactionId);
        Assert.Null(result.AlertId);
    }

    [Fact]
    public async Task EvaluateTransactionAsync_HighRisk_ReturnsDeclineAndGeneratesAlert()
    {
        // Arrange
        var transaction = CreateTestTransaction(amount: 25000.00m, country: "NG");
        var triggeredRules = new List<string> { "rule-001", "rule-002" } as IReadOnlyList<string>;
        var highRiskScore = RiskScore.Create(0.90, new List<RiskFactor>
        {
            new("TransactionAmount", "High amount: $25,000", 0.2, 0.16),
            new("GeographicRisk", "High-risk country: NG", 0.15, 0.105),
            new("RuleTriggered", "2 rules triggered", 0.2, 0.08)
        });

        _ruleEvaluatorMock
            .Setup(x => x.EvaluateAsync(transaction, It.IsAny<CancellationToken>()))
            .ReturnsAsync(triggeredRules);

        _riskScoringEngineMock
            .Setup(x => x.CalculateRiskScoreAsync(
                transaction, triggeredRules, It.IsAny<CancellationToken>()))
            .ReturnsAsync(highRiskScore);

        _bankingApiClientMock
            .Setup(x => x.GetAccountRiskInfoAsync(
                transaction.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountRiskInfo?)null);

        // Act
        var result = await _service.EvaluateTransactionAsync(transaction);

        // Assert
        Assert.Equal(FraudDecision.Decline, result.Decision);
        Assert.NotNull(result.AlertId);
        Assert.Equal(2, result.TriggeredRules.Count);

        _alertRepositoryMock.Verify(
            x => x.CreateAsync(It.IsAny<FraudAlert>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateTransactionAsync_MediumRisk_ReturnsReviewDecision()
    {
        // Arrange
        var transaction = CreateTestTransaction(amount: 8000.00m);
        var triggeredRules = new List<string> { "rule-001" } as IReadOnlyList<string>;
        var mediumRiskScore = RiskScore.Create(0.65, new List<RiskFactor>
        {
            new("TransactionAmount", "Moderate amount", 0.2, 0.12),
            new("RuleTriggered", "1 rule triggered", 0.2, 0.04)
        });

        _ruleEvaluatorMock
            .Setup(x => x.EvaluateAsync(transaction, It.IsAny<CancellationToken>()))
            .ReturnsAsync(triggeredRules);

        _riskScoringEngineMock
            .Setup(x => x.CalculateRiskScoreAsync(
                transaction, triggeredRules, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediumRiskScore);

        _bankingApiClientMock
            .Setup(x => x.GetAccountRiskInfoAsync(
                transaction.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountRiskInfo?)null);

        // Act
        var result = await _service.EvaluateTransactionAsync(transaction);

        // Assert
        Assert.Equal(FraudDecision.Review, result.Decision);
        Assert.NotNull(result.AlertId);
    }

    [Fact]
    public async Task EvaluateTransactionAsync_UpdatesCustomerRiskProfile()
    {
        // Arrange
        var transaction = CreateTestTransaction(amount: 100.00m);
        var triggeredRules = Array.Empty<string>() as IReadOnlyList<string>;
        var riskScore = RiskScore.Create(0.15, new List<RiskFactor>());

        _ruleEvaluatorMock
            .Setup(x => x.EvaluateAsync(transaction, It.IsAny<CancellationToken>()))
            .ReturnsAsync(triggeredRules);

        _riskScoringEngineMock
            .Setup(x => x.CalculateRiskScoreAsync(
                transaction, triggeredRules, It.IsAny<CancellationToken>()))
            .ReturnsAsync(riskScore);

        // Act
        await _service.EvaluateTransactionAsync(transaction);

        // Assert
        _riskScoringEngineMock.Verify(
            x => x.UpdateCustomerRiskProfileAsync(
                transaction.CustomerId,
                riskScore,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateBatchAsync_ProcessesAllTransactions()
    {
        // Arrange
        var transactions = Enumerable.Range(1, 5)
            .Select(i => CreateTestTransaction(
                transactionId: $"TXN-{i:D4}",
                amount: 100.00m * i))
            .ToList();

        var triggeredRules = Array.Empty<string>() as IReadOnlyList<string>;
        var riskScore = RiskScore.Create(0.10, new List<RiskFactor>());

        _ruleEvaluatorMock
            .Setup(x => x.EvaluateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(triggeredRules);

        _riskScoringEngineMock
            .Setup(x => x.CalculateRiskScoreAsync(
                It.IsAny<Transaction>(), triggeredRules, It.IsAny<CancellationToken>()))
            .ReturnsAsync(riskScore);

        // Act
        var results = new List<FraudCheckResult>();
        await foreach (var result in _service.EvaluateBatchAsync(transactions))
        {
            results.Add(result);
        }

        // Assert
        Assert.Equal(5, results.Count);
    }

    private static Transaction CreateTestTransaction(
        string transactionId = "TXN-TEST-001",
        string merchantId = "MERCH-001",
        string customerId = "CUST-001",
        decimal amount = 100.00m,
        string currency = "USD",
        string? country = "US",
        TransactionChannel channel = TransactionChannel.Online) =>
        new(
            TransactionId: transactionId,
            MerchantId: merchantId,
            CustomerId: customerId,
            Amount: amount,
            Currency: currency,
            Timestamp: DateTimeOffset.UtcNow,
            CardLast4: "4242",
            IpAddress: "192.168.1.100",
            Country: country,
            Channel: channel);
}
