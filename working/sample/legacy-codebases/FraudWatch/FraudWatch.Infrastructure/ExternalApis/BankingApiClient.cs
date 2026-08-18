using System.Net.Http.Json;
using System.Text.Json;
using FraudWatch.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FraudWatch.Infrastructure.ExternalApis;

/// <summary>
/// Typed HttpClient for the external Banking API.
/// Implements resilience patterns (retry, circuit breaker) via Polly integration.
/// </summary>
public class BankingApiClient : IBankingApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BankingApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public BankingApiClient(
        HttpClient httpClient,
        ILogger<BankingApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<BankingVerificationResult> VerifyTransactionAsync(
        string transactionId,
        string merchantId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Verifying transaction {TransactionId} with Banking API, merchant={MerchantId}, amount={Amount}",
            transactionId, merchantId, amount);

        try
        {
            var request = new
            {
                transactionId,
                merchantId,
                amount,
                timestamp = DateTimeOffset.UtcNow
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/api/v2/transactions/verify",
                request,
                JsonOptions,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BankingVerificationResponse>(
                JsonOptions, cancellationToken);

            if (result is null)
            {
                _logger.LogWarning("Banking API returned null response for transaction {TransactionId}",
                    transactionId);

                return new BankingVerificationResult(
                    IsVerified: false,
                    Status: "Unknown",
                    ReasonCode: "NULL_RESPONSE",
                    VerifiedAt: DateTimeOffset.UtcNow);
            }

            _logger.LogDebug(
                "Transaction {TransactionId} verification result: {Status}",
                transactionId, result.Status);

            return new BankingVerificationResult(
                IsVerified: result.Status == "VERIFIED",
                Status: result.Status,
                ReasonCode: result.ReasonCode,
                VerifiedAt: result.VerifiedAt ?? DateTimeOffset.UtcNow);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Banking API request failed for transaction {TransactionId}. Status: {StatusCode}",
                transactionId, ex.StatusCode);

            return new BankingVerificationResult(
                IsVerified: false,
                Status: "ERROR",
                ReasonCode: $"HTTP_{(int?)ex.StatusCode}",
                VerifiedAt: DateTimeOffset.UtcNow);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning(
                "Banking API request timed out for transaction {TransactionId}",
                transactionId);

            return new BankingVerificationResult(
                IsVerified: false,
                Status: "TIMEOUT",
                ReasonCode: "REQUEST_TIMEOUT",
                VerifiedAt: DateTimeOffset.UtcNow);
        }
    }

    /// <inheritdoc/>
    public async Task<AccountRiskInfo?> GetAccountRiskInfoAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching account risk info for customer {CustomerId}", customerId);

        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/v2/accounts/{customerId}/risk",
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("No account risk info found for customer {CustomerId}", customerId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AccountRiskApiResponse>(
                JsonOptions, cancellationToken);

            if (result is null)
            {
                return null;
            }

            return new AccountRiskInfo(
                CustomerId: customerId,
                AccountStatus: result.AccountStatus,
                AccountAgeMonths: result.AccountAgeMonths,
                HasRecentChargebacks: result.ChargebackCount90Days > 0,
                ChargebackCount90Days: result.ChargebackCount90Days,
                ExternalRiskScore: result.RiskScore);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex,
                "Failed to fetch account risk info for customer {CustomerId}. Status: {StatusCode}",
                customerId, ex.StatusCode);
            return null;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning(
                "Account risk info request timed out for customer {CustomerId}", customerId);
            return null;
        }
    }
}

/// <summary>
/// Internal DTO for Banking API verification response deserialization.
/// </summary>
internal record BankingVerificationResponse(
    string TransactionId,
    string Status,
    string? ReasonCode,
    DateTimeOffset? VerifiedAt);

/// <summary>
/// Internal DTO for Banking API account risk response deserialization.
/// </summary>
internal record AccountRiskApiResponse(
    string AccountId,
    string AccountStatus,
    int AccountAgeMonths,
    int ChargebackCount90Days,
    double RiskScore,
    DateTimeOffset LastUpdated);
