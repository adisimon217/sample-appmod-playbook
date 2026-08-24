using System;
using System.Collections.Generic;

namespace MerchantHub.Core.Models
{
    /// <summary>
    /// Report configuration and metadata
    /// </summary>
    public class ReportConfig
    {
        public int ReportId { get; set; }
        public string ReportType { get; set; }
        public string ReportName { get; set; }
        public string Description { get; set; }
        public string TemplateFile { get; set; } // .rpt file name
        public bool RequiresDateRange { get; set; }
        public bool RequiresMerchantId { get; set; }
        public List<string> AvailableFormats { get; set; }
        public string Category { get; set; } // Financial, Compliance, Activity
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Generated report record
    /// </summary>
    public class GeneratedReport
    {
        public int GeneratedReportId { get; set; }
        public int MerchantId { get; set; }
        public string ReportType { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string Format { get; set; }
        public DateTime GeneratedDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string GeneratedBy { get; set; }
        public string Status { get; set; } // Completed, Failed, InProgress
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Monthly statement data
    /// </summary>
    public class MonthlyStatement
    {
        public int StatementId { get; set; }
        public int MerchantId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public DateTime StatementDate { get; set; }
        public decimal TotalVolume { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalFees { get; set; }
        public decimal NetSettlement { get; set; }
        public decimal ChargebackAmount { get; set; }
        public int ChargebackCount { get; set; }
        public decimal RefundAmount { get; set; }
        public int RefundCount { get; set; }
        public string FilePath { get; set; }
        public DateTime GeneratedDate { get; set; }
    }

    /// <summary>
    /// Result of report generation
    /// </summary>
    public class ReportGenerationResult
    {
        public bool Success { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string ErrorMessage { get; set; }
    }
}
