using System;
using System.Collections.Generic;
using System.Linq;
using ReconcEngine.Core.Interfaces;
using ReconcEngine.Models;
using ReconcEngine.Models.Enums;

namespace ReconcEngine.Core.Engine
{
    /// <summary>
    /// Matches settlement records to transactions using a multi-criteria approach:
    /// 1. Exact match on reference number
    /// 2. Amount within tolerance
    /// 3. Date within acceptable range (same day or T+1)
    /// </summary>
    public class TransactionMatcher : ITransactionMatcher
    {
        private readonly decimal _toleranceAmount;
        private static readonly TimeSpan DateTolerance = TimeSpan.FromDays(1);

        public TransactionMatcher(decimal toleranceAmount)
        {
            if (toleranceAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(toleranceAmount), "Tolerance must be non-negative.");

            _toleranceAmount = toleranceAmount;
        }

        public MatchResult Match(SettlementRecord settlementRecord, IReadOnlyList<Transaction> candidateTransactions)
        {
            if (settlementRecord == null)
                throw new ArgumentNullException(nameof(settlementRecord));

            if (candidateTransactions == null || !candidateTransactions.Any())
            {
                return CreateUnmatchedResult(settlementRecord, "No candidate transactions available");
            }

            // Step 1: Try exact match by reference number
            var exactMatches = candidateTransactions
                .Where(t => IsReferenceMatch(settlementRecord.ReferenceNumber, t.ReferenceNumber))
                .ToList();

            if (exactMatches.Count == 1)
            {
                var transaction = exactMatches[0];
                return CreateMatchResult(settlementRecord, transaction);
            }

            if (exactMatches.Count > 1)
            {
                // Multiple reference matches - narrow by amount
                var amountMatch = exactMatches
                    .FirstOrDefault(t => IsAmountWithinTolerance(settlementRecord.Amount, t.Amount));

                if (amountMatch != null)
                {
                    return CreateMatchResult(settlementRecord, amountMatch);
                }
            }

            // Step 2: Fuzzy match by amount + date
            var fuzzyMatches = candidateTransactions
                .Where(t => IsAmountWithinTolerance(settlementRecord.Amount, t.Amount))
                .Where(t => IsDateWithinTolerance(settlementRecord.TransactionDate, t.TransactionDate))
                .Where(t => settlementRecord.Currency == t.Currency)
                .OrderBy(t => Math.Abs(settlementRecord.Amount - t.Amount))
                .ToList();

            if (fuzzyMatches.Count == 1)
            {
                return CreateMatchResult(settlementRecord, fuzzyMatches[0]);
            }

            if (fuzzyMatches.Count > 1)
            {
                // Take best match by closest amount
                return CreateMatchResult(settlementRecord, fuzzyMatches[0]);
            }

            return CreateUnmatchedResult(settlementRecord, "No matching transaction found");
        }

        public bool IsExactMatch(SettlementRecord record, Transaction transaction)
        {
            return IsReferenceMatch(record.ReferenceNumber, transaction.ReferenceNumber)
                && record.Amount == transaction.Amount
                && record.Currency == transaction.Currency
                && IsDateWithinTolerance(record.TransactionDate, transaction.TransactionDate);
        }

        public bool IsAmountWithinTolerance(decimal settlementAmount, decimal transactionAmount)
        {
            return Math.Abs(settlementAmount - transactionAmount) <= _toleranceAmount;
        }

        private bool IsReferenceMatch(string settlementRef, string transactionRef)
        {
            if (string.IsNullOrWhiteSpace(settlementRef) || string.IsNullOrWhiteSpace(transactionRef))
                return false;

            return string.Equals(
                settlementRef.Trim(),
                transactionRef.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        private bool IsDateWithinTolerance(DateTime settlementDate, DateTime transactionDate)
        {
            return Math.Abs((settlementDate.Date - transactionDate.Date).TotalDays) <= DateTolerance.TotalDays;
        }

        private MatchResult CreateMatchResult(SettlementRecord settlement, Transaction transaction)
        {
            var discrepancyAmount = settlement.Amount - transaction.Amount;
            var isExact = discrepancyAmount == 0m;

            return new MatchResult
            {
                MatchResultId = Guid.NewGuid(),
                SettlementRecordId = settlement.SettlementRecordId,
                TransactionId = transaction.TransactionId,
                Status = isExact ? MatchStatus.Matched : MatchStatus.MatchedWithDiscrepancy,
                SettlementAmount = settlement.Amount,
                TransactionAmount = transaction.Amount,
                DiscrepancyAmount = discrepancyAmount,
                MatchedOn = DateTime.UtcNow,
                MatchCriteria = isExact ? "ExactMatch" : "FuzzyMatch-AmountTolerance"
            };
        }

        private MatchResult CreateUnmatchedResult(SettlementRecord settlement, string reason)
        {
            return new MatchResult
            {
                MatchResultId = Guid.NewGuid(),
                SettlementRecordId = settlement.SettlementRecordId,
                TransactionId = Guid.Empty,
                Status = MatchStatus.Unmatched,
                SettlementAmount = settlement.Amount,
                TransactionAmount = 0m,
                DiscrepancyAmount = settlement.Amount,
                MatchedOn = DateTime.UtcNow,
                MatchCriteria = reason
            };
        }
    }
}
