using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using log4net;
using MerchantHub.Core;
using MerchantHub.Core.Models;
using MerchantHub.Core.Services;
using MerchantHub.Data.Repositories;

namespace MerchantHub.Web.Controllers
{
    [Authorize]
    public class TransactionController : Controller
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(TransactionController));

        // GET: /Transaction/Index
        public ActionResult Index(int page = 1, int pageSize = 50, string status = null,
            DateTime? startDate = null, DateTime? endDate = null, string search = null)
        {
            try
            {
                var merchantId = ServiceLocator.GetCurrentMerchantId();
                var transactionRepo = ServiceLocator.Resolve<ITransactionRepository>();

                // Default date range to last 30 days
                if (!startDate.HasValue) startDate = DateTime.Today.AddDays(-30);
                if (!endDate.HasValue) endDate = DateTime.Today.AddDays(1);

                var transactions = transactionRepo.Search(merchantId, startDate.Value, endDate.Value,
                    status, search, page, pageSize);
                var totalCount = transactionRepo.SearchCount(merchantId, startDate.Value, endDate.Value,
                    status, search);

                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                ViewBag.Status = status;
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;
                ViewBag.Search = search;

                // Status options for filter dropdown
                ViewBag.StatusOptions = new List<SelectListItem>
                {
                    new SelectListItem { Text = "All", Value = "" },
                    new SelectListItem { Text = "Approved", Value = "Approved" },
                    new SelectListItem { Text = "Declined", Value = "Declined" },
                    new SelectListItem { Text = "Pending", Value = "Pending" },
                    new SelectListItem { Text = "Refunded", Value = "Refunded" },
                    new SelectListItem { Text = "Chargeback", Value = "Chargeback" },
                    new SelectListItem { Text = "Voided", Value = "Voided" }
                };

                return View(transactions);
            }
            catch (Exception ex)
            {
                _log.Error("Error loading transactions", ex);
                TempData["Error"] = "An error occurred loading transactions.";
                return RedirectToAction("Dashboard", "Merchant");
            }
        }

        // GET: /Transaction/Details/5
        public ActionResult Details(long id)
        {
            var merchantId = ServiceLocator.GetCurrentMerchantId();
            var transactionRepo = ServiceLocator.Resolve<ITransactionRepository>();

            var transaction = transactionRepo.GetById(id);
            if (transaction == null || transaction.MerchantId != merchantId)
            {
                return HttpNotFound();
            }

            // Get related transactions (same card)
            ViewBag.RelatedTransactions = transactionRepo.GetByCardFingerprint(
                transaction.CardFingerprint, merchantId, 5);

            return View(transaction);
        }

        // POST: /Transaction/Refund
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Refund(long transactionId, decimal refundAmount, string reason)
        {
            try
            {
                var merchantId = ServiceLocator.GetCurrentMerchantId();
                var transactionService = ServiceLocator.Resolve<ITransactionService>();

                // Business logic in controller - should be in service
                var transactionRepo = ServiceLocator.Resolve<ITransactionRepository>();
                var transaction = transactionRepo.GetById(transactionId);

                if (transaction == null || transaction.MerchantId != merchantId)
                {
                    return HttpNotFound();
                }

                if (transaction.Status != "Approved")
                {
                    TempData["Error"] = "Only approved transactions can be refunded.";
                    return RedirectToAction("Details", new { id = transactionId });
                }

                if (refundAmount > transaction.Amount)
                {
                    TempData["Error"] = "Refund amount cannot exceed the original transaction amount.";
                    return RedirectToAction("Details", new { id = transactionId });
                }

                if ((DateTime.Now - transaction.TransactionDate).TotalDays > 120)
                {
                    TempData["Error"] = "Transactions older than 120 days cannot be refunded.";
                    return RedirectToAction("Details", new { id = transactionId });
                }

                var result = transactionService.ProcessRefund(transactionId, refundAmount, reason, User.Identity.Name);

                if (result.Success)
                {
                    TempData["Success"] = string.Format("Refund of {0:C} processed successfully. Reference: {1}",
                        refundAmount, result.ReferenceNumber);
                    _log.InfoFormat("Refund processed: TxnId={0}, Amount={1:C}, By={2}",
                        transactionId, refundAmount, User.Identity.Name);
                }
                else
                {
                    TempData["Error"] = "Refund failed: " + result.ErrorMessage;
                    _log.WarnFormat("Refund failed: TxnId={0}, Reason={1}", transactionId, result.ErrorMessage);
                }

                return RedirectToAction("Details", new { id = transactionId });
            }
            catch (Exception ex)
            {
                _log.Error("Error processing refund", ex);
                TempData["Error"] = "An error occurred processing the refund.";
                return RedirectToAction("Details", new { id = transactionId });
            }
        }

        // GET: /Transaction/BatchSummary
        public ActionResult BatchSummary(DateTime? date = null)
        {
            var merchantId = ServiceLocator.GetCurrentMerchantId();
            var batchDate = date ?? DateTime.Today;

            var transactionRepo = ServiceLocator.Resolve<ITransactionRepository>();
            var batches = transactionRepo.GetBatchSummary(merchantId, batchDate);

            ViewBag.BatchDate = batchDate;
            return View(batches);
        }
    }
}
