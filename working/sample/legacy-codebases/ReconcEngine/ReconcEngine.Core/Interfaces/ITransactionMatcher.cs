using System.Collections.Generic;
using ReconcEngine.Models;

namespace ReconcEngine.Core.Interfaces
{
    /// <summary>
    /// Matches settlement records to transactions based on reference numbers, amounts, and dates.
    /// </summary>
    public interface ITransactionMatcher
    {
        MatchResult Match(SettlementRecord settlementRecord, IReadOnlyList<Transaction> candidateTransactions);
        bool IsExactMatch(SettlementRecord record, Transaction transaction);
        bool IsAmountWithinTolerance(decimal settlementAmount, decimal transactionAmount);
    }
}
