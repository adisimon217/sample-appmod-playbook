using System;
using System.Linq;
using System.Web.Http;
using log4net;
using MerchantHub.Core;
using MerchantHub.Data.Repositories;

namespace MerchantHub.Api.Controllers
{
    [RoutePrefix("api/disputes")]
    public class DisputeApiController : ApiController
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DisputeApiController));

        // GET api/disputes?status=Open&page=1&pageSize=25
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetDisputes(string status = null, int page = 1, int pageSize = 25)
        {
            try
            {
                var merchantId = GetMerchantIdFromApiKey();
                if (merchantId == 0) return Unauthorized();

                var repo = ServiceLocator.Resolve<IDisputeRepository>();
                var disputes = repo.GetByMerchant(merchantId, status, page, pageSize);
                var totalCount = repo.GetCountByMerchant(merchantId, status);

                return Ok(new
                {
                    data = disputes.Select(d => new
                    {
                        d.DisputeId,
                        d.CaseNumber,
                        d.TransactionId,
                        d.Amount,
                        d.Status,
                        d.ReasonCode,
                        d.ReasonDescription,
                        filedDate = d.FiledDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        responseDeadline = d.ResponseDeadline.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        d.CardType,
                        d.Last4Digits
                    }),
                    pagination = new { page, pageSize, totalCount }
                });
            }
            catch (Exception ex)
            {
                _log.Error("API Error - GetDisputes", ex);
                return InternalServerError();
            }
        }

        // GET api/disputes/{id}
        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult GetDispute(int id)
        {
            try
            {
                var merchantId = GetMerchantIdFromApiKey();
                if (merchantId == 0) return Unauthorized();

                var repo = ServiceLocator.Resolve<IDisputeRepository>();
                var dispute = repo.GetById(id);

                if (dispute == null || dispute.MerchantId != merchantId)
                {
                    return NotFound();
                }

                var history = repo.GetDisputeHistory(id);

                return Ok(new
                {
                    dispute = new
                    {
                        dispute.DisputeId,
                        dispute.CaseNumber,
                        dispute.TransactionId,
                        dispute.Amount,
                        dispute.Status,
                        dispute.ReasonCode,
                        dispute.ReasonDescription,
                        dispute.MerchantResponse,
                        dispute.Resolution,
                        filedDate = dispute.FiledDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        responseDeadline = dispute.ResponseDeadline.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        respondedDate = dispute.RespondedDate?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        resolvedDate = dispute.ResolvedDate?.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    },
                    history = history.Select(h => new
                    {
                        h.Action,
                        h.Details,
                        h.PerformedBy,
                        actionDate = h.ActionDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    })
                });
            }
            catch (Exception ex)
            {
                _log.Error("API Error - GetDispute", ex);
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
