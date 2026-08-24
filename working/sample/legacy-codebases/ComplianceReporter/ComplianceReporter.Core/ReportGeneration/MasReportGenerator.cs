using System;
using System.Configuration;
using System.Data;
using System.IO;
using ComplianceReporter.Data;

namespace ComplianceReporter.Core.ReportGeneration
{
    /// <summary>
    /// Orchestrates the generation of MAS regulatory reports.
    /// Coordinates data retrieval from PayGateDB (via Linked Server) and
    /// renders reports using local .rdlc SSRS report definitions.
    /// 
    /// Report types:
    /// - Monthly Transaction Summary (due by 5th of following month)
    /// - Quarterly Compliance Report (due within 15 business days of quarter end)
    /// - Annual Audit Report (due by Jan 31st for previous year)
    /// 
    /// MAS Reference: MAS Notice PSN02 - Submission of Returns/Reports
    /// </summary>
    public class MasReportGenerator
    {
        private readonly PayGateDataAccess _dataAccess;
        private readonly string _outputPath;
        private readonly string _institutionCode;
        private readonly string _reportingEntity;
        private readonly string _masLicenseNumber;

        public MasReportGenerator(PayGateDataAccess dataAccess, string outputPath)
        {
            _dataAccess = dataAccess;
            _outputPath = outputPath;
            _institutionCode = ConfigurationManager.AppSettings["InstitutionCode"] ?? "VOY001";
            _reportingEntity = ConfigurationManager.AppSettings["ReportingEntity"] ?? "Voyager Payment Solutions Pte Ltd";
            _masLicenseNumber = ConfigurationManager.AppSettings["MasLicenseNumber"] ?? "PS20190001234";

            // Ensure output directory exists
            if (!Directory.Exists(_outputPath))
            {
                Directory.CreateDirectory(_outputPath);
            }
        }

        /// <summary>
        /// Generates the monthly transaction summary for MAS submission.
        /// Returns the file path of the generated PDF.
        /// </summary>
        public string GenerateMonthlyTransactionSummary(DateTime reportDate)
        {
            EventLogger.WriteInfo(string.Format("Generating monthly transaction summary for {0:yyyy-MM}...",
                reportDate));

            // Get transaction data from PayGateDB
            TransactionDataProvider dataProvider = new TransactionDataProvider(_dataAccess);
            DataSet transactionData = dataProvider.GetMonthlyTransactionSummary(
                reportDate.Year, reportDate.Month);

            if (transactionData == null || transactionData.Tables.Count == 0 ||
                transactionData.Tables[0].Rows.Count == 0)
            {
                EventLogger.WriteWarning("No transaction data found for " + reportDate.ToString("yyyy-MM"));
                // Still generate an empty report per MAS requirement
            }

            // Add report metadata
            DataTable metadataTable = CreateMetadataTable(reportDate, "MONTHLY_TXN_SUMMARY");
            transactionData.Tables.Add(metadataTable);

            // Render the report using SSRS local report
            TransactionSummaryReport report = new TransactionSummaryReport();
            string reportFileName = string.Format("MAS_TXN_SUMMARY_{0}_{1:yyyyMM}.pdf",
                _institutionCode, reportDate);

            string outputFilePath = Path.Combine(_outputPath, reportFileName);
            report.Generate(transactionData, outputFilePath);

            EventLogger.WriteInfo("Monthly transaction summary generated: " + outputFilePath);
            return outputFilePath;
        }

        /// <summary>
        /// Generates the quarterly compliance report for MAS submission.
        /// Returns the file path of the generated PDF.
        /// </summary>
        public string GenerateQuarterlyComplianceReport(DateTime reportDate)
        {
            int quarter = (reportDate.Month - 1) / 3 + 1;
            EventLogger.WriteInfo(string.Format("Generating quarterly compliance report Q{0} {1}...",
                quarter, reportDate.Year));

            // Get compliance check data
            TransactionDataProvider dataProvider = new TransactionDataProvider(_dataAccess);
            DataSet complianceData = dataProvider.GetQuarterlyComplianceData(reportDate.Year, quarter);

            // Add report metadata
            DataTable metadataTable = CreateMetadataTable(reportDate,
                string.Format("QUARTERLY_COMPLIANCE_Q{0}", quarter));
            complianceData.Tables.Add(metadataTable);

            // Render the report
            ComplianceCheckReport report = new ComplianceCheckReport();
            string reportFileName = string.Format("MAS_COMPLIANCE_{0}_Q{1}_{2}.pdf",
                _institutionCode, quarter, reportDate.Year);

            string outputFilePath = Path.Combine(_outputPath, reportFileName);
            report.Generate(complianceData, outputFilePath);

            EventLogger.WriteInfo("Quarterly compliance report generated: " + outputFilePath);
            return outputFilePath;
        }

        /// <summary>
        /// Generates the annual audit report for MAS submission.
        /// Returns the file path of the generated PDF.
        /// </summary>
        public string GenerateAnnualAuditReport(DateTime reportDate)
        {
            EventLogger.WriteInfo(string.Format("Generating annual audit report for {0}...",
                reportDate.Year));

            // Get full year transaction and compliance data
            TransactionDataProvider dataProvider = new TransactionDataProvider(_dataAccess);
            DataSet annualData = dataProvider.GetAnnualAuditData(reportDate.Year);

            // Add report metadata
            DataTable metadataTable = CreateMetadataTable(reportDate, "ANNUAL_AUDIT");
            annualData.Tables.Add(metadataTable);

            // Render using the annual audit report template
            ReportExporter exporter = new ReportExporter();
            string reportFileName = string.Format("MAS_ANNUAL_AUDIT_{0}_{1}.pdf",
                _institutionCode, reportDate.Year);

            string outputFilePath = Path.Combine(_outputPath, reportFileName);
            string rdlcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Reports", "AnnualAuditReport.rdlc");

            exporter.ExportToPdf(rdlcPath, annualData, outputFilePath);

            EventLogger.WriteInfo("Annual audit report generated: " + outputFilePath);
            return outputFilePath;
        }

        private DataTable CreateMetadataTable(DateTime reportDate, string reportType)
        {
            DataTable metadata = new DataTable("ReportMetadata");
            metadata.Columns.Add("InstitutionCode", typeof(string));
            metadata.Columns.Add("ReportingEntity", typeof(string));
            metadata.Columns.Add("MasLicenseNumber", typeof(string));
            metadata.Columns.Add("ReportType", typeof(string));
            metadata.Columns.Add("ReportingPeriodEnd", typeof(DateTime));
            metadata.Columns.Add("GeneratedDate", typeof(DateTime));
            metadata.Columns.Add("GeneratedBy", typeof(string));

            DataRow row = metadata.NewRow();
            row["InstitutionCode"] = _institutionCode;
            row["ReportingEntity"] = _reportingEntity;
            row["MasLicenseNumber"] = _masLicenseNumber;
            row["ReportType"] = reportType;
            row["ReportingPeriodEnd"] = reportDate;
            row["GeneratedDate"] = DateTime.Now;
            row["GeneratedBy"] = "ComplianceReporter.Service v1.3.2";
            metadata.Rows.Add(row);

            return metadata;
        }
    }
}
