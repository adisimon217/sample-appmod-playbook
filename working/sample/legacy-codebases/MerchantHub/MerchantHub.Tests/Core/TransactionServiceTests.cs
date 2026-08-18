using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MerchantHub.Core.Models;
using MerchantHub.Core.Services;
using MerchantHub.Data;
using MerchantHub.Data.Repositories;

namespace MerchantHub.Tests.Core
{
    [TestClass]
    public class TransactionServiceTests
    {
        private Mock<ITransactionRepository> _mockTransactionRepo;
        private Mock<PayGateReadContext> _mockPayGateContext;
        private TransactionService _service;

        [TestInitialize]
        public void Setup()
        {
            _mockTransactionRepo = new Mock<ITransactionRepository>();
            _mockPayGateContext = new Mock<PayGateReadContext>();
            _service = new TransactionService(_mockTransactionRepo.Object, _mockPayGateContext.Object);
        }

        [TestMethod]
        public void ProcessRefund_TransactionNotFound_ReturnsFailure()
        {
            _mockTransactionRepo.Setup(r => r.GetById(It.IsAny<long>())).Returns((Transaction)null);

            var result = _service.ProcessRefund(12345, 50.00m, "Customer request", "testuser");

            Assert.IsFalse(result.Success);
            Assert.AreEqual("Transaction not found", result.ErrorMessage);
        }

        [TestMethod]
        public void ProcessRefund_TransactionDeclined_ReturnsFailure()
        {
            var transaction = new Transaction
            {
                TransactionId = 12345,
                Amount = 100.00m,
                Status = "Declined",
                TransactionDate = DateTime.Now.AddDays(-1)
            };
            _mockTransactionRepo.Setup(r => r.GetById(12345)).Returns(transaction);

            var result = _service.ProcessRefund(12345, 50.00m, "Customer request", "testuser");

            Assert.IsFalse(result.Success);
            Assert.AreEqual("Transaction is not in a refundable state", result.ErrorMessage);
        }

        [TestMethod]
        public void ProcessRefund_AmountExceedsOriginal_ReturnsFailure()
        {
            var transaction = new Transaction
            {
                TransactionId = 12345,
                Amount = 100.00m,
                RefundAmount = 0,
                Status = "Approved",
                TransactionDate = DateTime.Now.AddDays(-5)
            };
            _mockTransactionRepo.Setup(r => r.GetById(12345)).Returns(transaction);

            var result = _service.ProcessRefund(12345, 150.00m, "Customer request", "testuser");

            Assert.IsFalse(result.Success);
            StringAssert.Contains(result.ErrorMessage, "exceeds remaining refundable amount");
        }

        [TestMethod]
        public void ProcessRefund_TransactionTooOld_ReturnsFailure()
        {
            var transaction = new Transaction
            {
                TransactionId = 12345,
                Amount = 100.00m,
                RefundAmount = 0,
                Status = "Approved",
                TransactionDate = DateTime.Now.AddDays(-150) // Past 120-day window
            };
            _mockTransactionRepo.Setup(r => r.GetById(12345)).Returns(transaction);

            var result = _service.ProcessRefund(12345, 50.00m, "Customer request", "testuser");

            Assert.IsFalse(result.Success);
            Assert.AreEqual("Transaction is past the 120-day refund window", result.ErrorMessage);
        }

        // NOTE: Cannot test successful refund without SQL connection.
        // This is a known limitation of the current architecture.
        // TODO: Extract SQL operations behind an interface (JIRA-4521)
    }
}
