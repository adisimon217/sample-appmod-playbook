using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MerchantHub.Core.Models;
using MerchantHub.Core.Services;
using MerchantHub.Data.Repositories;

namespace MerchantHub.Tests.Core
{
    [TestClass]
    public class DisputeServiceTests
    {
        private Mock<IDisputeRepository> _mockDisputeRepo;
        private Mock<ITransactionRepository> _mockTransactionRepo;
        private DisputeService _service;

        [TestInitialize]
        public void Setup()
        {
            _mockDisputeRepo = new Mock<IDisputeRepository>();
            _mockTransactionRepo = new Mock<ITransactionRepository>();
            _service = new DisputeService(_mockDisputeRepo.Object, _mockTransactionRepo.Object);
        }

        [TestMethod]
        public void SubmitResponse_DisputeNotFound_ReturnsFalse()
        {
            _mockDisputeRepo.Setup(r => r.GetById(It.IsAny<int>())).Returns((Dispute)null);

            var result = _service.SubmitResponse(999, "My response", null, "testuser");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SubmitResponse_DisputeAlreadyResolved_ReturnsFalse()
        {
            var dispute = new Dispute
            {
                DisputeId = 1,
                Status = "Won",
                ResponseDeadline = DateTime.Now.AddDays(7)
            };
            _mockDisputeRepo.Setup(r => r.GetById(1)).Returns(dispute);

            var result = _service.SubmitResponse(1, "My response", null, "testuser");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SubmitResponse_PastDeadline_ReturnsFalse()
        {
            var dispute = new Dispute
            {
                DisputeId = 1,
                Status = "Open",
                ResponseDeadline = DateTime.Now.AddDays(-1)
            };
            _mockDisputeRepo.Setup(r => r.GetById(1)).Returns(dispute);

            var result = _service.SubmitResponse(1, "My response", null, "testuser");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SubmitResponse_ValidResponse_ReturnsTrue()
        {
            var dispute = new Dispute
            {
                DisputeId = 1,
                Status = "Open",
                ResponseDeadline = DateTime.Now.AddDays(7)
            };
            _mockDisputeRepo.Setup(r => r.GetById(1)).Returns(dispute);
            _mockDisputeRepo.Setup(r => r.Update(It.IsAny<Dispute>()));
            _mockDisputeRepo.Setup(r => r.AddHistory(It.IsAny<DisputeHistoryEntry>()));

            var result = _service.SubmitResponse(1, "My evidence shows...", null, "testuser");

            Assert.IsTrue(result);
            _mockDisputeRepo.Verify(r => r.Update(It.Is<Dispute>(d => d.Status == "Responded")), Times.Once);
            _mockDisputeRepo.Verify(r => r.AddHistory(It.IsAny<DisputeHistoryEntry>()), Times.Once);
        }

        [TestMethod]
        public void SubmitResponse_WithEvidence_AddsDocuments()
        {
            var dispute = new Dispute
            {
                DisputeId = 1,
                Status = "Open",
                ResponseDeadline = DateTime.Now.AddDays(7)
            };
            _mockDisputeRepo.Setup(r => r.GetById(1)).Returns(dispute);
            _mockDisputeRepo.Setup(r => r.Update(It.IsAny<Dispute>()));
            _mockDisputeRepo.Setup(r => r.AddHistory(It.IsAny<DisputeHistoryEntry>()));
            _mockDisputeRepo.Setup(r => r.AddDocument(It.IsAny<DisputeDocument>()));

            // Note: In a real test we'd need actual files. This test is incomplete.
            // See JIRA-4982 for test improvement backlog
            var evidenceFiles = new List<string>();

            var result = _service.SubmitResponse(1, "Evidence attached", evidenceFiles, "testuser");

            Assert.IsTrue(result);
        }
    }
}
