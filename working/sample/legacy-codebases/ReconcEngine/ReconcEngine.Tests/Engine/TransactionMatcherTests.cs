using System;
using System.Collections.Generic;
using Xunit;
using ReconcEngine.Core.Engine;
using ReconcEngine.Models;
using ReconcEngine.Models.Enums;

namespace ReconcEngine.Tests.Engine
{
    public class TransactionMatcherTests
    {
        private readonly TransactionMatcher _sut;
        private const decimal DefaultTolerance = 0.01m;

        public TransactionMatcherTests()
        {
            _sut = new TransactionMatcher(DefaultTolerance);
        }

        [Fact]
        public void Match_ExactReferenceAndAmount_ReturnsMatched()
        {
            // Arrange
            var settlement = CreateSettlementRecord("REF001", 100.00m, "SGD");
            var transactions = new List<Transaction>
            {
                CreateTransaction("REF001", 100.00m, "SGD")
            };

            // Act
            var result = _sut.Match(settlement, transactions);

            // Assert
            Assert.Equal(MatchStatus.Matched, result.Status);
            Assert.Equal(0m, result.DiscrepancyAmount);
            Assert.Equal("ExactMatch", result.MatchCriteria);
        }

        [Fact]
        public void Match_AmountWithinTolerance_ReturnsMatchedWithDiscrepancy()
        {
            // Arrange
            var settlement = CreateSettlementRecord("REF002", 100.005m, "SGD");
            var transactions = new List<Transaction>
            {
                CreateTransaction("REF002", 100.00m, "SGD")
            };

            // Act
            var result = _sut.Match(settlement, transactions);

            // Assert
            Assert.Equal(MatchStatus.MatchedWithDiscrepancy, result.Status);
            Assert.Equal(0.005m, result.DiscrepancyAmount);
        }

        [Fact]
        public void Match_AmountExceedsTolerance_ReturnsMatchedWithDiscrepancy()
        {
            // Arrange
            var settlement = CreateSettlementRecord("REF003", 100.50m, "SGD");
            var transactions = new List<Transaction>
            {
                CreateTransaction("REF003", 100.00m, "SGD")
            };

            // Act
            var result = _sut.Match(settlement, transactions);

            // Assert
            Assert.Equal(MatchStatus.MatchedWithDiscrepancy, result.Status);
            Assert.Equal(0.50m, result.DiscrepancyAmount);
        }

        [Fact]
        public void Match_NoMatchingTransaction_ReturnsUnmatched()
        {
            // Arrange
            var settlement = CreateSettlementRecord("REF999", 100.00m, "SGD");
            var transactions = new List<Transaction>
            {
                CreateTransaction("REF001", 200.00m, "SGD"),
                CreateTransaction("REF002", 300.00m, "SGD")
            };

            // Act
            var result = _sut.Match(settlement, transactions);

            // Assert
            Assert.Equal(MatchStatus.Unmatched, result.Status);
        }

        [Fact]
        public void Match_EmptyTransactionList_ReturnsUnmatched()
        {
            // Arrange
            var settlement = CreateSettlementRecord("REF001", 100.00m, "SGD");
            var transactions = new List<Transaction>();

            // Act
            var result = _sut.Match(settlement, transactions);

            // Assert
            Assert.Equal(MatchStatus.Unmatched, result.Status);
            Assert.Contains("No candidate transactions", result.MatchCriteria);
        }

        [Fact]
        public void Match_MultipleReferenceMatches_SelectsByAmount()
        {
            // Arrange
            var settlement = CreateSettlementRecord("REF001", 50.00m, "SGD");
            var transactions = new List<Transaction>
            {
                CreateTransaction("REF001", 100.00m, "SGD"),
                CreateTransaction("REF001", 50.00m, "SGD"),
                CreateTransaction("REF001", 75.00m, "SGD")
            };

            // Act
            var result = _sut.Match(settlement, transactions);

            // Assert
            Assert.Equal(MatchStatus.Matched, result.Status);
            Assert.Equal(50.00m, result.TransactionAmount);
        }

        [Fact]
        public void Match_CaseInsensitiveReference_Matches()
        {
            // Arrange
            var settlement = CreateSettlementRecord("ref001", 100.00m, "SGD");
            var transactions = new List<Transaction>
            {
                CreateTransaction("REF001", 100.00m, "SGD")
            };

            // Act
            var result = _sut.Match(settlement, transactions);

            // Assert
            Assert.Equal(MatchStatus.Matched, result.Status);
        }

        [Fact]
        public void Match_NullSettlementRecord_ThrowsArgumentNull()
        {
            // Arrange
            var transactions = new List<Transaction> { CreateTransaction("REF001", 100m, "SGD") };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _sut.Match(null, transactions));
        }

        [Fact]
        public void IsAmountWithinTolerance_ExactAmount_ReturnsTrue()
        {
            Assert.True(_sut.IsAmountWithinTolerance(100.00m, 100.00m));
        }

        [Fact]
        public void IsAmountWithinTolerance_WithinTolerance_ReturnsTrue()
        {
            Assert.True(_sut.IsAmountWithinTolerance(100.005m, 100.00m));
        }

        [Fact]
        public void IsAmountWithinTolerance_ExceedsTolerance_ReturnsFalse()
        {
            Assert.False(_sut.IsAmountWithinTolerance(100.02m, 100.00m));
        }

        [Fact]
        public void IsExactMatch_AllFieldsMatch_ReturnsTrue()
        {
            // Arrange
            var now = DateTime.UtcNow.Date;
            var settlement = CreateSettlementRecord("REF001", 100.00m, "SGD", now);
            var transaction = CreateTransaction("REF001", 100.00m, "SGD", now);

            // Act & Assert
            Assert.True(_sut.IsExactMatch(settlement, transaction));
        }

        [Fact]
        public void IsExactMatch_DifferentCurrency_ReturnsFalse()
        {
            // Arrange
            var now = DateTime.UtcNow.Date;
            var settlement = CreateSettlementRecord("REF001", 100.00m, "SGD", now);
            var transaction = CreateTransaction("REF001", 100.00m, "USD", now);

            // Act & Assert
            Assert.False(_sut.IsExactMatch(settlement, transaction));
        }

        [Theory]
        [InlineData(-0.01)]
        [InlineData(-1.00)]
        public void Constructor_NegativeTolerance_ThrowsArgumentOutOfRange(decimal tolerance)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TransactionMatcher(tolerance));
        }

        #region Helper Methods

        private SettlementRecord CreateSettlementRecord(string reference, decimal amount, string currency, DateTime? date = null)
        {
            return new SettlementRecord
            {
                SettlementRecordId = Guid.NewGuid(),
                ReferenceNumber = reference,
                Amount = amount,
                Currency = currency,
                TransactionDate = date ?? DateTime.UtcNow.Date,
                SettlementDate = date?.AddDays(1) ?? DateTime.UtcNow.Date.AddDays(1),
                MerchantId = "MERCH001",
                ProcessorCode = "VISA",
                CardType = "Visa",
                BatchNumber = "BATCH001"
            };
        }

        private Transaction CreateTransaction(string reference, decimal amount, string currency, DateTime? date = null)
        {
            return new Transaction
            {
                TransactionId = Guid.NewGuid(),
                ReferenceNumber = reference,
                Amount = amount,
                Currency = currency,
                TransactionDate = date ?? DateTime.UtcNow.Date,
                MerchantId = "MERCH001",
                Status = "Settled",
                ProcessorResponse = "00",
                CardType = "Visa",
                BatchNumber = "BATCH001",
                CreatedAt = DateTime.UtcNow
            };
        }

        #endregion
    }
}
