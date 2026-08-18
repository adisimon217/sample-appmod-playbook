using System;
using System.Web.Http;
using log4net;
using MerchantHub.Core;
using MerchantHub.Core.Services;
using MerchantHub.Data.Repositories;

namespace MerchantHub.Api.Controllers
{
    [RoutePrefix("api/merchant")]
    public class MerchantApiController : ApiController
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MerchantApiController));

        // GET api/merchant/profile
        [HttpGet]
        [Route("profile")]
        public IHttpActionResult GetProfile()
        {
            try
            {
                var merchantId = GetMerchantIdFromApiKey();
                if (merchantId == 0) return Unauthorized();

                var repo = ServiceLocator.Resolve<IMerchantRepository>();
                var merchant = repo.GetById(merchantId);

                if (merchant == null) return NotFound();

                return Ok(new
                {
                    merchant.MerchantId,
                    merchant.BusinessName,
                    merchant.DBA,
                    merchant.MerchantNumber,
                    merchant.Status,
                    merchant.Tier,
                    merchant.Email,
                    merchant.Phone,
                    merchant.MonthlyVolumeLimit,
                    merchant.SingleTransactionLimit,
                    merchant.ProcessingFeeRate
                });
            }
            catch (Exception ex)
            {
                _log.Error("API Error - GetProfile", ex);
                return InternalServerError();
            }
        }

        // GET api/merchant/stats
        [HttpGet]
        [Route("stats")]
        public IHttpActionResult GetStats(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var merchantId = GetMerchantIdFromApiKey();
                if (merchantId == 0) return Unauthorized();

                var start = startDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                var end = endDate ?? DateTime.Today.AddDays(1);

                var transactionRepo = ServiceLocator.Resolve<ITransactionRepository>();
                var transactions = transactionRepo.GetByMerchantAndDateRange(merchantId, start, end);

                var totalVolume = 0m;
                var approvedCount = 0;
                var declinedCount = 0;
                var totalCount = 0;

                foreach (var txn in transactions)
                {
                    totalCount++;
                    totalVolume += txn.Amount;
                    if (txn.Status == "Approved") approvedCount++;
                    if (txn.Status == "Declined") declinedCount++;
                }

                return Ok(new
                {
                    period = new { start = start.ToString("yyyy-MM-dd"), end = end.ToString("yyyy-MM-dd") },
                    totalTransactions = totalCount,
                    totalVolume,
                    approvedCount,
                    declinedCount,
                    approvalRate = totalCount > 0 ? (decimal)approvedCount / totalCount * 100 : 0
                });
            }
            catch (Exception ex)
            {
                _log.Error("API Error - GetStats", ex);
                return InternalServerError();
            }
        }

        private int GetMerchantIdFromApiKey()
        {
            if (Request.Properties.ContainsKey("MerchantId"))
            {
                return (int)Request.Properties["MerchantId"];
            }
            return 0;
        }
    }
}
