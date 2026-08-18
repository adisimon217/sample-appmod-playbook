using System;
using System.IO;
using log4net;
using MerchantHub.Core.Models;

namespace MerchantHub.Reports
{
    /// <summary>
    /// High-level report generation orchestrator.
    /// Coordinates between Crystal Reports wrapper and the data layer.
    /// NOTE: This was supposed to replace CrystalReportsWrapper but both exist now.
    ///       CrystalReportsWrapper does the actual generation, this class is just a facade.
    ///       TODO: Consolidate these two classes (JIRA-5234)
    /// </summary>
    public class ReportGenerator
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReportGenerator));
        private readonly CrystalReportsWrapper _wrapper;

        public ReportGenerator(CrystalReportsWrapper wrapper)
        {
            _wrapper = wrapper;
        }

        /// <summary>
        /// Generate a report and return file path.
        /// Handles output directory creation, naming convention, and cleanup.
        /// </summary>
        public string GenerateAndSave(string reportType, int merchantId, DateTime startDate, DateTime endDate, string format = "PDF")
        {
            var outputDir = System.Configuration.ConfigurationManager.AppSettings["MerchantHub.ReportOutputPath"];
            var fileName = string.Format("{0}_{1}_{2}_{3}.{4}",
                reportType,
                merchantId,
                startDate.ToString("yyyyMMdd"),
                endDate.ToString("yyyyMMdd"),
                format.ToLower());
            var outputPath = Path.Combine(outputDir, fileName);

            _log.InfoFormat("ReportGenerator.GenerateAndSave: {0}", fileName);

            var result = _wrapper.GenerateReport(reportType, merchantId, startDate, endDate, outputPath, format);

            if (!result.Success)
            {
                _log.ErrorFormat("Report generation failed: {0}", result.ErrorMessage);
                throw new InvalidOperationException("Report generation failed: " + result.ErrorMessage);
            }

            return outputPath;
        }
    }
}
