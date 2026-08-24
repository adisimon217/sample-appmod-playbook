using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using ReconcEngine.Core.Interfaces;
using ReconcEngine.Models;
using ReconcEngine.Models.Enums;

namespace ReconcEngine.Core.Engine
{
    /// <summary>
    /// Core reconciliation engine that coordinates matching and discrepancy detection.
    /// </summary>
    public class ReconciliationEngine : IReconciliationEngine
    {
        private readonly ITransactionMatcher _matcher;
        private readonly IDiscrepancyDetector _discrepancyDetector;
        private readonly IReconcRepository _reconcRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IBatchRunRepository _batchRunRepository;
        private readonly ILogger _logger;

        public ReconciliationEngine(
            ITransactionMatcher matcher,
            IDiscrepancyDetector discrepancyDetector,
            IReconcRepository reconcRepository,
            ITransactionRepository transactionRepository,
            IBatchRunRepository batchRunRepository,
            ILogger logger)
        {
            _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
            _discrepancyDetector = discrepancyDetector ?? throw new ArgumentNullException(nameof(discrepancyDetector));
            _reconcRepository = reconcRepository ?? throw new ArgumentNullException(nameof(reconcRepository));
            _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
            _batchRunRepository = batchRunRepository ?? throw new ArgumentNullException(nameof(batchRunRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ReconciliationResult> ReconcileAsync(
            IReadOnlyList<SettlementRecord> settlementRecords, BatchRun batchRun)
        {
            _logger.Information("Starting reconciliation for batch {BatchRunId} with {RecordCount} settlement records",
                batchRun.BatchRunId, settlementRecords.Count);

            // Determine the date range from settlement records
            var minDate = settlementRecords.Min(r => r.TransactionDate).Date;
            var maxDate = settlementRecords.Max(r => r.TransactionDate).Date.AddDays(1);

            // Fetch transactions from PayGateDB for the relevant date range
            var transactions = await _transactionRepository.GetTransactionsByDateAsync(minDate);
            _logger.Information("Retrieved {TransactionCount} transactions from PayGateDB for date range {MinDate} to {MaxDate}",
                transactions.Count, minDate, maxDate);

            // Perform matching
            var matchResults = new List<MatchResult>();
            var matchedTransactionIds = new HashSet<Guid>();
            var unmatchedSettlements = new List<SettlementRecord>();

            foreach (var settlement in settlementRecords)
            {
                var candidateTransactions = transactions
                    .Where(t => !matchedTransactionIds.Contains(t.TransactionId))
                    .ToList();

                var matchResult = _matcher.Match(settlement, candidateTransactions);

                if (matchResult.Status == MatchStatus.Matched || matchResult.Status == MatchStatus.MatchedWithDiscrepancy)
                {
                    matchedTransactionIds.Add(matchResult.TransactionId);
                    matchResults.Add(matchResult);
                    await _reconcRepository.InsertReconcResultAsync(matchResult);
                }
                else
                {
                    unmatchedSettlements.Add(settlement);
                    matchResults.Add(matchResult);
                    await _reconcRepository.InsertReconcResultAsync(matchResult);
                }
            }

            // Identify unmatched transactions
            var unmatchedTransactions = transactions
                .Where(t => !matchedTransactionIds.Contains(t.TransactionId))
                .ToList();

            _logger.Information("Matching complete. Matched: {Matched}, Unmatched settlements: {UnmatchedSettlements}, " +
                "Unmatched transactions: {UnmatchedTransactions}",
                matchResults.Count(r => r.Status == MatchStatus.Matched || r.Status == MatchStatus.MatchedWithDiscrepancy),
                unmatchedSettlements.Count, unmatchedTransactions.Count);

            // Detect discrepancies
            var discrepancies = _discrepancyDetector.DetectDiscrepancies(
                matchResults, unmatchedSettlements, unmatchedTransactions);

            foreach (var discrepancy in discrepancies)
            {
                discrepancy.BatchRunId = batchRun.BatchRunId;
                await _reconcRepository.InsertDiscrepancyAsync(discrepancy);
            }

            var result = new ReconciliationResult
            {
                BatchRunId = batchRun.BatchRunId,
                Success = true,
                MatchedCount = matchResults.Count(r => r.Status == MatchStatus.Matched),
                DiscrepancyCount = discrepancies.Count,
                FailedCount = unmatchedSettlements.Count + unmatchedTransactions.Count,
                Duration = TimeSpan.Zero, // Will be set by orchestrator
                MatchResults = matchResults,
                Discrepancies = discrepancies.ToList(),
                UnmatchedSettlements = unmatchedSettlements,
                UnmatchedTransactions = unmatchedTransactions
            };

            _logger.Information("Reconciliation engine completed for batch {BatchRunId}", batchRun.BatchRunId);
            return result;
        }
    }
}
