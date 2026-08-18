using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ReconcEngine.Core.Analysis;
using ReconcEngine.Models;
using ReconcEngine.Models.Enums;

namespace ReconcEngine.Tests.Engine
{
    public class DiscrepancyDetectorTests
    {
        private readonly DiscrepancyDetector _sut;
        private const decimal DefaultTolerance = 0.01m;

        public DiscrepancyDetectorTests()
        {
            _sut = new DiscrepancyDetector(DefaultTolerance);
        }

        [Fact]
        public void DetectDiscrepancies_AmountMismatch_ReturnsAmountDiscrepancy()
        {
            // Arrange
            var matchResults = new List<MatchResult>
            {
                new MatchResult
                {
                    MatchResultId = Guid.NewGuid(),
                    SettlementRecordId = Guid.NewGuid(),
                    TransactionId = Guid.NewGuid(),
                    Status = MatchStatus.MatchedWithDiscrepancy,
                    SettlementAmount = 100.50m,
                    TransactionAmount = 100.00m,
                    DiscrepancyAmount = 0.50m,
                    MatchedOn = DateTime.UtcNow
                }
            };

            // Act
            var discrepancies = _sut.DetectDiscrepancies(matchResults, new List<SettlementRecord>(), new List<Transaction>());

            // Assert
            Assert.Single(discrepancies);
            Assert.Equal(DiscrepancyType.AmountMismatch, discrepancies[0].Type);
            Assert.Equal(0.50m, discrepancies[0].DiscrepancyAmount);
        }

        [Fact]
        public void DetectDiscrepancies_UnmatchedSettlement_ReturnsMissingTransaction()
        {
            // Arrange
            var unmatchedSettlements = new List<SettlementRecord>
            {
                new SettlementRecord
                {
                    SettlementRecordId = Guid.NewGuid(),
                    ReferenceNumber = "REF001",
                    Amount = 250.00m,
                    Currency = "SGD",
                    TransactionDate = DateTime.UtcNow.Date
                }
            };

            // Act
            var discrepancies = _sut.DetectDiscrepancies(
                new List<MatchResult>(), unmatchedSettlements, new List<Transaction>());

            // Assert
            Assert.Single(discrepancies);
            Assert.Equal(DiscrepancyType.MissingTransaction, discrepancies[0].Type);
            Assert.Equal(250.00m, discrepancies[0].DiscrepancyAmount);
        }

        [Fact]
        public void DetectDiscrepancies_UnmatchedTransaction_ReturnsMissingSettlement()
        {
            // Arrange
            var unmatchedTransactions = new List<Transaction>
            {
                new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    ReferenceNumber = "TXN001",
                    Amount = 175.00m,
                    Currency = "SGD",
                    TransactionDate = DateTime.UtcNow.Date
                }
            };

            // Act
            var discrepancies = _sut.DetectDiscrepancies(
                new List<MatchResult>(), new List<SettlementRecord>(), unmatchedTransactions);

            // Assert
            Assert.Single(discrepancies);
            Assert.Equal(DiscrepancyType.MissingSettlement, discrepancies[0].Type);
            Assert.Equal(-175.00m, discrepancies[0].DiscrepancyAmount);
        }

        [Fact]
        public void DetectDiscrepancies_DuplicateSettlement_ReturnsDuplicateDiscrepancy()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var matchResults = new List<MatchResult>
            {
                new MatchResult
                {
                    MatchResultId = Guid.NewGuid(),
                    SettlementRecordId = Guid.NewGuid(),
                    TransactionId = transactionId,
                    Status = MatchStatus.Matched,
                    SettlementAmount = 100.00m,
                    TransactionAmount = 100.00m,
                    DiscrepancyAmount = 0m,
                    MatchedOn = DateTime.UtcNow
                },
                new MatchResult
                {
                    MatchResultId = Guid.NewGuid(),
                    SettlementRecordId = Guid.NewGuid(),
                    TransactionId = transactionId,
                    Status = MatchStatus.Matched,
                    SettlementAmount = 100.00m,
                    TransactionAmount = 100.00m,
                    DiscrepancyAmount = 0m,
                    MatchedOn = DateTime.UtcNow
                }
            };

            // Act
            var discrepancies = _sut.DetectDiscrepancies(
                matchResults, new List<SettlementRecord>(), new List<Transaction>());

            // Assert
            Assert.Contains(discrepancies, d => d.Type == DiscrepancyType.DuplicateSettlement);
        }

        [Fact]
        public void DetectDiscrepancies_NoIssues_ReturnsEmptyList()
        {
            // Arrange
            var matchResults = new List<MatchResult>
            {
                new MatchResult
                {
                    MatchResultId = Guid.NewGuid(),
                    SettlementRecordId = Guid.NewGuid(),
                    TransactionId = Guid.NewGuid(),
                    Status = MatchStatus.Matched,
                    SettlementAmount = 100.00m,
                    TransactionAmount = 100.00m,
                    DiscrepancyAmount = 0m,
                    MatchedOn = DateTime.UtcNow
                }
            };

            // Act
            var discrepancies = _sut.DetectDiscrepancies(
                matchResults, new List<SettlementRecord>(), new List<Transaction>());

            // Assert
            Assert.Empty(discrepancies);
        }

        [Fact]
        public void DetectDiscrepancies_OrderedByAbsoluteAmount()
        {
            // Arrange
            var unmatchedSettlements = new List<SettlementRecord>
            {
                new SettlementRecord { SettlementRecordId = Guid.NewGuid(), ReferenceNumber = "A", Amount = 50.00m, Currency = "SGD", TransactionDate = DateTime.UtcNow },
                new SettlementRecord { SettlementRecordId = Guid.NewGuid(), ReferenceNumber = "B", Amount = 500.00m, Currency = "SGD", TransactionDate = DateTime.UtcNow },
                new SettlementRecord { SettlementRecordId = Guid.NewGuid(), ReferenceNumber = "C", Amount = 100.00m, Currency = "SGD", TransactionDate = DateTime.UtcNow }
            };

            // Act
            var discrepancies = _sut.DetectDiscrepancies(
                new List<MatchResult>(), unmatchedSettlements, new List<Transaction>());

            // Assert
            Assert.Equal(3, discrepancies.Count);
            Assert.True(Math.Abs(discrepancies[0].DiscrepancyAmount) >= Math.Abs(discrepancies[1].DiscrepancyAmount));
            Assert.True(Math.Abs(discrepancies[1].DiscrepancyAmount) >= Math.Abs(discrepancies[2].DiscrepancyAmount));
        }

        [Theory]
        [InlineData(-0.01)]
        [InlineData(-5.00)]
        public void Constructor_NegativeTolerance_ThrowsArgumentOutOfRange(decimal tolerance)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DiscrepancyDetector(tolerance));
        }
    }
}
