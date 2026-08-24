namespace FraudWatch.Core.Models;

/// <summary>
/// Represents a payment transaction to be evaluated for fraud.
/// </summary>
public record Transaction(
    string TransactionId,
    string MerchantId,
    string CustomerId,
    decimal Amount,
    string Currency,
    DateTimeOffset Timestamp,
    string? CardLast4,
    string? IpAddress,
    string? Country,
    TransactionChannel Channel);

/// <summary>
/// Channel through which the transaction was initiated.
/// </summary>
public enum TransactionChannel
{
    Online,
    InStore,
    Mobile,
    Phone,
    Api
}
