using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using MerchantHub.Core.Models;
using MerchantHub.Data.Repositories;

namespace MerchantHub.Core.Services
{
    public interface IReportService
    {
        List<ReportConfig> GetAvailableReports(int merchantId);
        List<GeneratedReport> GetRecentGeneratedReports(int merchantId, int count);
        MonthlyStatement GetMonthlyStatement(int merchantId, int year, int month);
        void GenerateNightlyStatements();
    }

    public class ReportService : IReportService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReportService));
        private readonly IMonthlyStatementRepository _statementRepo;
        private readonly IMerchantRepository _merchantRepo;

        public ReportService(IMonthlyStatementRepository statementRepo, IMerchantRepository merchantRepo)
        {
            _statementRepo = statementRepo;
            _merchantRepo = merchantRepo;
        }

        public List<ReportConfig> GetAvailableReports(int merchantId)
        {
            // Hardcoded report configs - should be in database but was never migrated
            var reports = new List<ReportConfig>
            {
                new ReportConfig { ReportType = "MonthlyStatement", ReportName = "Monthly Statement", TemplateFile = "MonthlyStatement.rpt", RequiresDateRange = true, RequiresMerchantId = true, Category = "Financial", IsActive = true, AvailableFormats = new List<string> { "PDF", "XLSX" } },
                new ReportConfig { ReportType = "DailyTransaction", ReportName = "Daily Transaction Summary", TemplateFile = "DailyTransactionSummary.rpt", RequiresDateRange = true, RequiresMerchantId = true, Category = "Financial", IsActive = true, AvailableFormats = new List<string> { "PDF", "CSV" } },
                new ReportConfig { ReportType = "Settlement", ReportName = "Settlement Reconciliation", TemplateFile = "SettlementReconciliation.rpt", RequiresDateRange = true, RequiresMerchantId = true, Category = "Financial", IsActive = true, AvailableFormats = new List<string> { "PDF", "XLSX" } },
                new ReportConfig { ReportType = "DisputeStatus", ReportName = "Dispute Status Report", TemplateFile = "DisputeStatus.rpt", RequiresDateRange = false, RequiresMerchantId = true, Category = "Activity", IsActive = true, AvailableFormats = new List<string> { "PDF" } },
                new ReportConfig { ReportType = "MerchantActivity", ReportName = "Merchant Activity Report", TemplateFile = "MerchantActivity.rpt", RequiresDateRange = true, RequiresMerchantId = true, Category = "Activity", IsActive = true, AvailableFormats = new List<string> { "PDF", "XLSX" } },
                new ReportConfig { ReportType = "Chargeback", ReportName = "Chargeback Summary", TemplateFile = "ChargebackSummary.rpt", RequiresDateRange = true, RequiresMerchantId = true, Category = "Compliance", IsActive = true, AvailableFormats = new List<string> { "PDF" } },
                new ReportConfig { ReportType = "QuarterlyCompliance", ReportName = "Quarterly Compliance Report", TemplateFile = "QuarterlyCompliance.rpt", RequiresDateRange = true, RequiresMerchantId = true, Category = "Compliance", IsActive = true, AvailableFormats = new List<string> { "PDF" } }
            };

            return reports.Where(r => r.IsActive).ToList();
        }

        public List<GeneratedReport> GetRecentGeneratedReports(int merchantId, int count)
        {
            return _statementRepo.GetRecentReports(merchantId, count);
        }

        public MonthlyStatement GetMonthlyStatement(int merchantId, int year, int month)
        {
            return _statementRepo.GetStatement(merchantId, year, month);
        }

        /// <summary>
        /// Nightly statement generation job (called by Hangfire at 2 AM).
        /// Generates monthly statements for all active merchants on the 1st of each month,
        /// and daily interim statements for Enterprise tier merchants.
        /// </summary>
        public void GenerateNightlyStatements()
        {
            _log.Info("Starting nightly statement generation...");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var today = DateTime.Today;

                // Generate monthly statements on the 1st
                if (today.Day == 1)
                {
                    var previousMonth = today.AddMonths(-1);
                    GenerateMonthlyStatementsForAll(previousMonth.Year, previousMonth.Month);
                }

                // Generate daily statements for Enterprise merchants
                var enterpriseMerchants = _merchantRepo.GetAll()
                    .Where(m => m.Status == "Active" && m.Tier == "Enterprise")
                    .ToList();

                int dailyCount = 0;
                foreach (var merchant in enterpriseMerchants)
                {
                    try
                    {
                        // Use Crystal Reports to generate statement PDF
                        var reportGenerator = ServiceLocator.Resolve<Reports.IReportGenerator>();
                        var outputPath = System.Configuration.ConfigurationManager.AppSettings["MerchantHub.ReportOutputPath"];
                        var fileName = string.Format("DailyStatement_{0}_{1}.pdf",
                            merchant.MerchantId, today.AddDays(-1).ToString("yyyyMMdd"));
                        var fullPath = System.IO.Path.Combine(outputPath, fileName);

                        reportGenerator.GenerateReport("DailyTransaction", merchant.MerchantId,
                            today.AddDays(-1), today, fullPath, "PDF");
                        dailyCount++;
                    }
                    catch (Exception ex)
                    {
                        _log.ErrorFormat("Failed to generate daily statement for merchant {0}: {1}",
                            merchant.MerchantId, ex.Message);
                    }
                }

                stopwatch.Stop();
                _log.InfoFormat("Nightly statement generation completed in {0}ms. Daily statements: {1}",
                    stopwatch.ElapsedMilliseconds, dailyCount);
            }
            catch (Exception ex)
            {
                _log.Error("Error in nightly statement generation", ex);
                throw;
            }
        }

        private void GenerateMonthlyStatementsForAll(int year, int month)
        {
            _log.InfoFormat("Generating monthly statements for {0}/{1}...", year, month);

            var merchants = _merchantRepo.GetAll().Where(m => m.Status == "Active").ToList();
            int successCount = 0;
            int failCount = 0;

            foreach (var merchant in merchants)
            {
                try
                {
                    _statementRepo.GenerateMonthlyStatement(merchant.MerchantId, year, month);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failCount++;
                    _log.ErrorFormat("Failed to generate monthly statement for merchant {0}: {1}",
                        merchant.MerchantId, ex.Message);
                }
            }

            _log.InfoFormat("Monthly statement generation complete. Success: {0}, Failed: {1}",
                successCount, failCount);
        }
    }
}
