using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using PayGate.Core.Models;
using PayGate.Data.Repositories;

namespace PayGate.Core.Services
{
    /// <summary>
    /// Core payment processing engine. Handles authorization, capture, refunds.
    /// Processes ~50,000 transactions/hour at peak with p99 latency target of 200ms.
    /// </summary>
    public class PaymentProcessingService
    {
        private readonly TransactionRepository _transactionRepo;
        private readonly MerchantValidationService _merchantValidator;
        private readonly TransactionRoutingService _routingService;

        private static readonly int TimeoutSeconds =
            int.Parse(ConfigurationManager.AppSettings["PayGate:TransactionTimeoutSeconds"] ?? "30");

        public PaymentProcessingService(
            TransactionRepository transactionRepo,
            MerchantValidationService merchantValidator)
        {
            _transactionRepo = transactionRepo;
            _merchantValidator = merchantValidator;
            _routingService = new TransactionRoutingService();
        }

        /// <summary>
        /// Process a payment transaction through the authorization pipeline.
        /// </summary>
        public async Task<TransactionResult> ProcessPaymentAsync(Transaction transaction)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                // Step 1: Basic validation
                var validationError = ValidateTransaction(transaction);
                if (validationError != null)
                {
                    return new TransactionResult
                    {
                        Success = false,
                        TransactionId = transaction.TransactionId,
                        ErrorCode = "VALIDATION_ERROR",
                        ErrorMessage = validationError,
                        Timestamp = DateTime.UtcNow
                    };
                }

                // Step 2: Determine card brand and routing
                transaction.CardBrand = DetermineCardBrand(transaction.CardNumber);
                transaction.CardBin = transaction.CardNumber.Substring(0, 6);
                transaction.CardLast4 = transaction.CardNumber.Substring(transaction.CardNumber.Length - 4);

                // Step 3: Fraud checks (basic velocity check)
                var fraudCheck = await CheckVelocityAsync(transaction);
                if (!fraudCheck)
                {
                    transaction.Status = TransactionStatus.Declined;
                    transaction.ResponseCode = "FRAUD_SUSPECT";
                    transaction.ResponseMessage = "Transaction declined by fraud detection.";
                    await _transactionRepo.InsertAsync(transaction);

                    return new TransactionResult
                    {
                        Success = false,
                        TransactionId = transaction.TransactionId,
                        ErrorCode = "FRAUD_DECLINED",
                        ErrorMessage = "Transaction declined.",
                        Timestamp = DateTime.UtcNow
                    };
                }

                // Step 4: Calculate processing fee
                transaction.ProcessingFee = CalculateProcessingFee(transaction.Amount, transaction.CardBrand);

                // Step 5: Simulate authorization (in production, this calls card network)
                var authCode = GenerateAuthorizationCode();
                transaction.AuthorizationCode = authCode;
                transaction.Status = TransactionStatus.Authorized;
                transaction.ResponseCode = "00"; // Approved
                transaction.ResponseMessage = "Approved";
                transaction.ProcessedDate = DateTime.UtcNow;

                sw.Stop();
                transaction.LatencyMs = (int)sw.ElapsedMilliseconds;

                // Step 6: Persist transaction (uses bulk-optimized path)
                await _transactionRepo.InsertAsync(transaction);

                return new TransactionResult
                {
                    Success = true,
                    TransactionId = transaction.TransactionId,
                    AuthorizationCode = authCode,
                    ResponseCode = "00",
                    Timestamp = DateTime.UtcNow,
                    ProcessingFee = transaction.ProcessingFee,
                    LatencyMs = transaction.LatencyMs
                };
            }
            catch (TimeoutException)
            {
                sw.Stop();
                transaction.Status = TransactionStatus.Failed;
                transaction.ResponseCode = "TIMEOUT";
                transaction.LatencyMs = (int)sw.ElapsedMilliseconds;

                // Still persist failed transaction for audit
                try { await _transactionRepo.InsertAsync(transaction); } catch { /* best effort */ }

                throw; // Re-throw for controller to handle
            }
            catch (Exception ex)
            {
                sw.Stop();
                transaction.Status = TransactionStatus.Failed;
                transaction.ResponseCode = "ERROR";
                transaction.ResponseMessage = ex.Message;
                transaction.LatencyMs = (int)sw.ElapsedMilliseconds;

                try { await _transactionRepo.InsertAsync(transaction); } catch { /* best effort */ }

                return new TransactionResult
                {
                    Success = false,
                    TransactionId = transaction.TransactionId,
                    ErrorCode = "PROCESSING_ERROR",
                    ErrorMessage = "An error occurred processing the transaction.",
                    Timestamp = DateTime.UtcNow,
                    LatencyMs = (int)sw.ElapsedMilliseconds
                };
            }
        }

        /// <summary>
        /// Process a refund against a previously authorized/captured transaction.
        /// </summary>
        public async Task<TransactionResult> ProcessRefundAsync(Transaction originalTransaction, decimal refundAmount, string reason)
        {
            var refundTransaction = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                MerchantId = originalTransaction.MerchantId,
                Amount = -refundAmount, // Negative amount for refund
                Currency = originalTransaction.Currency,
                CardNumber = originalTransaction.CardNumber,
                CardBrand = originalTransaction.CardBrand,
                CardBin = originalTransaction.CardBin,
                CardLast4 = originalTransaction.CardLast4,
                TransactionType = TransactionType.Refund,
                Status = TransactionStatus.Authorized,
                CreatedDate = DateTime.UtcNow,
                ProcessedDate = DateTime.UtcNow,
                OriginalTransactionId = originalTransaction.TransactionId,
                AuthorizationCode = GenerateAuthorizationCode(),
                ResponseCode = "00",
                ResponseMessage = $"Refund processed. Reason: {reason}"
            };

            await _transactionRepo.InsertAsync(refundTransaction);

            // Update original transaction status
            await _transactionRepo.UpdateStatusAsync(
                originalTransaction.TransactionId, TransactionStatus.Refunded);

            return new TransactionResult
            {
                Success = true,
                TransactionId = refundTransaction.TransactionId,
                AuthorizationCode = refundTransaction.AuthorizationCode,
                ResponseCode = "00",
                Timestamp = DateTime.UtcNow
            };
        }

        private string ValidateTransaction(Transaction transaction)
        {
            if (transaction.Amount <= 0)
                return "Amount must be greater than zero.";

            if (transaction.Amount > 999999.99m)
                return "Amount exceeds maximum allowed ($999,999.99).";

            if (string.IsNullOrWhiteSpace(transaction.CardNumber))
                return "Card number is required.";

            if (!LuhnCheck(transaction.CardNumber))
                return "Invalid card number.";

            if (!string.IsNullOrEmpty(transaction.CardExpiry))
            {
                if (!IsCardNotExpired(transaction.CardExpiry))
                    return "Card is expired.";
            }

            return null;
        }

        private bool LuhnCheck(string cardNumber)
        {
            // Luhn algorithm for card number validation
            var sum = 0;
            var alternate = false;

            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(cardNumber[i])) continue;

                var digit = cardNumber[i] - '0';
                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9) digit -= 9;
                }
                sum += digit;
                alternate = !alternate;
            }

            return sum % 10 == 0;
        }

        private bool IsCardNotExpired(string expiry)
        {
            // Format: MM/YY or MM/YYYY
            var parts = expiry.Split('/');
            if (parts.Length != 2) return false;

            if (!int.TryParse(parts[0], out int month)) return false;
            if (!int.TryParse(parts[1], out int year)) return false;

            if (year < 100) year += 2000;

            var expiryDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            return expiryDate >= DateTime.UtcNow.Date;
        }

        private string DetermineCardBrand(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber)) return "Unknown";

            if (cardNumber.StartsWith("4")) return "Visa";
            if (cardNumber.StartsWith("5") || cardNumber.StartsWith("2")) return "Mastercard";
            if (cardNumber.StartsWith("34") || cardNumber.StartsWith("37")) return "Amex";
            if (cardNumber.StartsWith("6")) return "Discover";

            return "Unknown";
        }

        private decimal CalculateProcessingFee(decimal amount, string cardBrand)
        {
            // Fee structure: percentage + fixed fee per transaction
            decimal percentFee;
            decimal fixedFee = 0.30m;

            switch (cardBrand)
            {
                case "Visa":
                    percentFee = 0.0195m; // 1.95%
                    break;
                case "Mastercard":
                    percentFee = 0.0200m; // 2.00%
                    break;
                case "Amex":
                    percentFee = 0.0295m; // 2.95%
                    fixedFee = 0.35m;
                    break;
                default:
                    percentFee = 0.0250m; // 2.50%
                    break;
            }

            return Math.Round(amount * percentFee + fixedFee, 2);
        }

        private async Task<bool> CheckVelocityAsync(Transaction transaction)
        {
            // Check how many transactions this card has done in the last hour
            var recentCount = await _transactionRepo.GetCardVelocityAsync(
                transaction.CardBin + "XXXXXX" + transaction.CardLast4,
                TimeSpan.FromHours(1));

            // Decline if more than 10 transactions per card per hour
            if (recentCount > 10) return false;

            // Check for duplicate amount from same merchant in last 5 minutes
            var isDuplicate = await _transactionRepo.IsDuplicateTransactionAsync(
                transaction.MerchantId,
                transaction.Amount,
                transaction.CardLast4,
                TimeSpan.FromMinutes(5));

            return !isDuplicate;
        }

        private string GenerateAuthorizationCode()
        {
            // Generate a 6-character alphanumeric authorization code
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var code = new char[6];
            for (int i = 0; i < 6; i++)
            {
                code[i] = chars[random.Next(chars.Length)];
            }
            return new string(code);
        }
    }
}
