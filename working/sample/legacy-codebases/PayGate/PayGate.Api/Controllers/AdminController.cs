using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Web.Http;
using PayGate.Data.Repositories;
using PayGate.Security;

namespace PayGate.Api.Controllers
{
    /// <summary>
    /// Administrative endpoints - requires Windows Authentication.
    /// Only accessible by PAYGATE\PayGate-Admins domain group.
    /// </summary>
    [Authorize(Roles = "PAYGATE\\PayGate-Admins")]
    [RoutePrefix("api/admin")]
    public class AdminController : ApiController
    {
        private readonly TransactionRepository _transactionRepo;
        private readonly MerchantRepository _merchantRepo;
        private readonly SettlementRepository _settlementRepo;

        public AdminController()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["PaymentsDB"].ConnectionString;
            _transactionRepo = new TransactionRepository(connectionString);
            _merchantRepo = new MerchantRepository(connectionString);
            _settlementRepo = new SettlementRepository(connectionString);
        }

        /// <summary>
        /// Get system health metrics (transaction volume, error rates, latency percentiles).
        /// </summary>
        [HttpGet]
        [Route("metrics")]
        public async Task<IHttpActionResult> GetSystemMetrics([FromUri] DateTime? date = null)
        {
            var targetDate = date ?? DateTime.UtcNow.Date;

            var peakMetrics = await _transactionRepo.GetPeakHourMetricsAsync(targetDate);
            var failedTxn = await _transactionRepo.GetFailedTransactionCountAsync(targetDate);

            return Ok(new
            {
                Date = targetDate,
                PeakTransactionsPerHour = peakMetrics.PeakVolume,
                PeakHour = peakMetrics.PeakHour,
                TotalTransactions = peakMetrics.TotalVolume,
                FailedTransactions = failedTxn,
                AverageLatencyMs = peakMetrics.AvgLatencyMs,
                P99LatencyMs = peakMetrics.P99LatencyMs,
                SuccessRate = peakMetrics.TotalVolume > 0
                    ? (1.0 - (double)failedTxn / peakMetrics.TotalVolume) * 100
                    : 0
            });
        }

        /// <summary>
        /// Suspend a merchant (fraud concern, compliance issue, etc.)
        /// </summary>
        [HttpPost]
        [Route("merchants/{merchantId}/suspend")]
        public async Task<IHttpActionResult> SuspendMerchant(string merchantId, [FromBody] SuspensionRequest request)
        {
            var currentUser = WindowsAuthHelper.GetCurrentWindowsUser(RequestContext);
            if (string.IsNullOrEmpty(currentUser))
                return Unauthorized();

            try
            {
                await _merchantRepo.SuspendAsync(merchantId, request?.Reason, currentUser);

                System.Diagnostics.EventLog.WriteEntry("PayGate",
                    $"Merchant {merchantId} suspended by {currentUser}. Reason: {request?.Reason}",
                    System.Diagnostics.EventLogEntryType.Warning);

                return Ok(new { Message = $"Merchant {merchantId} has been suspended.", SuspendedBy = currentUser });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Get daily revenue report.
        /// </summary>
        [HttpGet]
        [Route("revenue")]
        public async Task<IHttpActionResult> GetRevenueReport(
            [FromUri] DateTime startDate,
            [FromUri] DateTime endDate)
        {
            if (endDate < startDate)
                return BadRequest("End date must be after start date.");

            var report = await _transactionRepo.GetRevenueReportAsync(startDate, endDate);
            return Ok(report);
        }

        /// <summary>
        /// Trigger archival of old transactions (retention policy: 7 years).
        /// </summary>
        [HttpPost]
        [Route("archive")]
        public async Task<IHttpActionResult> ArchiveTransactions([FromBody] ArchiveRequest request)
        {
            var currentUser = WindowsAuthHelper.GetCurrentWindowsUser(RequestContext);
            var cutoffDate = request?.CutoffDate ?? DateTime.UtcNow.AddYears(-7);

            try
            {
                var archivedCount = await _transactionRepo.ArchiveTransactionsAsync(cutoffDate);

                System.Diagnostics.EventLog.WriteEntry("PayGate",
                    $"Archive initiated by {currentUser}: {archivedCount} transactions archived (before {cutoffDate:yyyy-MM-dd})",
                    System.Diagnostics.EventLogEntryType.Information);

                return Ok(new { ArchivedCount = archivedCount, CutoffDate = cutoffDate, InitiatedBy = currentUser });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Get chargeback report for a date range.
        /// </summary>
        [HttpGet]
        [Route("chargebacks")]
        public async Task<IHttpActionResult> GetChargebackReport(
            [FromUri] DateTime startDate,
            [FromUri] DateTime endDate)
        {
            var report = await _settlementRepo.GetChargebackReportAsync(startDate, endDate);
            return Ok(report);
        }
    }

    public class SuspensionRequest
    {
        public string Reason { get; set; }
    }

    public class ArchiveRequest
    {
        public DateTime? CutoffDate { get; set; }
    }
}
