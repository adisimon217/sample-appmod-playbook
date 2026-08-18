using FraudWatch.Api.Controllers;
using FraudWatch.Core.Interfaces;
using FraudWatch.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FraudWatch.Tests.Controllers;

public class FraudCheckControllerTests
{
    private readonly Mock<IFraudDetectionService> _fraudDetectionServiceMock;
    private readonly Mock<IRiskScoringEngine> _riskScoringEngineMock;
    private readonly Mock<ILogger<FraudCheckController>> _loggerMock;
    private readonly FraudCheckController _controller;

    public FraudCheckControllerTests()
    {
        _fraudDetectionServiceMock = new Mock<IFraudDetectionService>();
        _riskScoringEngineMock = new Mock<IRiskScoringEngine>();
        _loggerMock = new Mock<ILogger<FraudCheckController>>();

        _controller = new FraudCheckController(
            _fraudDetectionServiceMock.Object,
            _riskScoringEngineMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task EvaluateTransaction_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new TransactionCheckRequest(
            TransactionId: "TXN-001",
            MerchantId: "MERCH-001",
            CustomerId: "CUST-001",
            Amount: 150.00m,
            Currency: "USD",
            CardLast4: "4242",
            IpAddress: "192.168.1.1",
            Country: "US",
            Channel: TransactionChannel.Online,
            Timestamp: DateTimeOffset.UtcNow);

        var expectedResult = new FraudCheckResult(
            TransactionId: "TXN-001",
            RiskScore: RiskScore.Create(0.15, new List<RiskFactor>()),
            Decision: FraudDecision.Approve,
            TriggeredRules: Array.Empty<string>(),
            AlertId: null,
            EvaluatedAt: DateTimeOffset.UtcNow);

        _fraudDetectionServiceMock
            .Setup(x => x.EvaluateTransactionAsync(
                It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.EvaluateTransaction(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var fraudCheckResult = Assert.IsType<FraudCheckResult>(okResult.Value);
        Assert.Equal(FraudDecision.Approve, fraudCheckResult.Decision);
        Assert.Equal("TXN-001", fraudCheckResult.TransactionId);
    }

    [Fact]
    public async Task EvaluateTransaction_ZeroAmount_ReturnsBadRequest()
    {
        // Arrange
        var request = new TransactionCheckRequest(
            TransactionId: "TXN-BAD",
            MerchantId: "MERCH-001",
            CustomerId: "CUST-001",
            Amount: 0m,
            Currency: "USD",
            CardLast4: "4242",
            IpAddress: "192.168.1.1",
            Country: "US",
            Channel: TransactionChannel.Online);

        // Act
        var result = await _controller.EvaluateTransaction(request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task EvaluateTransaction_NegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        var request = new TransactionCheckRequest(
            TransactionId: "TXN-NEG",
            MerchantId: "MERCH-001",
            CustomerId: "CUST-001",
            Amount: -100m,
            Currency: "USD",
            CardLast4: "4242",
            IpAddress: "192.168.1.1",
            Country: "US",
            Channel: TransactionChannel.Online);

        // Act
        var result = await _controller.EvaluateTransaction(request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task EvaluateTransaction_ServiceThrowsOperationCanceled_Returns503()
    {
        // Arrange
        var request = new TransactionCheckRequest(
            TransactionId: "TXN-TIMEOUT",
            MerchantId: "MERCH-001",
            CustomerId: "CUST-001",
            Amount: 100m,
            Currency: "USD",
            CardLast4: "4242",
            IpAddress: "192.168.1.1",
            Country: "US",
            Channel: TransactionChannel.Online);

        _fraudDetectionServiceMock
            .Setup(x => x.EvaluateTransactionAsync(
                It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _controller.EvaluateTransaction(request, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(503, statusResult.StatusCode);
    }

    [Fact]
    public async Task EvaluateBatch_EmptyTransactions_ReturnsBadRequest()
    {
        // Arrange
        var request = new BatchTransactionCheckRequest(
            Transactions: Array.Empty<TransactionCheckRequest>());

        // Act
        var result = await _controller.EvaluateBatch(request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task EvaluateBatch_ExceedsMaxBatchSize_ReturnsBadRequest()
    {
        // Arrange
        var transactions = Enumerable.Range(1, 101)
            .Select(i => new TransactionCheckRequest(
                $"TXN-{i}", "MERCH-001", "CUST-001", 100m, "USD",
                "4242", "10.0.0.1", "US", TransactionChannel.Online, null))
            .ToList();

        var request = new BatchTransactionCheckRequest(Transactions: transactions);

        // Act
        var result = await _controller.EvaluateBatch(request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetCustomerRiskProfile_ExistingCustomer_ReturnsProfile()
    {
        // Arrange
        var expectedProfile = new CustomerRiskProfile(
            CustomerId: "CUST-001",
            BaselineRiskScore: 0.25,
            CurrentRiskLevel: RiskLevel.Low,
            TotalTransactions: 150,
            FlaggedTransactions: 3,
            AverageTransactionAmount: 250.00m,
            KnownCountries: new[] { "US", "CA" },
            KnownDevices: new[] { "device-abc", "device-xyz" },
            LastActivity: DateTimeOffset.UtcNow.AddHours(-2),
            ProfileUpdatedAt: DateTimeOffset.UtcNow.AddHours(-1));

        _riskScoringEngineMock
            .Setup(x => x.GetCustomerRiskProfileAsync("CUST-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProfile);

        // Act
        var result = await _controller.GetCustomerRiskProfile("CUST-001", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var profile = Assert.IsType<CustomerRiskProfile>(okResult.Value);
        Assert.Equal("CUST-001", profile.CustomerId);
        Assert.Equal(150, profile.TotalTransactions);
    }

    [Fact]
    public async Task GetCustomerRiskProfile_UnknownCustomer_Returns404()
    {
        // Arrange
        _riskScoringEngineMock
            .Setup(x => x.GetCustomerRiskProfileAsync("CUST-UNKNOWN", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerRiskProfile?)null);

        // Act
        var result = await _controller.GetCustomerRiskProfile("CUST-UNKNOWN", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
