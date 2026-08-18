using System;
using System.Collections.Generic;
using System.Linq;
using ReconcEngine.Core.Interfaces;
using ReconcEngine.Models;
using ReconcEngine.Models.Enums;

namespace ReconcEngine.Core.Analysis
{
    /// <summary>
    /// Analyzes reconciliation results to categorize and report discrepancies.
    /// Identifies amount mismatches, missing transactions, duplicate settlements,
    /// and other reconciliation issues.
    /// </summary>
    public class DiscrepancyDetector : IDiscrepancyDetector
    {
        private readonly decimal _toleranceAmount;

        public DiscrepancyDetector(decimal toleranceAmount)
        {
            if (toleranceAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(toleranceAmount), "Tolerance must be non-negative.");

            _toleranceAmount = toleranceAmount;
        }

        public IReadOnlyList<DiscrepancyReport> DetectDiscrepancies(
            IReadOnlyList<MatchResult> matchResults,
            IReadOnlyList<SettlementRecord> unmatchedSettlements,
            IReadOnlyList<Transaction> unmatchedTransactions)
        {
            var discrepancies = new List<DiscrepancyReport>();

            // Amount discrepancies from matched records
            discrepancies.AddRange(DetectAmountDiscrepancies(matchResults));

            // Missing transaction discrepancies (settlement exists but no transaction)
            discrepancies.AddRange(DetectMissingTransactions(unmatchedSettlements));

            // Missing settlement discrepancies (transaction exists but no settlement)
            discrepancies.AddRange(DetectMissingSettlements(unmatchedTransactions));

            // Duplicate detection
            discrepancies.AddRange(DetectDuplicateSettlements(matchResults));

            return discrepancies.OrderByDescending(d => Math.Abs(d.DiscrepancyAmount)).ToList();
        }

        private IEnumerable<DiscrepancyReport> DetectAmountDiscrepancies(IReadOnlyList<MatchResult> matchResults)
        {
            return matchResults
                .Where(r => r.Status == MatchStatus.MatchedWithDiscrepancy)
                .Where(r => Math.Abs(r.DiscrepancyAmount) > _toleranceAmount)
                .Select(r => new DiscrepancyReport
                {
                    DiscrepancyId = Guid.NewGuid(),
                    Type = DiscrepancyType.AmountMismatch,
                    SettlementRecordId = r.SettlementRecordId,
                    TransactionId = r.TransactionId,
                    SettlementAmount = r.SettlementAmount,
                    TransactionAmount = r.TransactionAmount,
                    DiscrepancyAmount = r.DiscrepancyAmount,
                    DetectedAt = DateTime.UtcNow,
                    Description = $"Amount mismatch: Settlement {r.SettlementAmount:C} vs Transaction {r.TransactionAmount:C} " +
                                  $"(difference: {r.DiscrepancyAmount:C})"
                });
        }

        private IEnumerable<DiscrepancyReport> DetectMissingTransactions(
            IReadOnlyList<SettlementRecord> unmatchedSettlements)
        {
            return unmatchedSettlements.Select(s => new DiscrepancyReport
            {
                DiscrepancyId = Guid.NewGuid(),
                Type = DiscrepancyType.MissingTransaction,
                SettlementRecordId = s.SettlementRecordId,
                TransactionId = Guid.Empty,
                SettlementAmount = s.Amount,
                TransactionAmount = 0m,
                DiscrepancyAmount = s.Amount,
                DetectedAt = DateTime.UtcNow,
                Description = $"Settlement record '{s.ReferenceNumber}' has no matching transaction. " +
                              $"Amount: {s.Amount:C} {s.Currency}, Date: {s.TransactionDate:yyyy-MM-dd}"
            });
        }

        private IEnumerable<DiscrepancyReport> DetectMissingSettlements(
            IReadOnlyList<Transaction> unmatchedTransactions)
        {
            return unmatchedTransactions.Select(t => new DiscrepancyReport
            {
                DiscrepancyId = Guid.NewGuid(),
                Type = DiscrepancyType.MissingSettlement,
                SettlementRecordId = Guid.Empty,
                TransactionId = t.TransactionId,
                SettlementAmount = 0m,
                TransactionAmount = t.Amount,
                DiscrepancyAmount = -t.Amount,
                DetectedAt = DateTime.UtcNow,
                Description = $"Transaction '{t.ReferenceNumber}' has no matching settlement record. " +
                              $"Amount: {t.Amount:C} {t.Currency}, Date: {t.TransactionDate:yyyy-MM-dd}"
            });
        }

        private IEnumerable<DiscrepancyReport> DetectDuplicateSettlements(IReadOnlyList<MatchResult> matchResults)
        {
            // Group by transaction ID to find duplicates
            var duplicates = matchResults
                .Where(r => r.TransactionId != Guid.Empty)
                .GroupBy(r => r.TransactionId)
                .Where(g => g.Count() > 1);

            foreach (var group in duplicates)
            {
                var items = group.ToList();
                var totalSettlement = items.Sum(i => i.SettlementAmount);

                yield return new DiscrepancyReport
                {
                    DiscrepancyId = Guid.NewGuid(),
                    Type = DiscrepancyType.DuplicateSettlement,
                    SettlementRecordId = items.First().SettlementRecordId,
                    TransactionId = group.Key,
                    SettlementAmount = totalSettlement,
                    TransactionAmount = items.First().TransactionAmount,
                    DiscrepancyAmount = totalSettlement - items.First().TransactionAmount,
                    DetectedAt = DateTime.UtcNow,
                    Description = $"Transaction {group.Key} matched to {items.Count} settlement records. " +
                                  $"Possible duplicate settlement. Total: {totalSettlement:C}"
                };
            }
        }
    }
}
