using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Xunit;
using ReconcEngine.Core.Data;
using ReconcEngine.Models;
using ReconcEngine.Models.Enums;

namespace ReconcEngine.Tests.Data
{
    /// <summary>
    /// Integration tests for ReconcRepository.
    /// These tests require a running SQL Server instance with ReconcDB.
    /// Mark with [Trait] to allow exclusion in CI when no DB is available.
    /// </summary>
    [Trait("Category", "Integration")]
    public class ReconcRepositoryTests
    {
        // Test connection string pointing to local test database
        private const string TestConnectionString =
            @"Server=(localdb)\MSSQLLocalDB;Database=ReconcDB_Test;Integrated Security=true;Connection Timeout=30;";

        [Fact(Skip = "Requires test database - run manually")]
        public async Task InsertReconcResult_ValidResult_DoesNotThrow()
        {
            // Arrange
            var repository = new ReconcRepository(TestConnectionString);
            var result = new MatchResult
            {
                MatchResultId = Guid.NewGuid(),
                SettlementRecordId = Guid.NewGuid(),
                TransactionId = Guid.NewGuid(),
                Status = MatchStatus.Matched,
                SettlementAmount = 100.00m,
                TransactionAmount = 100.00m,
                DiscrepancyAmount = 0m,
                MatchedOn = DateTime.UtcNow,
                MatchCriteria = "ExactMatch"
            };

            // Act & Assert - should not throw
            await repository.InsertReconcResultAsync(result);
        }

        [Fact(Skip = "Requires test database - run manually")]
        public async Task InsertDiscrepancy_ValidDiscrepancy_DoesNotThrow()
        {
            // Arrange
            var repository = new ReconcRepository(TestConnectionString);
            var discrepancy = new DiscrepancyReport
            {
                DiscrepancyId = Guid.NewGuid(),
                BatchRunId = Guid.NewGuid(),
                Type = DiscrepancyType.AmountMismatch,
                SettlementRecordId = Guid.NewGuid(),
                TransactionId = Guid.NewGuid(),
                SettlementAmount = 100.50m,
                TransactionAmount = 100.00m,
                DiscrepancyAmount = 0.50m,
                DetectedAt = DateTime.UtcNow,
                Description = "Test discrepancy"
            };

            // Act & Assert
            await repository.InsertDiscrepancyAsync(discrepancy);
        }

        [Fact(Skip = "Requires test database - run manually")]
        public async Task GetDiscrepancies_ValidBatchRun_ReturnsResults()
        {
            // Arrange
            var repository = new ReconcRepository(TestConnectionString);
            var batchRunId = Guid.NewGuid();

            // Act
            var results = await repository.GetDiscrepanciesAsync(batchRunId);

            // Assert
            Assert.NotNull(results);
        }

        [Fact(Skip = "Requires test database - run manually")]
        public async Task CleanupOldRecords_ValidRetention_DoesNotThrow()
        {
            // Arrange
            var repository = new ReconcRepository(TestConnectionString);

            // Act & Assert
            await repository.CleanupOldRecordsAsync(90);
        }

        [Fact]
        public void Constructor_NullConnectionString_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new ReconcRepository(null));
        }

        [Fact]
        public void Constructor_EmptyConnectionString_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new ReconcRepository(""));
        }

        [Fact]
        public void Constructor_WhitespaceConnectionString_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new ReconcRepository("   "));
        }
    }
}
