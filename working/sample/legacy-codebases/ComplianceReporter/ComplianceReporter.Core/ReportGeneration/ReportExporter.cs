using System;
using System.Data;
using System.IO;
using Microsoft.Reporting.WinForms;

namespace ComplianceReporter.Core.ReportGeneration
{
    /// <summary>
    /// Renders SSRS local reports (.rdlc) to PDF format.
    /// Uses Microsoft.ReportViewer.WinForms for local report processing.
    /// 
    /// PDF output settings are configured for A4 paper (MAS requirement)
    /// with specific margin and formatting requirements per MAS Notice PSN02.
    /// </summary>
    public class ReportExporter
    {
        // Device info for PDF rendering - A4 paper per MAS specifications
        private const string DEVICE_INFO =
            "<DeviceInfo>" +
            "  <OutputFormat>PDF</OutputFormat>" +
            "  <PageWidth>21cm</PageWidth>" +
            "  <PageHeight>29.7cm</PageHeight>" +
            "  <MarginTop>2cm</MarginTop>" +
            "  <MarginLeft>2cm</MarginLeft>" +
            "  <MarginRight>2cm</MarginRight>" +
            "  <MarginBottom>2cm</MarginBottom>" +
            "  <EmbedFonts>True</EmbedFonts>" +
            "</DeviceInfo>";

        /// <summary>
        /// Renders an already-configured LocalReport to PDF.
        /// </summary>
        public void RenderReportToPdf(LocalReport report, string outputFilePath)
        {
            Warning[] warnings;
            string[] streamIds;
            string mimeType;
            string encoding;
            string fileExtension;

            byte[] renderedBytes = report.Render(
                "PDF",
                DEVICE_INFO,
                out mimeType,
                out encoding,
                out fileExtension,
                out streamIds,
                out warnings);

            if (renderedBytes == null || renderedBytes.Length == 0)
            {
                throw new Exception("Report rendering returned empty result for: " + outputFilePath);
            }

            // Log any warnings from the report engine
            if (warnings != null && warnings.Length > 0)
            {
                foreach (Warning warning in warnings)
                {
                    EventLogger.WriteWarning(string.Format(
                        "Report rendering warning [{0}]: {1}",
                        warning.Severity, warning.Message));
                }
            }

            // Write PDF to output file
            string outputDir = Path.GetDirectoryName(outputFilePath);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            using (FileStream fs = new FileStream(outputFilePath, FileMode.Create))
            {
                fs.Write(renderedBytes, 0, renderedBytes.Length);
            }

            EventLogger.WriteInfo(string.Format("Report exported to PDF: {0} ({1} bytes)",
                outputFilePath, renderedBytes.Length));
        }

        /// <summary>
        /// Exports a DataSet to PDF using an .rdlc report definition file.
        /// Used for the annual audit report which has a more complex template.
        /// </summary>
        public void ExportToPdf(string rdlcPath, DataSet reportData, string outputFilePath)
        {
            if (!File.Exists(rdlcPath))
            {
                throw new FileNotFoundException("RDLC template not found: " + rdlcPath);
            }

            LocalReport report = new LocalReport();
            report.ReportPath = rdlcPath;

            // Bind all DataTables in the DataSet as data sources
            foreach (DataTable table in reportData.Tables)
            {
                string dataSourceName = table.TableName + "DataSet";
                ReportDataSource dataSource = new ReportDataSource(dataSourceName, table);
                report.DataSources.Add(dataSource);
            }

            // Set common parameters if metadata is available
            if (reportData.Tables.Contains("ReportMetadata") &&
                reportData.Tables["ReportMetadata"].Rows.Count > 0)
            {
                DataRow metadata = reportData.Tables["ReportMetadata"].Rows[0];

                try
                {
                    report.SetParameters(new ReportParameter[]
                    {
                        new ReportParameter("InstitutionCode", metadata["InstitutionCode"].ToString()),
                        new ReportParameter("ReportingEntity", metadata["ReportingEntity"].ToString()),
                        new ReportParameter("GeneratedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                        new ReportParameter("ReportVersion", "1.3.2")
                    });
                }
                catch (Exception ex)
                {
                    // Some reports may not have all parameters defined - non-fatal
                    EventLogger.WriteWarning("Could not set all report parameters: " + ex.Message);
                }
            }

            RenderReportToPdf(report, outputFilePath);
        }
    }
}
