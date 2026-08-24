using System;
using System.Data;
using System.IO;
using Microsoft.Reporting.WinForms;

namespace ComplianceReporter.Core.ReportGeneration
{
    /// <summary>
    /// Generates the Quarterly Compliance Check report for MAS submission.
    /// Uses local SSRS .rdlc report template: QuarterlyComplianceReport.rdlc
    /// 
    /// This report contains:
    /// - AML/CFT compliance checks performed
    /// - KYC verification status of merchant portfolio
    /// - Transaction monitoring alerts and resolutions
    /// - Regulatory threshold breach incidents
    /// - Suspicious Transaction Reports (STR) filed
    /// 
    /// MAS submission deadline: 15 business days after quarter end
    /// </summary>
    public class ComplianceCheckReport
    {
        private readonly string _rdlcPath;

        public ComplianceCheckReport()
        {
            _rdlcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Reports", "QuarterlyComplianceReport.rdlc");
        }

        /// <summary>
        /// Generates the quarterly compliance report PDF.
        /// </summary>
        /// <param name="complianceData">DataSet with compliance data from PayGateDB</param>
        /// <param name="outputFilePath">Full path for the output PDF file</param>
        public void Generate(DataSet complianceData, string outputFilePath)
        {
            if (!File.Exists(_rdlcPath))
            {
                throw new FileNotFoundException(
                    "RDLC report template not found: " + _rdlcPath +
                    ". Ensure Reports folder is deployed with the service.");
            }

            LocalReport report = new LocalReport();
            report.ReportPath = _rdlcPath;

            // Bind compliance data sources
            if (complianceData.Tables.Contains("ComplianceChecks"))
            {
                ReportDataSource checksSource = new ReportDataSource(
                    "ComplianceChecksDataSet",
                    complianceData.Tables["ComplianceChecks"]);
                report.DataSources.Add(checksSource);
            }

            if (complianceData.Tables.Contains("KycStatus"))
            {
                ReportDataSource kycSource = new ReportDataSource(
                    "KycStatusDataSet",
                    complianceData.Tables["KycStatus"]);
                report.DataSources.Add(kycSource);
            }

            if (complianceData.Tables.Contains("AlertsSummary"))
            {
                ReportDataSource alertsSource = new ReportDataSource(
                    "AlertsSummaryDataSet",
                    complianceData.Tables["AlertsSummary"]);
                report.DataSources.Add(alertsSource);
            }

            if (complianceData.Tables.Contains("ThresholdBreaches"))
            {
                ReportDataSource breachSource = new ReportDataSource(
                    "ThresholdBreachesDataSet",
                    complianceData.Tables["ThresholdBreaches"]);
                report.DataSources.Add(breachSource);
            }

            if (complianceData.Tables.Contains("ReportMetadata"))
            {
                ReportDataSource metadataSource = new ReportDataSource(
                    "MetadataDataSet",
                    complianceData.Tables["ReportMetadata"]);
                report.DataSources.Add(metadataSource);
            }

            // Set report parameters
            if (complianceData.Tables.Contains("ReportMetadata") &&
                complianceData.Tables["ReportMetadata"].Rows.Count > 0)
            {
                DataRow metadata = complianceData.Tables["ReportMetadata"].Rows[0];
                report.SetParameters(new ReportParameter[]
                {
                    new ReportParameter("InstitutionCode", metadata["InstitutionCode"].ToString()),
                    new ReportParameter("ReportingEntity", metadata["ReportingEntity"].ToString()),
                    new ReportParameter("Quarter", metadata["ReportType"].ToString()),
                    new ReportParameter("GeneratedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                });
            }

            // Render to PDF
            ReportExporter exporter = new ReportExporter();
            exporter.RenderReportToPdf(report, outputFilePath);
        }
    }
}
