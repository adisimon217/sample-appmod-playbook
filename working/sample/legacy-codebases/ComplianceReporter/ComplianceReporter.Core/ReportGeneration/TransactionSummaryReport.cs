using System;
using System.Data;
using System.IO;
using Microsoft.Reporting.WinForms;

namespace ComplianceReporter.Core.ReportGeneration
{
    /// <summary>
    /// Generates the Monthly Transaction Summary report for MAS submission.
    /// Uses local SSRS .rdlc report template: MonthlyTransactionSummary.rdlc
    /// 
    /// This report contains:
    /// - Total transaction volume and value by payment type
    /// - Cross-border transaction breakdown
    /// - Suspicious transaction flags (if any)
    /// - Settlement summary
    /// 
    /// MAS submission deadline: 5th of the following month
    /// </summary>
    public class TransactionSummaryReport
    {
        private readonly string _rdlcPath;

        public TransactionSummaryReport()
        {
            _rdlcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Reports", "MonthlyTransactionSummary.rdlc");
        }

        /// <summary>
        /// Generates the monthly transaction summary PDF report.
        /// </summary>
        /// <param name="transactionData">DataSet with transaction summary data from PayGateDB</param>
        /// <param name="outputFilePath">Full path for the output PDF file</param>
        public void Generate(DataSet transactionData, string outputFilePath)
        {
            if (!File.Exists(_rdlcPath))
            {
                throw new FileNotFoundException(
                    "RDLC report template not found: " + _rdlcPath +
                    ". Ensure Reports folder is deployed with the service.");
            }

            LocalReport report = new LocalReport();
            report.ReportPath = _rdlcPath;

            // Bind data sources to the report
            if (transactionData.Tables.Contains("TransactionSummary"))
            {
                ReportDataSource transactionSource = new ReportDataSource(
                    "TransactionSummaryDataSet",
                    transactionData.Tables["TransactionSummary"]);
                report.DataSources.Add(transactionSource);
            }

            if (transactionData.Tables.Contains("CrossBorderTransactions"))
            {
                ReportDataSource crossBorderSource = new ReportDataSource(
                    "CrossBorderDataSet",
                    transactionData.Tables["CrossBorderTransactions"]);
                report.DataSources.Add(crossBorderSource);
            }

            if (transactionData.Tables.Contains("ReportMetadata"))
            {
                ReportDataSource metadataSource = new ReportDataSource(
                    "MetadataDataSet",
                    transactionData.Tables["ReportMetadata"]);
                report.DataSources.Add(metadataSource);
            }

            // Set report parameters
            if (transactionData.Tables.Contains("ReportMetadata") &&
                transactionData.Tables["ReportMetadata"].Rows.Count > 0)
            {
                DataRow metadata = transactionData.Tables["ReportMetadata"].Rows[0];
                report.SetParameters(new ReportParameter[]
                {
                    new ReportParameter("InstitutionCode", metadata["InstitutionCode"].ToString()),
                    new ReportParameter("ReportingEntity", metadata["ReportingEntity"].ToString()),
                    new ReportParameter("ReportingPeriod", metadata["ReportingPeriodEnd"].ToString()),
                    new ReportParameter("GeneratedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                });
            }

            // Render to PDF
            ReportExporter exporter = new ReportExporter();
            exporter.RenderReportToPdf(report, outputFilePath);
        }
    }
}
