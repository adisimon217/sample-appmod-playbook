using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Runtime.InteropServices;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using log4net;
using MerchantHub.Core.Models;

namespace MerchantHub.Reports
{
    public interface IReportGenerator
    {
        ReportGenerationResult GenerateReport(string reportType, int merchantId, DateTime startDate, DateTime endDate, string outputPath, string format);
        int GetActiveReportCount();
    }

    /// <summary>
    /// Crystal Reports wrapper for report generation.
    /// Uses COM interop to communicate with Crystal Reports runtime.
    /// NOTE: CrystalDecisions assemblies must be registered in GAC.
    ///       Crystal Reports Runtime v13.0.35 must be installed on the server.
    ///       These assemblies cannot be deployed via NuGet/xcopy.
    /// WARNING: COM objects must be properly released to prevent memory leaks.
    ///          See incident INC-2022-0523 (memory leak in report generation).
    /// </summary>
    public class CrystalReportsWrapper : IReportGenerator, IDisposable
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CrystalReportsWrapper));
        private readonly string _reportOutputPath;
        private readonly string _licenseKey;
        private readonly string _reportTemplatePath;
        private static int _activeReportCount = 0;
        private static readonly object _reportLock = new object();
        private bool _disposed = false;

        // Report template file mapping
        private static readonly Dictionary<string, string> ReportTemplates = new Dictionary<string, string>
        {
            { "MonthlyStatement", "MonthlyStatement.rpt" },
            { "DailyTransaction", "DailyTransactionSummary.rpt" },
            { "Settlement", "SettlementReconciliation.rpt" },
            { "DisputeStatus", "DisputeStatus.rpt" },
            { "MerchantActivity", "MerchantActivity.rpt" },
            { "RevenueByRegion", "RevenueByRegion.rpt" },
            { "Chargeback", "ChargebackSummary.rpt" },
            { "NewMerchant", "NewMerchantReport.rpt" },
            { "AnnualStatement", "AnnualStatement.rpt" },
            { "QuarterlyCompliance", "QuarterlyCompliance.rpt" },
            { "FailedTransactions", "FailedTransactions.rpt" },
            { "MerchantOnboarding", "MerchantOnboarding.rpt" }
        };

        public CrystalReportsWrapper(string reportOutputPath, string licenseKey)
        {
            _reportOutputPath = reportOutputPath;
            _licenseKey = licenseKey;
            _reportTemplatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "Reports");

            _log.Info("CrystalReportsWrapper initialized");
            _log.InfoFormat("Report template path: {0}", _reportTemplatePath);
            _log.InfoFormat("Report output path: {0}", _reportOutputPath);
        }

        /// <summary>
        /// Generate a report using Crystal Reports engine.
        /// COM interop - ensure proper cleanup of COM objects.
        /// </summary>
        public ReportGenerationResult GenerateReport(string reportType, int merchantId,
            DateTime startDate, DateTime endDate, string outputPath, string format)
        {
            ReportDocument reportDocument = null;
            var result = new ReportGenerationResult();

            try
            {
                lock (_reportLock)
                {
                    _activeReportCount++;
                }

                _log.InfoFormat("Generating report: Type={0}, Merchant={1}, Format={2}",
                    reportType, merchantId, format);

                // Find the template
                if (!ReportTemplates.ContainsKey(reportType))
                {
                    return new ReportGenerationResult
                    {
                        Success = false,
                        ErrorMessage = "Unknown report type: " + reportType
                    };
                }

                var templateFile = Path.Combine(_reportTemplatePath, ReportTemplates[reportType]);
                if (!File.Exists(templateFile))
                {
                    _log.ErrorFormat("Report template not found: {0}", templateFile);
                    return new ReportGenerationResult
                    {
                        Success = false,
                        ErrorMessage = "Report template not found: " + ReportTemplates[reportType]
                    };
                }

                // Create report document (COM object)
                reportDocument = new ReportDocument();
                reportDocument.Load(templateFile);

                // Set database connection info
                var connectionString = ConfigurationManager.ConnectionStrings["MerchantHubDB"].ConnectionString;
                var connectionInfo = new ConnectionInfo
                {
                    ServerName = ExtractServerFromConnectionString(connectionString),
                    DatabaseName = ExtractDatabaseFromConnectionString(connectionString),
                    UserID = ExtractUserFromConnectionString(connectionString),
                    Password = ExtractPasswordFromConnectionString(connectionString)
                };

                // Apply connection info to all tables in the report
                foreach (Table table in reportDocument.Database.Tables)
                {
                    var tableLogonInfo = table.LogOnInfo;
                    tableLogonInfo.ConnectionInfo = connectionInfo;
                    table.ApplyLogOnInfo(tableLogonInfo);
                }

                // Set report parameters
                reportDocument.SetParameterValue("@MerchantId", merchantId);
                reportDocument.SetParameterValue("@StartDate", startDate);
                reportDocument.SetParameterValue("@EndDate", endDate);

                // Ensure output directory exists
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // Export to desired format
                ExportFormatType exportFormat;
                switch (format.ToUpper())
                {
                    case "PDF":
                        exportFormat = ExportFormatType.PortableDocFormat;
                        break;
                    case "XLSX":
                        exportFormat = ExportFormatType.Excel;
                        break;
                    case "CSV":
                        exportFormat = ExportFormatType.CharacterSeparatedValues;
                        break;
                    case "RTF":
                        exportFormat = ExportFormatType.RichText;
                        break;
                    default:
                        exportFormat = ExportFormatType.PortableDocFormat;
                        break;
                }

                reportDocument.ExportToDisk(exportFormat, outputPath);

                var fileInfo = new FileInfo(outputPath);
                result.Success = true;
                result.FilePath = outputPath;
                result.FileSize = fileInfo.Length;

                _log.InfoFormat("Report generated successfully: {0} ({1} bytes)", outputPath, fileInfo.Length);
            }
            catch (COMException comEx)
            {
                _log.Error("COM error generating report - Crystal Reports runtime issue", comEx);
                result.Success = false;
                result.ErrorMessage = "Crystal Reports error: " + comEx.Message;
            }
            catch (Exception ex)
            {
                _log.Error("Error generating report", ex);
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                // Critical: Release COM objects to prevent memory leaks
                if (reportDocument != null)
                {
                    try
                    {
                        reportDocument.Close();
                        reportDocument.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _log.Warn("Error disposing report document", ex);
                    }
                }

                lock (_reportLock)
                {
                    _activeReportCount--;
                }
            }

            return result;
        }

        public int GetActiveReportCount()
        {
            return _activeReportCount;
        }

        // Helper methods to parse connection string (legacy approach)
        private string ExtractServerFromConnectionString(string cs)
        {
            return ExtractValue(cs, "Server") ?? ExtractValue(cs, "Data Source") ?? "";
        }

        private string ExtractDatabaseFromConnectionString(string cs)
        {
            return ExtractValue(cs, "Database") ?? ExtractValue(cs, "Initial Catalog") ?? "";
        }

        private string ExtractUserFromConnectionString(string cs)
        {
            return ExtractValue(cs, "User Id") ?? ExtractValue(cs, "UID") ?? "";
        }

        private string ExtractPasswordFromConnectionString(string cs)
        {
            return ExtractValue(cs, "Password") ?? ExtractValue(cs, "PWD") ?? "";
        }

        private string ExtractValue(string connectionString, string key)
        {
            foreach (var part in connectionString.Split(';'))
            {
                var kvp = part.Split(new[] { '=' }, 2);
                if (kvp.Length == 2 && kvp[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp[1].Trim();
                }
            }
            return null;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Release any held resources
                _disposed = true;
                _log.Info("CrystalReportsWrapper disposed");
            }
        }
    }
}
