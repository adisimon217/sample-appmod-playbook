using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using PayGate.Core.Models;
using PayGate.Core.Services;
using PayGate.Data.Repositories;

namespace PayGate.Api.Controllers
{
    /// <summary>
    /// Core transaction processing controller.
    /// Handles ~50,000 transactions/hour at peak capacity.
    /// </summary>
    [RoutePrefix("api/transactions")]
    public class TransactionController : ApiController
    {
        private readonly PaymentProcessingService _paymentService;
        private readonly TransactionRepository _transactionRepo;
        private readonly MerchantValidationService _merchantValidator;

        public TransactionController()
        {
            // Direct instantiation - no DI container (legacy pattern)
            var connectionString = ConfigurationManager.ConnectionStrings["PaymentsDB"].ConnectionString;
            _transactionRepo = new TransactionRepository(connectionString);
            _merchantValidator = new MerchantValidationService(connectionString);
            _paymentService = new PaymentProcessingService(_transactionRepo, _merchantValidator);
        }

        /// <summary>
        /// Process a real-time payment transaction.
        /// Target latency: &lt;200ms p99 at 50K/hr throughput.
        /// </summary>
        [HttpPost]
        [Route("process")]
        public async Task<IHttpActionResult> ProcessTransaction([FromBody] TransactionRequest request)
        {
            if (request == null)
                return BadRequest("Transaction request body is required.");

            if (string.IsNullOrWhiteSpace(request.MerchantId))
                return BadRequest("MerchantId is required.");

            if (request.Amount <= 0)
                return BadRequest("Amount must be greater than zero.");

            if (string.IsNullOrWhiteSpace(request.CardNumber) || request.CardNumber.Length < 13)
                return BadRequest("Valid card number is required.");

            // Validate merchant is active and not suspended
            var merchantValid = await _merchantValidator.ValidateMerchantAsync(request.MerchantId);
            if (!merchantValid.IsValid)
            {
                return Content(HttpStatusCode.Forbidden, new TransactionResult
                {
                    Success = false,
                    ErrorCode = "MERCHANT_INVALID",
                    ErrorMessage = merchantValid.Reason,
                    TransactionId = Guid.Empty,
                    Timestamp = DateTime.UtcNow
                });
            }

            try
            {
                var result = await _paymentService.ProcessPaymentAsync(new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    MerchantId = request.MerchantId,
                    Amount = request.Amount,
                    Currency = request.Currency ?? "USD",
                    CardNumber = request.CardNumber,
                    CardExpiry = request.CardExpiry,
                    Cvv = request.Cvv,
                    CardholderName = request.CardholderName,
                    TransactionType = TransactionType.Authorization,
                    Status = TransactionStatus.Pending,
                    CreatedDate = DateTime.UtcNow,
                    IpAddress = GetClientIpAddress(),
                    UserAgent = Request.Headers.UserAgent?.ToString()
                });

                if (result.Success)
                    return Ok(result);

                return Content(HttpStatusCode.PaymentRequired, result);
            }
            catch (TimeoutException)
            {
                return Content(HttpStatusCode.GatewayTimeout, new TransactionResult
                {
                    Success = false,
                    ErrorCode = "TIMEOUT",
                    ErrorMessage = "Transaction processing timed out. Please retry.",
                    TransactionId = Guid.Empty,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.EventLog.WriteEntry("PayGate",
                    $"Transaction processing error: {ex.Message}\n{ex.StackTrace}",
                    System.Diagnostics.EventLogEntryType.Error);

                return InternalServerError();
            }
        }

        /// <summary>
        /// Process a batch of transactions (used for recurring billing).
        /// </summary>
        [HttpPost]
        [Route("batch")]
        public async Task<IHttpActionResult> ProcessBatch([FromBody] BatchTransactionRequest request)
        {
            if (request?.Transactions == null || !request.Transactions.Any())
                return BadRequest("At least one transaction is required in batch.");

            int maxBatchSize = int.Parse(ConfigurationManager.AppSettings["PayGate:SettlementBatchSize"] ?? "5000");
            if (request.Transactions.Count > maxBatchSize)
                return BadRequest($"Batch size exceeds maximum of {maxBatchSize} transactions.");

            var results = new List<TransactionResult>();
            var failedCount = 0;

            // Process in parallel chunks of 50 for throughput
            var chunks = request.Transactions
                .Select((t, i) => new { Transaction = t, Index = i })
                .GroupBy(x => x.Index / 50)
                .Select(g => g.Select(x => x.Transaction).ToList());

            foreach (var chunk in chunks)
            {
                var tasks = chunk.Select(async txn =>
                {
                    try
                    {
                        return await _paymentService.ProcessPaymentAsync(new Transaction
                        {
                            TransactionId = Guid.NewGuid(),
                            MerchantId = request.MerchantId,
                            Amount = txn.Amount,
                            Currency = txn.Currency ?? "USD",
                            CardNumber = txn.CardNumber,
                            CardExpiry = txn.CardExpiry,
                            Cvv = txn.Cvv,
                            CardholderName = txn.CardholderName,
                            TransactionType = TransactionType.Authorization,
                            Status = TransactionStatus.Pending,
                            CreatedDate = DateTime.UtcNow,
                            BatchId = request.BatchId
                        });
                    }
                    catch (Exception ex)
                    {
                        return new TransactionResult
                        {
                            Success = false,
                            ErrorCode = "PROCESSING_ERROR",
                            ErrorMessage = ex.Message,
                            Timestamp = DateTime.UtcNow
                        };
                    }
                });

                var chunkResults = await Task.WhenAll(tasks);
                results.AddRange(chunkResults);
                failedCount += chunkResults.Count(r => !r.Success);
            }

            return Ok(new BatchTransactionResponse
            {
                BatchId = request.BatchId,
                TotalProcessed = results.Count,
                SuccessCount = results.Count - failedCount,
                FailedCount = failedCount,
                Results = results
            });
        }

        /// <summary>
        /// Process a refund for a previously authorized transaction.
        /// </summary>
        [HttpPost]
        [Route("refund")]
        public async Task<IHttpActionResult> ProcessRefund([FromBody] RefundRequest request)
        {
            if (request == null || request.OriginalTransactionId == Guid.Empty)
                return BadRequest("Original transaction ID is required.");

            if (request.Amount <= 0)
                return BadRequest("Refund amount must be greater than zero.");

            var originalTxn = await _transactionRepo.GetByIdAsync(request.OriginalTransactionId);
            if (originalTxn == null)
                return NotFound();

            if (originalTxn.Status != TransactionStatus.Captured &&
                originalTxn.Status != TransactionStatus.Settled)
            {
                return BadRequest("Can only refund captured or settled transactions.");
            }

            if (request.Amount > originalTxn.Amount)
                return BadRequest("Refund amount cannot exceed original transaction amount.");

            try
            {
                var result = await _paymentService.ProcessRefundAsync(
                    originalTxn, request.Amount, request.Reason);
                return Ok(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.EventLog.WriteEntry("PayGate",
                    $"Refund processing error for {request.OriginalTransactionId}: {ex.Message}",
                    System.Diagnostics.EventLogEntryType.Error);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Get transaction by ID.
        /// </summary>
        [HttpGet]
        [Route("{id:guid}")]
        public async Task<IHttpActionResult> GetTransaction(Guid id)
        {
            var transaction = await _transactionRepo.GetByIdAsync(id);
            if (transaction == null)
                return NotFound();

            return Ok(transaction);
        }

        /// <summary>
        /// Get transactions for a merchant within a date range.
        /// </summary>
        [HttpGet]
        [Route("merchant/{merchantId}")]
        public async Task<IHttpActionResult> GetMerchantTransactions(
            string merchantId,
            [FromUri] DateTime? startDate = null,
            [FromUri] DateTime? endDate = null,
            [FromUri] int page = 1,
            [FromUri] int pageSize = 100)
        {
            if (pageSize > 1000) pageSize = 1000;

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var transactions = await _transactionRepo.GetByMerchantAsync(merchantId, start, end, page, pageSize);
            return Ok(transactions);
        }

        private string GetClientIpAddress()
        {
            if (Request.Properties.ContainsKey("MS_HttpContext"))
            {
                var ctx = Request.Properties["MS_HttpContext"] as System.Web.HttpContextWrapper;
                return ctx?.Request?.UserHostAddress;
            }
            return "0.0.0.0";
        }
    }

    // Request/Response DTOs
    public class TransactionRequest
    {
        public string MerchantId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string CardNumber { get; set; }
        public string CardExpiry { get; set; }
        public string Cvv { get; set; }
        public string CardholderName { get; set; }
    }

    public class BatchTransactionRequest
    {
        public string MerchantId { get; set; }
        public Guid BatchId { get; set; }
        public List<TransactionRequest> Transactions { get; set; }
    }

    public class BatchTransactionResponse
    {
        public Guid BatchId { get; set; }
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<TransactionResult> Results { get; set; }
    }

    public class RefundRequest
    {
        public Guid OriginalTransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; }
    }
}
