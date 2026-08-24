using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PayGate.Core.Models;
using PayGate.Integration.FileFormats;

namespace PayGate.Tests.Services
{
    /// <summary>
    /// Unit tests for settlement file generation and parsing.
    /// Tests focus on file format correctness since settlement errors
    /// can result in financial discrepancies and compliance issues.
    /// </summary>
    [TestClass]
    public class SettlementServiceTests
    {
        [TestMethod]
        public void VisaParser_GenerateFile_HasCorrectHeader()
        {
            // Arrange
            var parser = new VisaSettlementParser();
            var transactions = CreateTestTransactions("Visa", 5);
            var batchId = Guid.NewGuid();
            var settlementDate = DateTime.UtcNow.Date;

            // Act
            var fileContent = parser.GenerateSettlementFile(transactions, batchId, settlementDate);

            // Assert
            var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.IsTrue(lines.Length >= 7, "File should have header + 5 details + trailer");
            Assert.IsTrue(lines[0].StartsWith("HD"), "First line should be header record");
            Assert.IsTrue(lines[lines.Length - 1].StartsWith("TR"), "Last line should be trailer record");
        }

        [TestMethod]
        public void VisaParser_GenerateFile_DetailRecordsAreFixedWidth()
        {
            var parser = new VisaSettlementParser();
            var transactions = CreateTestTransactions("Visa", 3);
            var batchId = Guid.NewGuid();
            var settlementDate = DateTime.UtcNow.Date;

            var fileContent = parser.GenerateSettlementFile(transactions, batchId, settlementDate);
            var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Check detail records are exactly 120 chars
            foreach (var line in lines.Where(l => l.StartsWith("TD")))
            {
                Assert.AreEqual(120, line.Length, "Detail record must be exactly 120 characters");
            }
        }

        [TestMethod]
        public void VisaParser_GenerateFile_AmountInCents()
        {
            var parser = new VisaSettlementParser();
            var transactions = new List<Transaction>
            {
                new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    MerchantId = "MER-TEST001",
                    Amount = 99.95m,
                    Currency = "USD",
                    CardBin = "411111",
                    CardLast4 = "1111",
                    AuthorizationCode = "ABC123",
                    TransactionType = TransactionType.Sale,
                    ProcessedDate = DateTime.UtcNow
                }
            };

            var fileContent = parser.GenerateSettlementFile(transactions, Guid.NewGuid(), DateTime.UtcNow.Date);
            var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var detailLine = lines.First(l => l.StartsWith("TD"));
            // Amount at positions 35-46 (0-indexed: 34-45)
            var amountField = detailLine.Substring(34, 12).Trim();
            Assert.AreEqual("000000009995", amountField, "Amount should be 9995 cents for $99.95");
        }

        [TestMethod]
        public void MastercardParser_GenerateFile_HasCorrectCsvHeaders()
        {
            var parser = new MastercardSettlementParser();
            var transactions = CreateTestTransactions("Mastercard", 3);
            var batchId = Guid.NewGuid();
            var settlementDate = DateTime.UtcNow.Date;

            var fileContent = parser.GenerateSettlementFile(transactions, batchId, settlementDate);
            var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Find the column header line (after comment lines)
            var headerLine = lines.First(l => l.StartsWith("TransactionId"));
            Assert.IsNotNull(headerLine);
            Assert.IsTrue(headerLine.Contains("MerchantId"));
            Assert.IsTrue(headerLine.Contains("Amount"));
            Assert.IsTrue(headerLine.Contains("AuthorizationCode"));
        }

        [TestMethod]
        public void MastercardParser_GenerateFile_HasCorrectTransactionCount()
        {
            var parser = new MastercardSettlementParser();
            var transactions = CreateTestTransactions("Mastercard", 10);
            var batchId = Guid.NewGuid();
            var settlementDate = DateTime.UtcNow.Date;

            var fileContent = parser.GenerateSettlementFile(transactions, batchId, settlementDate);
            var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Count data lines (non-comment, non-header)
            var dataLines = lines
                .Where(l => !l.StartsWith("#") && !l.StartsWith("TransactionId") && !string.IsNullOrWhiteSpace(l))
                .Count();

            Assert.AreEqual(10, dataLines, "Should have exactly 10 data rows");
        }

        [TestMethod]
        public void MastercardParser_ParseResponseFile_MatchesTransactions()
        {
            var parser = new MastercardSettlementParser();

            var txnId1 = Guid.NewGuid();
            var txnId2 = Guid.NewGuid();

            var responseContent = @"# Mastercard Response File
TransactionId,ResponseCode,ResponseMessage,SettlementAmount,ChargebackIndicator
" + $"{txnId1},00,Approved,100.00,N\n" +
  $"{txnId2},00,Approved,50.00,N\n";

            var result = parser.ParseResponseFile(responseContent);

            Assert.AreEqual(2, result.MatchedTransactionIds.Count);
            Assert.IsTrue(result.MatchedTransactionIds.Contains(txnId1));
            Assert.IsTrue(result.MatchedTransactionIds.Contains(txnId2));
            Assert.AreEqual(150.00m, result.TotalSettledAmount);
        }

        [TestMethod]
        public void MastercardParser_ParseResponseFile_DetectsChargebacks()
        {
            var parser = new MastercardSettlementParser();

            var txnId1 = Guid.NewGuid();
            var txnId2 = Guid.NewGuid();

            var responseContent = @"# Mastercard Response
TransactionId,ResponseCode,ResponseMessage,SettlementAmount,ChargebackIndicator
" + $"{txnId1},00,Approved,100.00,N\n" +
  $"{txnId2},CB,Chargeback,75.00,CB\n";

            var result = parser.ParseResponseFile(responseContent);

            Assert.AreEqual(1, result.MatchedTransactionIds.Count);
            Assert.AreEqual(1, result.ChargebackCount);
            Assert.AreEqual(75.00m, result.ChargebackAmount);
        }

        private List<Transaction> CreateTestTransactions(string cardBrand, int count)
        {
            var transactions = new List<Transaction>();
            for (int i = 0; i < count; i++)
            {
                transactions.Add(new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    MerchantId = $"MER-TEST{i:D3}",
                    Amount = 50.00m + (i * 10),
                    Currency = "USD",
                    CardBrand = cardBrand,
                    CardBin = cardBrand == "Visa" ? "411111" : "511111",
                    CardLast4 = $"{1111 + i}",
                    AuthorizationCode = $"AUTH{i:D2}",
                    TransactionType = TransactionType.Sale,
                    Status = TransactionStatus.Captured,
                    CreatedDate = DateTime.UtcNow.AddHours(-i),
                    ProcessedDate = DateTime.UtcNow.AddHours(-i)
                });
            }
            return transactions;
        }
    }
}
