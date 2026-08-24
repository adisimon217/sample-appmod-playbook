using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using log4net;
using MerchantHub.Core;
using MerchantHub.Core.Models;
using MerchantHub.Data.Repositories;

namespace MerchantHub.Api.Controllers
{
    /// <summary>
    /// Transaction API for merchant integrations.
    /// Authenticated via API key (see ApiKeyAuthFilter).
    /// </summary>
    [RoutePrefix("api/transactions")]
    public class TransactionApiController : ApiController
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(TransactionApiController));

        // GET api/transactions?startDate=...&endDate=...&page=1&pageSize=100
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetTransactions(DateTime? startDate = null, DateTime? endDate = null,
            string status = null, int page = 1, int pageSize = 100)
        {
            try
            {
                var merchantId = GetMerchantIdFromApiKey();
                if (merchantId == 0)
                {
                    return Unauthorized();
                }

                if (pageSize > 500) pageSize = 500; // Max page size

                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today.AddDays(1);

                var repo = ServiceLocator.Resolve<ITransactionRepository>();
                var transactions = repo.Search(merchantId, start, end, status, null, page, pageSize);
                var totalCount = repo.SearchCount(merchantId, start, end, status, null);

                return Ok(new
                {
                    data = transactions.Select(t => new
                    {
                        t.TransactionId,
                        t.ReferenceNumber,
                        t.Amount,
                        t.Currency,
                        t.Status,
                        t.CardType,
                        t.Last4Digits,
                        t.EntryMode,
                        t.Description,
                        transactionDate = t.TransactionDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        t.AuthorizationCode,
                        t.ResponseCode,
                        t.BatchNumber
                    }),
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _log.Error("API Error - GetTransactions", ex);
                return InternalServerError();
            }
        }

        // GET api/transactions/{id}
        [HttpGet]
        [Route("{id:long}")]
        public IHttpActionResult GetTransaction(long id)
        {
            try
            {
                var merchantId = GetMerchantIdFromApiKey();
                var repo = ServiceLocator.Resolve<ITransactionRepository>();

                var transaction = repo.GetById(id);
                if (transaction == null || transaction.MerchantId != merchantId)
                {
                    return NotFound();
                }

                return Ok(transaction);
            }
            catch (Exception ex)
            {
                _log.Error("API Error - GetTransaction", ex);
                return InternalServerError();
            }
        }

        // GET api/transactions/summary?date=2023-08-15
        [HttpGet]
        [Route("summary")]
        public IHttpActionResult GetDailySummary(DateTime? date = null)
        {
            try
            {
                var merchantId = GetMerchantIdFromApiKey();
                var summaryDate = date ?? DateTime.Today;

                var repo = ServiceLocator.Resolve<ITransactionRepository>();
                var batches = repo.GetBatchSummary(merchantId, summaryDate);

                return Ok(new
                {
                    date = summaryDate.ToString("yyyy-MM-dd"),
                    merchantId,
                    batches
                });
            }
            catch (Exception ex)
            {
                _log.Error("API Error - GetDailySummary", ex);
                return InternalServerError();
            }
        }

        private int GetMerchantIdFromApiKey()
        {
            // Merchant ID is set by ApiKeyAuthFilter
            if (Request.Properties.ContainsKey("MerchantId"))
            {
                return (int)Request.Properties["MerchantId"];
            }
            return 0;
        }
    }
}
