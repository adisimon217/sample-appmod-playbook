using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using log4net;
using MerchantHub.Core;
using MerchantHub.Core.Services;
using MerchantHub.Data.Repositories;

namespace MerchantHub.Web.Controllers
{
    /// <summary>
    /// Admin controller for internal users.
    /// TODO: Migrate to Windows/AD authentication for this controller (JIRA-5102)
    /// Currently uses Forms Auth with role check.
    /// </summary>
    [Authorize(Roles = "Admin,InternalSupport")]
    public class AdminController : Controller
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(AdminController));

        // GET: /Admin/Index
        public ActionResult Index()
        {
            var merchantRepo = ServiceLocator.Resolve<IMerchantRepository>();
            var transactionRepo = ServiceLocator.Resolve<ITransactionRepository>();

            // Admin dashboard metrics - direct queries
            ViewBag.TotalMerchants = merchantRepo.GetActiveCount();
            ViewBag.TotalTransactionsToday = transactionRepo.GetTodayCount();
            ViewBag.ActiveSessions = System.Web.HttpContext.Current.Application["ActiveSessionCount"] ?? 0;
            ViewBag.PendingDisputes = ServiceLocator.Resolve<IDisputeRepository>().GetPendingCount();

            // High chargeback merchants
            ViewBag.HighChargebackMerchants = merchantRepo.GetHighChargebackMerchants(1.0m);

            return View();
        }

        // GET: /Admin/MerchantSearch
        public ActionResult MerchantSearch(string query, string status = null, int page = 1)
        {
            var merchantRepo = ServiceLocator.Resolve<IMerchantRepository>();
            var merchants = merchantRepo.Search(query, status, page, 50);
            var totalCount = merchantRepo.SearchCount(query, status);

            ViewBag.Query = query;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalCount = totalCount;

            return View(merchants);
        }

        // POST: /Admin/SuspendMerchant
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult SuspendMerchant(int merchantId, string reason)
        {
            try
            {
                var merchantRepo = ServiceLocator.Resolve<IMerchantRepository>();
                var merchant = merchantRepo.GetById(merchantId);

                if (merchant == null)
                    return HttpNotFound();

                merchant.Status = "Suspended";
                merchant.SuspensionReason = reason;
                merchant.SuspendedDate = DateTime.Now;
                merchant.SuspendedBy = User.Identity.Name;
                merchantRepo.Update(merchant);

                // Direct SQL for audit log - pattern seen throughout the app
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["MerchantHubDB"].ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(
                        @"INSERT INTO MH_AuditLog (EntityType, EntityId, Action, UserId, Details, CreatedDate)
                          VALUES ('Merchant', @MerchantId, 'Suspended', @UserId, @Details, GETDATE())", conn))
                    {
                        cmd.Parameters.AddWithValue("@MerchantId", merchantId);
                        cmd.Parameters.AddWithValue("@UserId", User.Identity.Name);
                        cmd.Parameters.AddWithValue("@Details", "Suspended: " + reason);
                        cmd.ExecuteNonQuery();
                    }
                }

                _log.WarnFormat("Merchant {0} suspended by {1}. Reason: {2}",
                    merchantId, User.Identity.Name, reason);

                TempData["Success"] = "Merchant has been suspended.";
                return RedirectToAction("MerchantSearch");
            }
            catch (Exception ex)
            {
                _log.Error("Error suspending merchant", ex);
                TempData["Error"] = "Failed to suspend merchant.";
                return RedirectToAction("MerchantSearch");
            }
        }

        // GET: /Admin/SystemStatus
        public ActionResult SystemStatus()
        {
            var status = new Dictionary<string, object>();

            // Check database connections
            try
            {
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["MerchantHubDB"].ConnectionString))
                {
                    conn.Open();
                    status["MerchantHubDB"] = "Connected";
                }
            }
            catch (Exception ex)
            {
                status["MerchantHubDB"] = "ERROR: " + ex.Message;
            }

            try
            {
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["PayGateDB"].ConnectionString))
                {
                    conn.Open();
                    status["PayGateDB"] = "Connected";
                }
            }
            catch (Exception ex)
            {
                status["PayGateDB"] = "ERROR: " + ex.Message;
            }

            // Check disk space for reports
            var reportPath = ConfigurationManager.AppSettings["MerchantHub.ReportOutputPath"];
            try
            {
                var drive = new System.IO.DriveInfo(System.IO.Path.GetPathRoot(reportPath));
                status["ReportDisk_FreeGB"] = (drive.AvailableFreeSpace / 1073741824.0).ToString("F1");
                status["ReportDisk_TotalGB"] = (drive.TotalSize / 1073741824.0).ToString("F1");
            }
            catch
            {
                status["ReportDisk"] = "Unable to check";
            }

            status["ActiveSessions"] = System.Web.HttpContext.Current.Application["ActiveSessionCount"] ?? 0;
            status["Environment"] = ConfigurationManager.AppSettings["MerchantHub.Environment"];
            status["Version"] = ConfigurationManager.AppSettings["MerchantHub.Version"];
            status["ServerTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            status["MachineName"] = Environment.MachineName;

            ViewBag.Status = status;
            return View();
        }
    }
}
