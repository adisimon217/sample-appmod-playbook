using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Web.Http;
using PayGate.Core.Services;
using PayGate.Data.Repositories;
using PayGate.Integration.Sftp;

namespace PayGate.Api.Controllers
{
    /// <summary>
    /// Manages settlement processing with Visa and Mastercard networks.
    /// Settlement files are exchanged via SFTP using PGP encryption.
    /// </summary>
    [RoutePrefix("api/settlement")]
    public class SettlementController : ApiController
    {
        private readonly SettlementService _settlementService;
        private readonly SettlementRepository _settlementRepo;
        private readonly VisaSettlementClient _visaClient;
        private readonly MastercardSettlementClient _mastercardClient;

        public SettlementController()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["PaymentsDB"].ConnectionString;
            _settlementRepo = new SettlementRepository(connectionString);
            _visaClient = new VisaSettlementClient();
            _mastercardClient = new MastercardSettlementClient();
            _settlementService = new SettlementService(_settlementRepo, _visaClient, _mastercardClient);
        }

        /// <summary>
        /// Trigger daily settlement batch processing.
        /// Generates settlement files, encrypts with PGP, and sends via SFTP.
        /// </summary>
        [HttpPost]
        [Route("process-daily")]
        public async Task<IHttpActionResult> ProcessDailySettlement([FromBody] DailySettlementRequest request)
        {
            if (request == null)
                return BadRequest("Settlement request is required.");

            var settlementDate = request.SettlementDate ?? DateTime.UtcNow.Date.AddDays(-1);

            try
            {
                var result = await _settlementService.ProcessDailySettlementAsync(settlementDate);

                return Ok(new
                {
                    SettlementDate = settlementDate,
                    VisaTransactionCount = result.VisaCount,
                    MastercardTransactionCount = result.MastercardCount,
                    TotalAmount = result.TotalSettlementAmount,
                    VisaFileGenerated = result.VisaFileGenerated,
                    MastercardFileGenerated = result.MastercardFileGenerated,
                    Status = result.Success ? "Completed" : "PartialFailure",
                    Errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.EventLog.WriteEntry("PayGate",
                    $"Settlement processing failed for {settlementDate:yyyy-MM-dd}: {ex.Message}\n{ex.StackTrace}",
                    System.Diagnostics.EventLogEntryType.Error);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Download and process incoming settlement response files from networks.
        /// </summary>
        [HttpPost]
        [Route("reconcile")]
        public async Task<IHttpActionResult> ReconcileSettlement([FromBody] ReconcileRequest request)
        {
            if (request == null)
                return BadRequest("Reconcile request is required.");

            try
            {
                var result = await _settlementService.ReconcileAsync(request.Network, request.SettlementDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.EventLog.WriteEntry("PayGate",
                    $"Settlement reconciliation failed: {ex.Message}",
                    System.Diagnostics.EventLogEntryType.Error);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Get settlement status for a specific date and network.
        /// </summary>
        [HttpGet]
        [Route("status/{settlementDate}")]
        public async Task<IHttpActionResult> GetSettlementStatus(DateTime settlementDate, [FromUri] string network = null)
        {
            var settlements = await _settlementRepo.GetByDateAsync(settlementDate, network);
            if (settlements == null)
                return NotFound();

            return Ok(settlements);
        }

        /// <summary>
        /// Get settlement batch details.
        /// </summary>
        [HttpGet]
        [Route("batch/{batchId:guid}")]
        public async Task<IHttpActionResult> GetBatchDetails(Guid batchId)
        {
            var batch = await _settlementRepo.GetBatchAsync(batchId);
            if (batch == null)
                return NotFound();

            return Ok(batch);
        }
    }

    public class DailySettlementRequest
    {
        public DateTime? SettlementDate { get; set; }
    }

    public class ReconcileRequest
    {
        public string Network { get; set; }
        public DateTime SettlementDate { get; set; }
    }
}
