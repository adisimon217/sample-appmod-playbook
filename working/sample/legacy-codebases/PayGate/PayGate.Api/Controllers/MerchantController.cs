using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Web.Http;
using PayGate.Core.Models;
using PayGate.Core.Services;
using PayGate.Data.Repositories;

namespace PayGate.Api.Controllers
{
    /// <summary>
    /// Merchant management endpoints - API key authenticated.
    /// </summary>
    [RoutePrefix("api/merchants")]
    public class MerchantController : ApiController
    {
        private readonly MerchantRepository _merchantRepo;
        private readonly MerchantValidationService _validationService;

        public MerchantController()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["PaymentsDB"].ConnectionString;
            _merchantRepo = new MerchantRepository(connectionString);
            _validationService = new MerchantValidationService(connectionString);
        }

        /// <summary>
        /// Get merchant profile.
        /// </summary>
        [HttpGet]
        [Route("{merchantId}")]
        public async Task<IHttpActionResult> GetMerchant(string merchantId)
        {
            var merchant = await _merchantRepo.GetByIdAsync(merchantId);
            if (merchant == null)
                return NotFound();

            return Ok(merchant);
        }

        /// <summary>
        /// Update merchant configuration (rate limits, settlement schedule, etc.)
        /// </summary>
        [HttpPut]
        [Route("{merchantId}/configuration")]
        public async Task<IHttpActionResult> UpdateConfiguration(string merchantId, [FromBody] MerchantConfigUpdate config)
        {
            if (config == null)
                return BadRequest("Configuration update is required.");

            var merchant = await _merchantRepo.GetByIdAsync(merchantId);
            if (merchant == null)
                return NotFound();

            try
            {
                await _merchantRepo.UpdateConfigurationAsync(merchantId, config.MaxTransactionsPerHour,
                    config.SettlementSchedule, config.WebhookUrl, config.AllowedIpAddresses);
                return Ok(new { Message = "Configuration updated successfully." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.EventLog.WriteEntry("PayGate",
                    $"Error updating merchant config for {merchantId}: {ex.Message}",
                    System.Diagnostics.EventLogEntryType.Error);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Get merchant transaction volume statistics.
        /// </summary>
        [HttpGet]
        [Route("{merchantId}/volume")]
        public async Task<IHttpActionResult> GetTransactionVolume(
            string merchantId,
            [FromUri] DateTime? startDate = null,
            [FromUri] DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var volume = await _merchantRepo.GetTransactionVolumeAsync(merchantId, start, end);
            return Ok(volume);
        }

        /// <summary>
        /// Onboard a new merchant.
        /// </summary>
        [HttpPost]
        [Route("onboard")]
        public async Task<IHttpActionResult> OnboardMerchant([FromBody] MerchantOnboardRequest request)
        {
            if (request == null)
                return BadRequest("Merchant onboarding request is required.");

            if (string.IsNullOrWhiteSpace(request.BusinessName))
                return BadRequest("Business name is required.");

            if (string.IsNullOrWhiteSpace(request.TaxId))
                return BadRequest("Tax ID is required.");

            try
            {
                var merchant = new Merchant
                {
                    MerchantId = $"MER-{Guid.NewGuid():N}".Substring(0, 15).ToUpper(),
                    BusinessName = request.BusinessName,
                    TaxId = request.TaxId,
                    ContactEmail = request.ContactEmail,
                    ContactPhone = request.ContactPhone,
                    Status = MerchantStatus.PendingVerification,
                    CreatedDate = DateTime.UtcNow,
                    MccCode = request.MccCode,
                    SettlementSchedule = "T+1",
                    MaxTransactionsPerHour = 1000 // Default limit for new merchants
                };

                await _merchantRepo.CreateAsync(merchant);

                return Created($"api/merchants/{merchant.MerchantId}", new
                {
                    merchant.MerchantId,
                    merchant.Status,
                    Message = "Merchant created. Pending verification."
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.EventLog.WriteEntry("PayGate",
                    $"Merchant onboarding error: {ex.Message}",
                    System.Diagnostics.EventLogEntryType.Error);
                return InternalServerError();
            }
        }
    }

    public class MerchantConfigUpdate
    {
        public int? MaxTransactionsPerHour { get; set; }
        public string SettlementSchedule { get; set; }
        public string WebhookUrl { get; set; }
        public string[] AllowedIpAddresses { get; set; }
    }

    public class MerchantOnboardRequest
    {
        public string BusinessName { get; set; }
        public string TaxId { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string MccCode { get; set; }
    }
}
