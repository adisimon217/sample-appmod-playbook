using System.Collections.Generic;
using System.Threading.Tasks;
using ReconcEngine.Models;

namespace ReconcEngine.Core.Interfaces
{
    /// <summary>
    /// Generates reconciliation reports (CSV format) and writes them to the report output directory.
    /// </summary>
    public interface IReportGenerator
    {
        Task GenerateDiscrepancyReportAsync(IReadOnlyList<DiscrepancyReport> discrepancies, BatchRun batchRun);
        Task GenerateSummaryReportAsync(ReconciliationResult result, BatchRun batchRun);
    }

    /// <summary>
    /// Repository for batch run persistence.
    /// </summary>
    public interface IBatchRunRepository
    {
        Task InsertBatchRunAsync(BatchRun batchRun);
        Task UpdateBatchStatusAsync(BatchRun batchRun);
        Task<BatchRun> GetBatchRunAsync(System.Guid batchRunId);
        Task<BatchRunSummary> GetBatchRunSummaryAsync(System.Guid batchRunId);
    }

    /// <summary>
    /// Repository for reconciliation data (ReconcDB).
    /// </summary>
    public interface IReconcRepository
    {
        Task InsertReconcResultAsync(MatchResult result);
        Task InsertDiscrepancyAsync(DiscrepancyReport discrepancy);
        Task<IReadOnlyList<DiscrepancyReport>> GetDiscrepanciesAsync(System.Guid batchRunId);
        Task CleanupOldRecordsAsync(int retentionDays);
    }

    /// <summary>
    /// Repository for reading transaction data from PayGateDB (read-only).
    /// </summary>
    public interface ITransactionRepository
    {
        Task<IReadOnlyList<Transaction>> GetTransactionsByDateAsync(System.DateTime date);
        Task<IReadOnlyList<Transaction>> GetTransactionsByReferenceAsync(string referenceNumber);
        Task<IReadOnlyList<Transaction>> GetUnmatchedTransactionsAsync(System.DateTime fromDate, System.DateTime toDate);
    }

    /// <summary>
    /// PGP decryption service for encrypted settlement files.
    /// </summary>
    public interface IPgpDecryptor
    {
        System.IO.Stream Decrypt(string encryptedFilePath);
        bool IsEncrypted(string filePath);
    }

    /// <summary>
    /// Detects file format (CSV variants, encoding) for settlement files.
    /// </summary>
    public interface IFileFormatDetector
    {
        FileFormat DetectFormat(string filePath);
        System.Text.Encoding DetectEncoding(string filePath);
    }

    public enum FileFormat
    {
        StandardCsv,
        SemicolonDelimited,
        TabDelimited,
        FixedWidth,
        Unknown
    }
}
