using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using log4net;
using MerchantHub.Core;
using MerchantHub.Core.Models;
using MerchantHub.Core.Services;
using MerchantHub.Data.Repositories;

namespace MerchantHub.Web.Controllers
{
    [Authorize]
    public class DisputeController : Controller
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DisputeController));

        // GET: /Dispute/Index
        public ActionResult Index(string status = null, int page = 1, int pageSize = 25)
        {
            var merchantId = ServiceLocator.GetCurrentMerchantId();
            var disputeRepo = ServiceLocator.Resolve<IDisputeRepository>();

            var disputes = disputeRepo.GetByMerchant(merchantId, status, page, pageSize);
            var totalCount = disputeRepo.GetCountByMerchant(merchantId, status);

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.Status = status;

            ViewBag.StatusOptions = new List<SelectListItem>
            {
                new SelectListItem { Text = "All", Value = "" },
                new SelectListItem { Text = "Open", Value = "Open" },
                new SelectListItem { Text = "Under Review", Value = "UnderReview" },
                new SelectListItem { Text = "Responded", Value = "Responded" },
                new SelectListItem { Text = "Won", Value = "Won" },
                new SelectListItem { Text = "Lost", Value = "Lost" },
                new SelectListItem { Text = "Expired", Value = "Expired" }
            };

            return View(disputes);
        }

        // GET: /Dispute/Details/5
        public ActionResult Details(int id)
        {
            var merchantId = ServiceLocator.GetCurrentMerchantId();
            var disputeRepo = ServiceLocator.Resolve<IDisputeRepository>();

            var dispute = disputeRepo.GetById(id);
            if (dispute == null || dispute.MerchantId != merchantId)
            {
                return HttpNotFound();
            }

            ViewBag.History = disputeRepo.GetDisputeHistory(id);
            ViewBag.Documents = disputeRepo.GetDisputeDocuments(id);

            return View(dispute);
        }

        // POST: /Dispute/Respond
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Respond(int disputeId, string responseText, HttpPostedFileBase[] evidence)
        {
            try
            {
                var merchantId = ServiceLocator.GetCurrentMerchantId();
                var disputeService = ServiceLocator.Resolve<IDisputeService>();
                var disputeRepo = ServiceLocator.Resolve<IDisputeRepository>();

                var dispute = disputeRepo.GetById(disputeId);
                if (dispute == null || dispute.MerchantId != merchantId)
                {
                    return HttpNotFound();
                }

                // Check response deadline
                if (dispute.ResponseDeadline < DateTime.Now)
                {
                    TempData["Error"] = "The response deadline has passed for this dispute.";
                    return RedirectToAction("Details", new { id = disputeId });
                }

                // Save evidence files
                var evidenceFiles = new List<string>();
                if (evidence != null)
                {
                    var uploadPath = System.Configuration.ConfigurationManager.AppSettings["MerchantHub.UploadPath"];
                    var disputeFolder = Path.Combine(uploadPath, "Disputes", disputeId.ToString());

                    if (!Directory.Exists(disputeFolder))
                    {
                        Directory.CreateDirectory(disputeFolder);
                    }

                    foreach (var file in evidence.Where(f => f != null && f.ContentLength > 0))
                    {
                        var fileName = Path.GetFileName(file.FileName);
                        var savePath = Path.Combine(disputeFolder, fileName);
                        file.SaveAs(savePath);
                        evidenceFiles.Add(savePath);
                    }
                }

                var result = disputeService.SubmitResponse(disputeId, responseText, evidenceFiles, User.Identity.Name);

                if (result)
                {
                    TempData["Success"] = "Dispute response submitted successfully.";
                    _log.InfoFormat("Dispute {0} response submitted by {1}", disputeId, User.Identity.Name);
                }
                else
                {
                    TempData["Error"] = "Failed to submit dispute response.";
                }

                return RedirectToAction("Details", new { id = disputeId });
            }
            catch (Exception ex)
            {
                _log.Error("Error responding to dispute", ex);
                TempData["Error"] = "An error occurred while submitting your response.";
                return RedirectToAction("Details", new { id = disputeId });
            }
        }
    }
}
