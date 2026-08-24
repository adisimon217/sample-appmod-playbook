using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using ReconcEngine.Core.Interfaces;
using ReconcEngine.Models;
using ReconcEngine.Models.Enums;

namespace ReconcEngine.Core.Data
{
    /// <summary>
    /// Manages BatchRun lifecycle in ReconcDB using Dapper.
    /// Tracks each nightly reconciliation run's status, timing, and results.
    /// </summary>
    public class BatchRunRepository : IBatchRunRepository
    {
        private readonly string _connectionString;

        public BatchRunRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

            _connectionString = connectionString;
        }

        public async Task InsertBatchRunAsync(BatchRun batchRun)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync(
                    "EXEC sp_InsertBatchRun @BatchRunId, @StartTime, @Status, @CorrelationId",
                    new
                    {
                        batchRun.BatchRunId,
                        batchRun.StartTime,
                        Status = batchRun.Status.ToString(),
                        batchRun.CorrelationId
                    });
            }
        }

        public async Task UpdateBatchStatusAsync(BatchRun batchRun)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync(
                    "EXEC sp_UpdateBatchStatus @BatchRunId, @Status, @EndTime, " +
                    "@MatchedCount, @DiscrepancyCount, @FailedCount, @ErrorMessage",
                    new
                    {
                        batchRun.BatchRunId,
                        Status = batchRun.Status.ToString(),
                        batchRun.EndTime,
                        batchRun.MatchedCount,
                        batchRun.DiscrepancyCount,
                        batchRun.FailedCount,
                        batchRun.ErrorMessage
                    });
            }
        }

        public async Task<BatchRun> GetBatchRunAsync(Guid batchRunId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                return await connection.QuerySingleOrDefaultAsync<BatchRun>(
                    @"SELECT BatchRunId, StartTime, EndTime, Status, CorrelationId,
                             MatchedCount, DiscrepancyCount, FailedCount, ErrorMessage
                      FROM BatchRuns
                      WHERE BatchRunId = @BatchRunId",
                    new { BatchRunId = batchRunId });
            }
        }

        public async Task<BatchRunSummary> GetBatchRunSummaryAsync(Guid batchRunId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                return await connection.QuerySingleOrDefaultAsync<BatchRunSummary>(
                    "EXEC sp_GetBatchRunSummary @BatchRunId",
                    new { BatchRunId = batchRunId });
            }
        }
    }
}
