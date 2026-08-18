using System.Collections.Generic;
using System.Threading.Tasks;
using ReconcEngine.Models;

namespace ReconcEngine.Core.Interfaces
{
    /// <summary>
    /// Coordinates the reconciliation process: matching settlement records
    /// against transaction data and detecting discrepancies.
    /// </summary>
    public interface IReconciliationEngine
    {
        Task<ReconciliationResult> ReconcileAsync(IReadOnlyList<SettlementRecord> settlementRecords, BatchRun batchRun);
    }
}
