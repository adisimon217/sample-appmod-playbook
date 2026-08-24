using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ReconcEngine.Core.Interfaces;
using ReconcEngine.Models;
using ReconcEngine.Models.Enums;

namespace ReconcEngine.Core.Data
{
    /// <summary>
    /// Data access for the ReconcDB database using Dapper.
    /// All queries use parameterized SQL via stored procedures.
    /// </summary>
    public class ReconcRepository : IReconcRepository
    {
        private readonly string _connectionString;

        public ReconcRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

            _connectionString = connectionString;
        }

        public async Task InsertReconcResultAsync(MatchResult result)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync(
                    "EXEC sp_InsertReconcResult @MatchResultId, @SettlementRecordId, @TransactionId, " +
                    "@Status, @SettlementAmount, @TransactionAmount, @DiscrepancyAmount, @MatchedOn, @MatchCriteria",
                    new
                    {
                        result.MatchResultId,
                        result.SettlementRecordId,
                        result.TransactionId,
                        Status = result.Status.ToString(),
                        result.SettlementAmount,
                        result.TransactionAmount,
                        result.DiscrepancyAmount,
                        result.MatchedOn,
                        result.MatchCriteria
                    });
            }
        }

        public async Task InsertDiscrepancyAsync(DiscrepancyReport discrepancy)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync(
                    "EXEC sp_InsertDiscrepancy @DiscrepancyId, @BatchRunId, @Type, " +
                    "@SettlementRecordId, @TransactionId, @SettlementAmount, @TransactionAmount, " +
                    "@DiscrepancyAmount, @DetectedAt, @Description",
                    new
                    {
                        discrepancy.DiscrepancyId,
                        discrepancy.BatchRunId,
                        Type = discrepancy.Type.ToString(),
                        discrepancy.SettlementRecordId,
                        discrepancy.TransactionId,
                        discrepancy.SettlementAmount,
                        discrepancy.TransactionAmount,
                        discrepancy.DiscrepancyAmount,
                        discrepancy.DetectedAt,
                        discrepancy.Description
                    });
            }
        }

        public async Task<IReadOnlyList<DiscrepancyReport>> GetDiscrepanciesAsync(Guid batchRunId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var results = await connection.QueryAsync<DiscrepancyReport>(
                    "EXEC sp_GetDiscrepancies @BatchRunId",
                    new { BatchRunId = batchRunId });

                return results.ToList();
            }
        }

        public async Task CleanupOldRecordsAsync(int retentionDays)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync(
                    "EXEC sp_CleanupOldRecords @RetentionDays",
                    new { RetentionDays = retentionDays });
            }
        }
    }
}
