using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Polly;
using Polly.Retry;
using Serilog;
using ReconcEngine.Models;
using ReconcEngine.Models.Enums;

namespace ReconcEngine.RetryHandler
{
    /// <summary>
    /// Windows Service that polls for failed reconciliation matches every 30 minutes
    /// and retries them using Polly exponential backoff (max 3 attempts).
    /// </summary>
    public class RetryHandlerService : ServiceBase
    {
        private Timer _pollTimer;
        private readonly ILogger _logger;
        private readonly string _reconcDbConnectionString;
        private readonly string _payGateDbConnectionString;
        private readonly int _retryIntervalMinutes;
        private readonly int _maxRetryAttempts;
        private readonly int _retryBaseDelaySeconds;
        private readonly AsyncRetryPolicy _retryPolicy;
        private bool _isProcessing;
        private readonly object _lockObject = new object();

        public RetryHandlerService()
        {
            ServiceName = "ReconcEngine.RetryHandler";
            CanStop = true;
            CanPauseAndContinue = true;
            AutoLog = true;

            _logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .WriteTo.File(
                    ConfigurationManager.AppSettings["Serilog:WriteTo:File:Path"],
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.EventLog(
                    ConfigurationManager.AppSettings["Serilog:WriteTo:EventLog:Source"],
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning)
                .Enrich.WithProperty("Application", "ReconcEngine.RetryHandler")
                .CreateLogger();

            _reconcDbConnectionString = ConfigurationManager.ConnectionStrings["ReconcDB"].ConnectionString;
            _payGateDbConnectionString = ConfigurationManager.ConnectionStrings["PayGateDB"].ConnectionString;
            _retryIntervalMinutes = int.Parse(ConfigurationManager.AppSettings["RetryIntervalMinutes"]);
            _maxRetryAttempts = int.Parse(ConfigurationManager.AppSettings["MaxRetryAttempts"]);
            _retryBaseDelaySeconds = int.Parse(ConfigurationManager.AppSettings["RetryBaseDelaySeconds"]);

            // Configure Polly retry policy with exponential backoff
            _retryPolicy = Policy
                .Handle<SqlException>()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    retryCount: _maxRetryAttempts,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(
                        _retryBaseDelaySeconds * Math.Pow(2, attempt - 1)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.Warning(exception,
                            "Retry attempt {RetryCount}/{MaxRetries} for {OperationKey}. " +
                            "Waiting {DelaySeconds}s before next attempt.",
                            retryCount, _maxRetryAttempts,
                            context.OperationKey, timeSpan.TotalSeconds);
                    });
        }

        protected override void OnStart(string[] args)
        {
            _logger.Information("RetryHandler service starting. " +
                "Poll interval: {IntervalMinutes} min, Max retries: {MaxRetries}, " +
                "Base delay: {BaseDelay}s (exponential backoff)",
                _retryIntervalMinutes, _maxRetryAttempts, _retryBaseDelaySeconds);

            _pollTimer = new Timer(
                async _ => await ProcessPendingRetriesAsync(),
                null,
                TimeSpan.FromSeconds(30), // Initial delay
                TimeSpan.FromMinutes(_retryIntervalMinutes));

            _logger.Information("RetryHandler service started");
        }

        protected override void OnStop()
        {
            _logger.Information("RetryHandler service stopping");
            _pollTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _pollTimer?.Dispose();
            _logger.Information("RetryHandler service stopped");
        }

        protected override void OnPause()
        {
            _logger.Information("RetryHandler service paused");
            _pollTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        protected override void OnContinue()
        {
            _logger.Information("RetryHandler service resuming");
            _pollTimer?.Change(TimeSpan.Zero, TimeSpan.FromMinutes(_retryIntervalMinutes));
        }

        private async Task ProcessPendingRetriesAsync()
        {
            lock (_lockObject)
            {
                if (_isProcessing)
                {
                    _logger.Debug("Previous retry cycle still in progress. Skipping.");
                    return;
                }
                _isProcessing = true;
            }

            try
            {
                _logger.Information("Starting retry cycle");

                var pendingRetries = await GetPendingRetriesAsync();

                if (!pendingRetries.Any())
                {
                    _logger.Debug("No pending retries found");
                    return;
                }

                _logger.Information("Processing {Count} pending retries", pendingRetries.Count);

                int successCount = 0;
                int failedCount = 0;

                foreach (var item in pendingRetries)
                {
                    try
                    {
                        var context = new Context($"Retry-{item.ReconcResultId}");

                        await _retryPolicy.ExecuteAsync(async (ctx) =>
                        {
                            await RetryMatchAsync(item);
                        }, context);

                        await UpdateRetryStatusAsync(item.ReconcResultId, MatchStatus.Matched, item.RetryCount + 1);
                        successCount++;

                        _logger.Information("Retry succeeded for {ReconcResultId} on attempt {Attempt}",
                            item.ReconcResultId, item.RetryCount + 1);
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        var newRetryCount = item.RetryCount + 1;

                        if (newRetryCount >= _maxRetryAttempts)
                        {
                            _logger.Error(ex, "Max retries exhausted for {ReconcResultId}. Marking as permanently failed.",
                                item.ReconcResultId);
                            await UpdateRetryStatusAsync(item.ReconcResultId, MatchStatus.PermanentlyFailed, newRetryCount);
                        }
                        else
                        {
                            _logger.Warning(ex, "Retry failed for {ReconcResultId}. Attempt {Attempt}/{Max}",
                                item.ReconcResultId, newRetryCount, _maxRetryAttempts);
                            await UpdateRetryStatusAsync(item.ReconcResultId, MatchStatus.PendingRetry, newRetryCount);
                        }
                    }
                }

                _logger.Information("Retry cycle completed. Success: {SuccessCount}, Failed: {FailedCount}",
                    successCount, failedCount);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Retry cycle failed unexpectedly");
            }
            finally
            {
                lock (_lockObject)
                {
                    _isProcessing = false;
                }
            }
        }

        private async Task<List<RetryQueueItem>> GetPendingRetriesAsync()
        {
            using (var connection = new SqlConnection(_reconcDbConnectionString))
            {
                await connection.OpenAsync();

                var items = await connection.QueryAsync<RetryQueueItem>(
                    "EXEC sp_GetPendingRetries @MaxRetryAttempts",
                    new { MaxRetryAttempts = _maxRetryAttempts });

                return items.ToList();
            }
        }

        private async Task RetryMatchAsync(RetryQueueItem item)
        {
            using (var reconcConn = new SqlConnection(_reconcDbConnectionString))
            using (var payGateConn = new SqlConnection(_payGateDbConnectionString))
            {
                await reconcConn.OpenAsync();
                await payGateConn.OpenAsync();

                // Fetch the latest transaction data from PayGateDB
                var transaction = await payGateConn.QuerySingleOrDefaultAsync<Transaction>(
                    @"SELECT TransactionId, MerchantId, Amount, Currency, TransactionDate, 
                             ReferenceNumber, Status, ProcessorResponse
                      FROM Transactions 
                      WHERE TransactionId = @TransactionId",
                    new { item.TransactionId });

                if (transaction == null)
                {
                    throw new InvalidOperationException(
                        $"Transaction {item.TransactionId} not found in PayGateDB.");
                }

                // Re-attempt the match
                var matchResult = await reconcConn.QuerySingleOrDefaultAsync<MatchResult>(
                    "EXEC sp_InsertReconcResult @TransactionId, @SettlementRecordId, @MatchStatus, @MatchedAmount, @DiscrepancyAmount",
                    new
                    {
                        item.TransactionId,
                        item.SettlementRecordId,
                        MatchStatus = MatchStatus.Matched.ToString(),
                        MatchedAmount = transaction.Amount,
                        DiscrepancyAmount = 0m
                    });
            }
        }

        private async Task UpdateRetryStatusAsync(Guid reconcResultId, MatchStatus status, int retryCount)
        {
            using (var connection = new SqlConnection(_reconcDbConnectionString))
            {
                await connection.OpenAsync();

                await connection.ExecuteAsync(
                    "EXEC sp_UpdateRetryCount @ReconcResultId, @Status, @RetryCount, @LastRetryTime",
                    new
                    {
                        ReconcResultId = reconcResultId,
                        Status = status.ToString(),
                        RetryCount = retryCount,
                        LastRetryTime = DateTime.UtcNow
                    });
            }
        }

        /// <summary>
        /// Internal model for items in the retry queue.
        /// </summary>
        private class RetryQueueItem
        {
            public Guid ReconcResultId { get; set; }
            public Guid TransactionId { get; set; }
            public Guid SettlementRecordId { get; set; }
            public int RetryCount { get; set; }
            public DateTime LastAttemptTime { get; set; }
            public string FailureReason { get; set; }
        }
    }
}
