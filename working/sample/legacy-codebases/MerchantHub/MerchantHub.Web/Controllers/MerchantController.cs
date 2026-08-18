using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using log4net;
using MerchantHub.Core;
using MerchantHub.Core.Models;
using MerchantHub.Core.Services;
using MerchantHub.Data;
using MerchantHub.Data.Repositories;

namespace MerchantHub.Web.Controllers
{
    /// <summary>
    /// Main merchant controller - handles dashboard, profile, settings
    /// NOTE: This controller has grown organically over 4 years. We know it's too big.
    ///       Refactoring ticket JIRA-4521 has been open since 2021.
    /// </summary>
    [Authorize]
    public class MerchantController : Controller
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MerchantController));

        // GET: /Merchant/{merchantId}/Dashboard
        public ActionResult Dashboard(int? merchantId)
        {
            try
            {
                var currentMerchantId = merchantId ?? ServiceLocator.GetCurrentMerchantId();
                if (currentMerchantId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Business logic mixed directly in controller - should be in service layer
                var merchantRepo = ServiceLocator.Resolve<IMerchantRepository>();
                var transactionRepo = ServiceLocator.Resolve<ITransactionRepository>();
                var disputeRepo = ServiceLocator.Resolve<IDisputeRepository>();

                var merchant = merchantRepo.GetById(currentMerchantId);
                if (merchant == null)
                {
                    _log.WarnFormat("Merchant not found: {0}", currentMerchantId);
                    return HttpNotFound("Merchant not found");
                }

                // Calculate dashboard metrics inline (should be in service)
                var today = DateTime.Today;
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);

                var monthlyTransactions = transactionRepo.GetByMerchantAndDateRange(
                    currentMerchantId, startOfMonth, today.AddDays(1));
                var weeklyTransactions = transactionRepo.GetByMerchantAndDateRange(
                    currentMerchantId, startOfWeek, today.AddDays(1));
                var todayTransactions = transactionRepo.GetByMerchantAndDateRange(
                    currentMerchantId, today, today.AddDays(1));

                // Calculate totals - raw business logic in controller
                var monthlyVolume = monthlyTransactions.Sum(t => t.Amount);
                var monthlyCount = monthlyTransactions.Count();
                var weeklyVolume = weeklyTransactions.Sum(t => t.Amount);
                var todayVolume = todayTransactions.Sum(t => t.Amount);
                var todayCount = todayTransactions.Count();

                // Calculate approval rate
                var approvedCount = monthlyTransactions.Count(t => t.Status == "Approved");
                var approvalRate = monthlyCount > 0 ? (decimal)approvedCount / monthlyCount * 100 : 0;

                // Get open disputes
                var openDisputes = disputeRepo.GetOpenDisputesByMerchant(currentMerchantId);
                var disputeAmount = openDisputes.Sum(d => d.Amount);

                // Calculate chargeback ratio (compliance requirement)
                var chargebackCount = monthlyTransactions.Count(t => t.Status == "Chargeback");
                var chargebackRatio = monthlyCount > 0 ? (decimal)chargebackCount / monthlyCount * 100 : 0;

                // Flag if chargeback ratio exceeds threshold
                if (chargebackRatio > 1.0m)
                {
                    _log.WarnFormat("Merchant {0} chargeback ratio {1:F2}% exceeds 1% threshold",
                        currentMerchantId, chargebackRatio);
                    ViewBag.ChargebackWarning = true;
                }

                // Get recent activity for activity feed
                var recentTransactions = transactionRepo.GetRecentByMerchant(currentMerchantId, 10);

                // Check if merchant needs to update profile (compliance)
                var daysSinceLastUpdate = (DateTime.Now - merchant.LastProfileUpdate).Days;
                if (daysSinceLastUpdate > 365)
                {
                    ViewBag.ProfileUpdateRequired = true;
                }

                // Build the view model inline (no separate VM class)
                ViewBag.Merchant = merchant;
                ViewBag.MonthlyVolume = monthlyVolume;
                ViewBag.MonthlyCount = monthlyCount;
                ViewBag.WeeklyVolume = weeklyVolume;
                ViewBag.TodayVolume = todayVolume;
                ViewBag.TodayCount = todayCount;
                ViewBag.ApprovalRate = approvalRate;
                ViewBag.OpenDisputeCount = openDisputes.Count();
                ViewBag.DisputeAmount = disputeAmount;
                ViewBag.ChargebackRatio = chargebackRatio;
                ViewBag.RecentTransactions = recentTransactions;
                ViewBag.MerchantStatus = merchant.Status;

                // Store in session for other pages
                Session["CurrentMerchantId"] = currentMerchantId;
                Session["MerchantName"] = merchant.BusinessName;
                Session["MerchantTier"] = merchant.Tier;

                return View();
            }
            catch (Exception ex)
            {
                _log.Error("Error loading merchant dashboard", ex);
                TempData["Error"] = "An error occurred while loading the dashboard. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Merchant/Profile
        public ActionResult Profile()
        {
            var merchantId = ServiceLocator.GetCurrentMerchantId();
            var repo = ServiceLocator.Resolve<IMerchantRepository>();
            var merchant = repo.GetById(merchantId);

            if (merchant == null)
                return HttpNotFound();

            return View(merchant);
        }

        // POST: /Merchant/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Profile(Merchant model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var merchantId = ServiceLocator.GetCurrentMerchantId();

                // Validation logic mixed in controller
                if (string.IsNullOrWhiteSpace(model.BusinessName))
                {
                    ModelState.AddModelError("BusinessName", "Business name is required");
                    return View(model);
                }

                if (model.BusinessName.Length > 200)
                {
                    ModelState.AddModelError("BusinessName", "Business name cannot exceed 200 characters");
                    return View(model);
                }

                // Validate EIN format
                if (!string.IsNullOrEmpty(model.EIN))
                {
                    var einClean = model.EIN.Replace("-", "");
                    if (einClean.Length != 9 || !einClean.All(char.IsDigit))
                    {
                        ModelState.AddModelError("EIN", "Invalid EIN format. Expected: XX-XXXXXXX");
                        return View(model);
                    }
                }

                // Direct database operations in controller
                var repo = ServiceLocator.Resolve<IMerchantRepository>();
                var existingMerchant = repo.GetById(merchantId);

                existingMerchant.BusinessName = model.BusinessName;
                existingMerchant.DBA = model.DBA;
                existingMerchant.EIN = model.EIN;
                existingMerchant.Address1 = model.Address1;
                existingMerchant.Address2 = model.Address2;
                existingMerchant.City = model.City;
                existingMerchant.State = model.State;
                existingMerchant.ZipCode = model.ZipCode;
                existingMerchant.Phone = model.Phone;
                existingMerchant.Email = model.Email;
                existingMerchant.Website = model.Website;
                existingMerchant.LastProfileUpdate = DateTime.Now;
                existingMerchant.ModifiedBy = User.Identity.Name;
                existingMerchant.ModifiedDate = DateTime.Now;

                repo.Update(existingMerchant);

                // Audit log - direct SQL because the audit service wasn't built yet
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["MerchantHubDB"].ConnectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand(
                        "INSERT INTO MH_AuditLog (EntityType, EntityId, Action, UserId, Details, CreatedDate) " +
                        "VALUES (@EntityType, @EntityId, @Action, @UserId, @Details, GETDATE())", conn);
                    cmd.Parameters.AddWithValue("@EntityType", "Merchant");
                    cmd.Parameters.AddWithValue("@EntityId", merchantId);
                    cmd.Parameters.AddWithValue("@Action", "ProfileUpdate");
                    cmd.Parameters.AddWithValue("@UserId", User.Identity.Name);
                    cmd.Parameters.AddWithValue("@Details", "Profile updated: " + model.BusinessName);
                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = "Profile updated successfully.";
                _log.InfoFormat("Merchant {0} profile updated by {1}", merchantId, User.Identity.Name);

                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                _log.Error("Error updating merchant profile", ex);
                ModelState.AddModelError("", "An error occurred while saving. Please try again.");
                return View(model);
            }
        }

        // GET: /Merchant/Statements
        public ActionResult Statements(int page = 1, int pageSize = 20)
        {
            var merchantId = ServiceLocator.GetCurrentMerchantId();
            var statementRepo = ServiceLocator.Resolve<IMonthlyStatementRepository>();

            var statements = statementRepo.GetByMerchant(merchantId, page, pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = statementRepo.GetCountByMerchant(merchantId);

            return View(statements);
        }

        // POST: /Merchant/UploadDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadDocument(HttpPostedFileBase file, string documentType)
        {
            try
            {
                var merchantId = ServiceLocator.GetCurrentMerchantId();

                if (file == null || file.ContentLength == 0)
                {
                    TempData["Error"] = "Please select a file to upload.";
                    return RedirectToAction("Profile");
                }

                // File size check - hardcoded limit
                var maxSize = int.Parse(ConfigurationManager.AppSettings["MerchantHub.MaxUploadSizeMB"]) * 1024 * 1024;
                if (file.ContentLength > maxSize)
                {
                    TempData["Error"] = "File size exceeds the maximum allowed (25 MB).";
                    return RedirectToAction("Profile");
                }

                // Allowed extensions - hardcoded list
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["Error"] = "File type not allowed. Accepted: PDF, DOC, DOCX, JPG, PNG";
                    return RedirectToAction("Profile");
                }

                // Save to local disk - D:\MerchantHub\Uploads\{MerchantId}\
                var uploadPath = ConfigurationManager.AppSettings["MerchantHub.UploadPath"];
                var merchantFolder = Path.Combine(uploadPath, merchantId.ToString());
                if (!Directory.Exists(merchantFolder))
                {
                    Directory.CreateDirectory(merchantFolder);
                }

                var fileName = string.Format("{0}_{1}_{2}{3}",
                    documentType,
                    DateTime.Now.ToString("yyyyMMdd_HHmmss"),
                    Guid.NewGuid().ToString("N").Substring(0, 8),
                    extension);
                var filePath = Path.Combine(merchantFolder, fileName);

                file.SaveAs(filePath);

                // Store file reference in database
                var repo = ServiceLocator.Resolve<IMerchantRepository>();
                repo.AddDocument(merchantId, documentType, fileName, filePath, file.ContentLength);

                _log.InfoFormat("Document uploaded for merchant {0}: {1} ({2} bytes)",
                    merchantId, fileName, file.ContentLength);

                TempData["Success"] = "Document uploaded successfully.";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                _log.Error("Error uploading document", ex);
                TempData["Error"] = "An error occurred while uploading the document.";
                return RedirectToAction("Profile");
            }
        }

        // GET: /Merchant/Settings
        public ActionResult Settings()
        {
            var merchantId = ServiceLocator.GetCurrentMerchantId();
            var repo = ServiceLocator.Resolve<IMerchantRepository>();
            var merchant = repo.GetById(merchantId);

            ViewBag.NotificationPreferences = repo.GetNotificationPreferences(merchantId);
            ViewBag.ApiKeys = repo.GetApiKeys(merchantId);

            return View(merchant);
        }

        // POST: /Merchant/GenerateApiKey
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerateApiKey(string keyName)
        {
            var merchantId = ServiceLocator.GetCurrentMerchantId();
            var repo = ServiceLocator.Resolve<IMerchantRepository>();

            // Generate new API key - simple implementation
            var apiKey = "mh_" + Guid.NewGuid().ToString("N");
            repo.AddApiKey(merchantId, keyName, apiKey, User.Identity.Name);

            _log.InfoFormat("API key generated for merchant {0} by {1}: {2}",
                merchantId, User.Identity.Name, keyName);

            TempData["NewApiKey"] = apiKey;
            TempData["Success"] = "API key generated. Please copy it now - it won't be shown again.";

            return RedirectToAction("Settings");
        }

        // GET: /Merchant/Export
        [HttpGet]
        public ActionResult Export(string format = "csv")
        {
            var merchantId = ServiceLocator.GetCurrentMerchantId();
            var transactionRepo = ServiceLocator.Resolve<ITransactionRepository>();

            // Export all transactions for the current month
            var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var transactions = transactionRepo.GetByMerchantAndDateRange(merchantId, startOfMonth, DateTime.Now);

            if (format.ToLower() == "csv")
            {
                var csv = "TransactionId,Date,Amount,Status,CardType,Last4,Description\r\n";
                foreach (var txn in transactions)
                {
                    csv += string.Format("{0},{1},{2},{3},{4},{5},{6}\r\n",
                        txn.TransactionId,
                        txn.TransactionDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        txn.Amount.ToString("F2"),
                        txn.Status,
                        txn.CardType,
                        txn.Last4Digits,
                        txn.Description?.Replace(",", " ") ?? "");
                }

                return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv",
                    string.Format("transactions_{0}_{1}.csv", merchantId, DateTime.Now.ToString("yyyyMMdd")));
            }

            TempData["Error"] = "Unsupported export format.";
            return RedirectToAction("Dashboard");
        }
    }
}
