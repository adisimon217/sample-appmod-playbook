using System.Collections.Generic;
using ReconcEngine.Models;

namespace ReconcEngine.Core.Interfaces
{
    /// <summary>
    /// Analyzes matched records to detect amount discrepancies, missing transactions,
    /// duplicate settlements, and other reconciliation issues.
    /// </summary>
    public interface IDiscrepancyDetector
    {
        IReadOnlyList<DiscrepancyReport> DetectDiscrepancies(
            IReadOnlyList<MatchResult> matchResults,
            IReadOnlyList<SettlementRecord> unmatchedSettlements,
            IReadOnlyList<Transaction> unmatchedTransactions);
    }
}
