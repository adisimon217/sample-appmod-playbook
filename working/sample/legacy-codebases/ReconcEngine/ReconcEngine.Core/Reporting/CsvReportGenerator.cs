using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using ReconcEngine.Core.Interfaces;
using ReconcEngine.Models;

namespace ReconcEngine.Core.Reporting
{
    /// <summary>
    /// Generates CSV reconciliation reports and writes them to the configured output path.
    /// Reports are written to \\fileserver\recon-reports\ with date-stamped filenames.
    /// </summary>
    public class CsvReportGenerator : IReportGenerator
    {
        private readonly string _outputPath;

        public CsvReportGenerator(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));

            _outputPath = outputPath;
        }

        public async Task GenerateDiscrepancyReportAsync(
            IReadOnlyList<DiscrepancyReport> discrepancies, BatchRun batchRun)
        {
            var fileName = $"Discrepancies_{batchRun.StartTime:yyyyMMdd_HHmmss}_{batchRun.BatchRunId:N}.csv";
            var filePath = Path.Combine(_outputPath, fileName);

            EnsureDirectoryExists(filePath);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            };

            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, config))
            {
                csv.WriteHeader<DiscrepancyReportRow>();
                await csv.NextRecordAsync();

                foreach (var discrepancy in discrepancies)
                {
                    csv.WriteRecord(new DiscrepancyReportRow
                    {
                        DiscrepancyId = discrepancy.DiscrepancyId.ToString(),
                        Type = discrepancy.Type.ToString(),
                        SettlementRecordId = discrepancy.SettlementRecordId.ToString(),
                        TransactionId = discrepancy.TransactionId.ToString(),
                        SettlementAmount = discrepancy.SettlementAmount,
                        TransactionAmount = discrepancy.TransactionAmount,
                        DiscrepancyAmount = discrepancy.DiscrepancyAmount,
                        DetectedAt = discrepancy.DetectedAt.ToString("o"),
                        Description = discrepancy.Description
                    });
                    await csv.NextRecordAsync();
                }
            }

            await Task.CompletedTask;
        }

        public async Task GenerateSummaryReportAsync(ReconciliationResult result, BatchRun batchRun)
        {
            var fileName = $"Summary_{batchRun.StartTime:yyyyMMdd_HHmmss}_{batchRun.BatchRunId:N}.csv";
            var filePath = Path.Combine(_outputPath, fileName);

            EnsureDirectoryExists(filePath);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            };

            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, config))
            {
                csv.WriteHeader<SummaryReportRow>();
                await csv.NextRecordAsync();

                csv.WriteRecord(new SummaryReportRow
                {
                    BatchRunId = batchRun.BatchRunId.ToString(),
                    RunDate = batchRun.StartTime.ToString("yyyy-MM-dd"),
                    StartTime = batchRun.StartTime.ToString("o"),
                    EndTime = batchRun.EndTime?.ToString("o") ?? "",
                    Status = batchRun.Status.ToString(),
                    MatchedCount = result.MatchedCount,
                    DiscrepancyCount = result.DiscrepancyCount,
                    FailedCount = result.FailedCount,
                    DurationMs = (long)result.Duration.TotalMilliseconds
                });
                await csv.NextRecordAsync();
            }

            await Task.CompletedTask;
        }

        private void EnsureDirectoryExists(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private class DiscrepancyReportRow
        {
            public string DiscrepancyId { get; set; }
            public string Type { get; set; }
            public string SettlementRecordId { get; set; }
            public string TransactionId { get; set; }
            public decimal SettlementAmount { get; set; }
            public decimal TransactionAmount { get; set; }
            public decimal DiscrepancyAmount { get; set; }
            public string DetectedAt { get; set; }
            public string Description { get; set; }
        }

        private class SummaryReportRow
        {
            public string BatchRunId { get; set; }
            public string RunDate { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public string Status { get; set; }
            public int MatchedCount { get; set; }
            public int DiscrepancyCount { get; set; }
            public int FailedCount { get; set; }
            public long DurationMs { get; set; }
        }
    }
}
