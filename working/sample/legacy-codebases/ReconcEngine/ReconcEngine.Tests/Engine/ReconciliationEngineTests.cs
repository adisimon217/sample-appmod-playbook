using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Serilog;
using ReconcEngine.Core.Engine;
using ReconcEngine.Core.Interfaces;
using ReconcEngine.Models;
using ReconcEngine.Models.Enums;

namespace ReconcEngine.Tests.Engine
{
    public class ReconciliationEngineTests
    {
        private readonly Mock<ITransactionMatcher> _mockMatcher;
        private readonly Mock<IDiscrepancyDetector> _mockDiscrepancyDetector;
        private readonly Mock<IReconcRepository> _mockReconcRepo;
        private readonly Mock<ITransactionRepository> _mockTransactionRepo;
        private readonly Mock<IBatchRunRepository> _mockBatchRunRepo;
        private readonly Mock<ILogger> _mockLogger;
        private readonly ReconciliationEngine _sut;

        public ReconciliationEngineTests()
        {
            _mockMatcher = new Mock<ITransactionMatcher>();
            _mockDiscrepancyDetector = new Mock<IDiscrepancyDetector>();
            _mockReconcRepo = new Mock<IReconcRepository>();
            _mockTransactionRepo = new Mock<ITransactionRepository>();
            _mockBatchRunRepo = new Mock<IBatchRunRepository>();
            _mockLogger = new Mock<ILogger>();

            _mockLogger.Setup(l => l.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .Returns(_mockLogger.Object);

            _sut = new ReconciliationEngine(
                _mockMatcher.Object,
                _mockDiscrepancyDetector.Object,
                _mockReconcRepo.Object,
                _mockTransactionRepo.Object,
                _mockBatchRunRepo.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task ReconcileAsync_AllRecordsMatch_ReturnsSuccessResult()
        {
            // Arrange
            var batchRun = CreateBatchRun();
            var settlements = CreateSettlementRecords(5);
            var transactions = CreateTransactions(5);

            _mockTransactionRepo
                .Setup(r => r.GetTransactionsByDateAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(transactions);

            _mockMatcher
                .Setup(m => m.Match(It.IsAny<SettlementRecord>(), It.IsAny<IReadOnlyList<Transaction>>()))
                .Returns((SettlementRecord s, IReadOnlyList<Transaction> _) => new MatchResult
                {
                    MatchResultId = Guid.NewGuid(),
                    SettlementRecordId = s.SettlementRecordId,
                    TransactionId = Guid.NewGuid(),
                    Status = MatchStatus.Matched,
                    SettlementAmount = s.Amount,
                    TransactionAmount = s.Amount,
                    DiscrepancyAmount = 0m,
                    MatchedOn = DateTime.UtcNow
                });

            _mockDiscrepancyDetector
                .Setup(d => d.DetectDiscrepancies(
                    It.IsAny<IReadOnlyList<MatchResult>>(),
                    It.IsAny<IReadOnlyList<SettlementRecord>>(),
                    It.IsAny<IReadOnlyList<Transaction>>()))
                .Returns(new List<DiscrepancyReport>());

            // Act
            var result = await _sut.ReconcileAsync(settlements, batchRun);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(5, result.MatchedCount);
            Assert.Equal(0, result.DiscrepancyCount);
        }

        [Fact]
        public async Task ReconcileAsync_SomeUnmatched_ReturnsCorrectCounts()
        {
            // Arrange
            var batchRun = CreateBatchRun();
            var settlements = CreateSettlementRecords(3);
            var transactions = CreateTransactions(2);

            _mockTransactionRepo
                .Setup(r => r.GetTransactionsByDateAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(transactions);

            var callCount = 0;
            _mockMatcher
                .Setup(m => m.Match(It.IsAny<SettlementRecord>(), It.IsAny<IReadOnlyList<Transaction>>()))
                .Returns((SettlementRecord s, IReadOnlyList<Transaction> _) =>
                {
                    callCount++;
                    if (callCount <= 2)
                    {
                        return new MatchResult
                        {
                            MatchResultId = Guid.NewGuid(),
                            SettlementRecordId = s.SettlementRecordId,
                            TransactionId = Guid.NewGuid(),
                            Status = MatchStatus.Matched,
                            SettlementAmount = s.Amount,
                            TransactionAmount = s.Amount,
                            DiscrepancyAmount = 0m,
                            MatchedOn = DateTime.UtcNow
                        };
                    }
                    return new MatchResult
                    {
                        MatchResultId = Guid.NewGuid(),
                        SettlementRecordId = s.SettlementRecordId,
                        TransactionId = Guid.Empty,
                        Status = MatchStatus.Unmatched,
                        SettlementAmount = s.Amount,
                        TransactionAmount = 0m,
                        DiscrepancyAmount = s.Amount,
                        MatchedOn = DateTime.UtcNow
                    };
                });

            _mockDiscrepancyDetector
                .Setup(d => d.DetectDiscrepancies(
                    It.IsAny<IReadOnlyList<MatchResult>>(),
                    It.IsAny<IReadOnlyList<SettlementRecord>>(),
                    It.IsAny<IReadOnlyList<Transaction>>()))
                .Returns(new List<DiscrepancyReport>
                {
                    new DiscrepancyReport { DiscrepancyId = Guid.NewGuid(), Type = DiscrepancyType.MissingTransaction }
                });

            // Act
            var result = await _sut.ReconcileAsync(settlements, batchRun);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, result.MatchedCount);
            Assert.Equal(1, result.DiscrepancyCount);
        }

        [Fact]
        public async Task ReconcileAsync_PersistsResultsToRepository()
        {
            // Arrange
            var batchRun = CreateBatchRun();
            var settlements = CreateSettlementRecords(2);
            var transactions = CreateTransactions(2);

            _mockTransactionRepo
                .Setup(r => r.GetTransactionsByDateAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(transactions);

            _mockMatcher
                .Setup(m => m.Match(It.IsAny<SettlementRecord>(), It.IsAny<IReadOnlyList<Transaction>>()))
                .Returns(new MatchResult
                {
                    MatchResultId = Guid.NewGuid(),
                    Status = MatchStatus.Matched,
                    TransactionId = Guid.NewGuid(),
                    MatchedOn = DateTime.UtcNow
                });

            _mockDiscrepancyDetector
                .Setup(d => d.DetectDiscrepancies(
                    It.IsAny<IReadOnlyList<MatchResult>>(),
                    It.IsAny<IReadOnlyList<SettlementRecord>>(),
                    It.IsAny<IReadOnlyList<Transaction>>()))
                .Returns(new List<DiscrepancyReport>());

            // Act
            await _sut.ReconcileAsync(settlements, batchRun);

            // Assert
            _mockReconcRepo.Verify(r => r.InsertReconcResultAsync(It.IsAny<MatchResult>()), Times.Exactly(2));
        }

        [Fact]
        public void Constructor_NullMatcher_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ReconciliationEngine(
                null, _mockDiscrepancyDetector.Object, _mockReconcRepo.Object,
                _mockTransactionRepo.Object, _mockBatchRunRepo.Object, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_NullRepository_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ReconciliationEngine(
                _mockMatcher.Object, _mockDiscrepancyDetector.Object, null,
                _mockTransactionRepo.Object, _mockBatchRunRepo.Object, _mockLogger.Object));
        }

        #region Helper Methods

        private BatchRun CreateBatchRun()
        {
            return new BatchRun
            {
                BatchRunId = Guid.NewGuid(),
                StartTime = DateTime.UtcNow,
                Status = BatchRunStatus.Running,
                CorrelationId = "test-correlation-001"
            };
        }

        private IReadOnlyList<SettlementRecord> CreateSettlementRecords(int count)
        {
            var records = new List<SettlementRecord>();
            for (int i = 0; i < count; i++)
            {
                records.Add(new SettlementRecord
                {
                    SettlementRecordId = Guid.NewGuid(),
                    ReferenceNumber = $"REF{i:D4}",
                    Amount = 100.00m + i,
                    Currency = "SGD",
                    TransactionDate = DateTime.UtcNow.Date,
                    SettlementDate = DateTime.UtcNow.Date.AddDays(1),
                    MerchantId = "MERCH001"
                });
            }
            return records;
        }

        private IReadOnlyList<Transaction> CreateTransactions(int count)
        {
            var transactions = new List<Transaction>();
            for (int i = 0; i < count; i++)
            {
                transactions.Add(new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    ReferenceNumber = $"REF{i:D4}",
                    Amount = 100.00m + i,
                    Currency = "SGD",
                    TransactionDate = DateTime.UtcNow.Date,
                    MerchantId = "MERCH001",
                    Status = "Settled"
                });
            }
            return transactions;
        }

        #endregion
    }
}
