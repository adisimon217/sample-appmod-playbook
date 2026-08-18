using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using ReconcEngine.Core.Interfaces;
using ReconcEngine.Models;
using ReconcEngine.Models.Enums;

namespace ReconcEngine.Console
{
    /// <summary>
    /// Coordinates the full reconciliation run: file discovery, parsing, matching, and reporting.
    /// </summary>
    public class ReconciliationOrchestrator
    {
        private readonly IReconciliationEngine _engine;
        private readonly ISettlementFileParser _fileParser;
        private readonly IReportGenerator _reportGenerator;
        private readonly IBatchRunRepository _batchRunRepository;
        private readonly string _settlementFilePath;
        private readonly int _batchSizeLimit;
        private readonly ILogger _logger;

        public ReconciliationOrchestrator(
            IReconciliationEngine engine,
            ISettlementFileParser fileParser,
            IReportGenerator reportGenerator,
            IBatchRunRepository batchRunRepository,
            string settlementFilePath,
            int batchSizeLimit,
            ILogger logger)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _fileParser = fileParser ?? throw new ArgumentNullException(nameof(fileParser));
            _reportGenerator = reportGenerator ?? throw new ArgumentNullException(nameof(reportGenerator));
            _batchRunRepository = batchRunRepository ?? throw new ArgumentNullException(nameof(batchRunRepository));
            _settlementFilePath = settlementFilePath ?? throw new ArgumentNullException(nameof(settlementFilePath));
            _batchSizeLimit = batchSizeLimit;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ReconciliationResult> RunAsync()
        {
            var correlationId = Guid.NewGuid().ToString("N").Substring(0, 12);
            var contextLogger = _logger.ForContext("CorrelationId", correlationId);

            contextLogger.Information("Starting reconciliation run {CorrelationId}", correlationId);

            var batchRun = new BatchRun
            {
                BatchRunId = Guid.NewGuid(),
                StartTime = DateTime.UtcNow,
                Status = BatchRunStatus.Running,
                CorrelationId = correlationId
            };

            await _batchRunRepository.InsertBatchRunAsync(batchRun);

            try
            {
                // Step 1: Discover settlement files
                var settlementFiles = DiscoverSettlementFiles();
                if (!settlementFiles.Any())
                {
                    contextLogger.Warning("No settlement files found in {Path}", _settlementFilePath);
                    return await CompleteBatchRun(batchRun, BatchRunStatus.CompletedNoData, 0, 0, 0);
                }

                contextLogger.Information("Found {FileCount} settlement files to process", settlementFiles.Count);

                // Step 2: Parse settlement files
                var allRecords = new List<SettlementRecord>();
                foreach (var file in settlementFiles)
                {
                    contextLogger.Debug("Parsing settlement file {FileName}", Path.GetFileName(file));
                    var records = await _fileParser.ParseFileAsync(file);
                    allRecords.AddRange(records);
                }

                contextLogger.Information("Parsed {RecordCount} settlement records from {FileCount} files",
                    allRecords.Count, settlementFiles.Count);

                if (allRecords.Count > _batchSizeLimit)
                {
                    contextLogger.Warning("Record count {Count} exceeds batch limit {Limit}. Processing in batches.",
                        allRecords.Count, _batchSizeLimit);
                }

                // Step 3: Run reconciliation engine
                var reconcResult = await _engine.ReconcileAsync(allRecords, batchRun);

                // Step 4: Generate reports
                if (reconcResult.Discrepancies.Any())
                {
                    contextLogger.Information("Generating discrepancy report with {Count} items",
                        reconcResult.Discrepancies.Count);
                    await _reportGenerator.GenerateDiscrepancyReportAsync(reconcResult.Discrepancies, batchRun);
                }

                await _reportGenerator.GenerateSummaryReportAsync(reconcResult, batchRun);

                // Step 5: Complete batch run
                return await CompleteBatchRun(batchRun, BatchRunStatus.Completed,
                    reconcResult.MatchedCount, reconcResult.DiscrepancyCount, reconcResult.FailedCount);
            }
            catch (Exception ex)
            {
                contextLogger.Error(ex, "Reconciliation run {CorrelationId} failed", correlationId);
                batchRun.Status = BatchRunStatus.Failed;
                batchRun.EndTime = DateTime.UtcNow;
                batchRun.ErrorMessage = ex.Message;
                await _batchRunRepository.UpdateBatchStatusAsync(batchRun);

                return new ReconciliationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Duration = DateTime.UtcNow - batchRun.StartTime
                };
            }
        }

        private List<string> DiscoverSettlementFiles()
        {
            if (!Directory.Exists(_settlementFilePath))
            {
                _logger.Warning("Settlement directory does not exist: {Path}", _settlementFilePath);
                return new List<string>();
            }

            var today = DateTime.Today;
            var files = Directory.GetFiles(_settlementFilePath, "*.pgp")
                .Concat(Directory.GetFiles(_settlementFilePath, "*.csv"))
                .Where(f => File.GetLastWriteTime(f).Date == today)
                .OrderBy(f => f)
                .ToList();

            return files;
        }

        private async Task<ReconciliationResult> CompleteBatchRun(
            BatchRun batchRun, BatchRunStatus status,
            int matchedCount, int discrepancyCount, int failedCount)
        {
            batchRun.Status = status;
            batchRun.EndTime = DateTime.UtcNow;
            batchRun.MatchedCount = matchedCount;
            batchRun.DiscrepancyCount = discrepancyCount;
            batchRun.FailedCount = failedCount;

            await _batchRunRepository.UpdateBatchStatusAsync(batchRun);

            return new ReconciliationResult
            {
                Success = status == BatchRunStatus.Completed || status == BatchRunStatus.CompletedNoData,
                MatchedCount = matchedCount,
                DiscrepancyCount = discrepancyCount,
                FailedCount = failedCount,
                Duration = batchRun.EndTime.Value - batchRun.StartTime
            };
        }
    }
}
