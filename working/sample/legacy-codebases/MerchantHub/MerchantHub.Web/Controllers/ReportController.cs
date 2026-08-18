using System;
using System.Configuration;
using System.IO;
using System.Web.Mvc;
using log4net;
using MerchantHub.Core;
using MerchantHub.Core.Services;
using MerchantHub.Reports;

namespace MerchantHub.Web.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReportController));

        // GET: /Report/Index
        public ActionResult Index()
        {
            var merchantId = ServiceLocator.GetCurrentMerchantId();
            var reportService = ServiceLocator.Resolve<IReportService>();

            ViewBag.AvailableReports = reportService.GetAvailableReports(merchantId);
            ViewBag.RecentReports = reportService.GetRecentGeneratedReports(merchantId, 10);

            return View();
        }

        // POST: /Report/Generate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Generate(string reportType, DateTime startDate, DateTime endDate, string format = "PDF")
        {
            try
            {
                var merchantId = ServiceLocator.GetCurrentMerchantId();
                _log.InfoFormat("Report generation requested: Type={0}, Merchant={1}, Range={2} to {3}",
                    reportType, merchantId, startDate.ToShortDateString(), endDate.ToShortDateString());

                // Validate date range
                if (endDate < startDate)
                {
                    TempData["Error"] = "End date must be after start date.";
                    return RedirectToAction("Index");
                }

                if ((endDate - startDate).TotalDays > 365)
                {
                    TempData["Error"] = "Report date range cannot exceed 365 days.";
                    return RedirectToAction("Index");
                }

                // Check concurrent report limit
                var maxConcurrent = int.Parse(ConfigurationManager.AppSettings["MerchantHub.MaxConcurrentReports"]);
                var reportGenerator = ServiceLocator.Resolve<IReportGenerator>();

                if (reportGenerator.GetActiveReportCount() >= maxConcurrent)
                {
                    TempData["Error"] = "Maximum concurrent report limit reached. Please try again in a few minutes.";
                    return RedirectToAction("Index");
                }

                // Generate report using Crystal Reports
                var outputPath = ConfigurationManager.AppSettings["MerchantHub.ReportOutputPath"];
                var fileName = string.Format("{0}_{1}_{2}.{3}",
                    reportType, merchantId, DateTime.Now.ToString("yyyyMMddHHmmss"),
                    format.ToLower());
                var fullPath = Path.Combine(outputPath, fileName);

                var result = reportGenerator.GenerateReport(reportType, merchantId, startDate, endDate, fullPath, format);

                if (result.Success)
                {
                    TempData["Success"] = "Report generated successfully.";
                    _log.InfoFormat("Report generated: {0} ({1} bytes)", fileName, result.FileSize);
                    return RedirectToAction("Download", new { fileName = fileName });
                }
                else
                {
                    TempData["Error"] = "Report generation failed: " + result.ErrorMessage;
                    _log.WarnFormat("Report generation failed: {0}", result.ErrorMessage);
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error generating report", ex);
                TempData["Error"] = "An unexpected error occurred while generating the report.";
                return RedirectToAction("Index");
            }
        }

        // GET: /Report/Download?fileName=xxx
        public ActionResult Download(string fileName)
        {
            var outputPath = ConfigurationManager.AppSettings["MerchantHub.ReportOutputPath"];
            var fullPath = Path.Combine(outputPath, fileName);

            if (!System.IO.File.Exists(fullPath))
            {
                return HttpNotFound("Report file not found.");
            }

            var contentType = fileName.EndsWith(".pdf") ? "application/pdf" :
                              fileName.EndsWith(".xlsx") ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" :
                              "application/octet-stream";

            return File(fullPath, contentType, fileName);
        }

        // GET: /Report/MonthlyStatement
        public ActionResult MonthlyStatement(int year, int month)
        {
            var merchantId = ServiceLocator.GetCurrentMerchantId();
            var reportService = ServiceLocator.Resolve<IReportService>();

            var statement = reportService.GetMonthlyStatement(merchantId, year, month);
            if (statement == null)
            {
                TempData["Error"] = "Statement not available for the selected period.";
                return RedirectToAction("Index");
            }

            return View(statement);
        }
    }
}
