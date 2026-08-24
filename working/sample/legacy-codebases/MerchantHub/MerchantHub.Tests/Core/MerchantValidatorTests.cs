using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MerchantHub.Core.Models;
using MerchantHub.Core.Validation;

namespace MerchantHub.Tests.Core
{
    [TestClass]
    public class MerchantValidatorTests
    {
        private MerchantValidator _validator;

        [TestInitialize]
        public void Setup()
        {
            _validator = new MerchantValidator();
        }

        [TestMethod]
        public void Validate_ValidMerchant_ReturnsValid()
        {
            var merchant = CreateValidMerchant();
            var result = _validator.Validate(merchant);
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [TestMethod]
        public void Validate_NullMerchant_ReturnsInvalid()
        {
            var result = _validator.Validate(null);
            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void Validate_EmptyBusinessName_ReturnsError()
        {
            var merchant = CreateValidMerchant();
            merchant.BusinessName = "";
            var result = _validator.Validate(merchant);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Contains("Business name is required"));
        }

        [TestMethod]
        public void Validate_BusinessNameTooLong_ReturnsError()
        {
            var merchant = CreateValidMerchant();
            merchant.BusinessName = new string('A', 201);
            var result = _validator.Validate(merchant);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Contains("Business name cannot exceed 200 characters"));
        }

        [TestMethod]
        public void Validate_ValidEIN_ReturnsValid()
        {
            var merchant = CreateValidMerchant();
            merchant.EIN = "12-3456789";
            var result = _validator.Validate(merchant);
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void Validate_InvalidEIN_ReturnsError()
        {
            var merchant = CreateValidMerchant();
            merchant.EIN = "123";
            var result = _validator.Validate(merchant);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Contains("Invalid EIN format. Expected: XX-XXXXXXX"));
        }

        [TestMethod]
        public void Validate_InvalidState_ReturnsError()
        {
            var merchant = CreateValidMerchant();
            merchant.State = "XX";
            var result = _validator.Validate(merchant);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Contains("Invalid state code"));
        }

        [TestMethod]
        public void Validate_InvalidZipCode_ReturnsError()
        {
            var merchant = CreateValidMerchant();
            merchant.ZipCode = "1234";
            var result = _validator.Validate(merchant);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Contains("Invalid ZIP code format"));
        }

        [TestMethod]
        public void Validate_InvalidEmail_ReturnsError()
        {
            var merchant = CreateValidMerchant();
            merchant.Email = "not-an-email";
            var result = _validator.Validate(merchant);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Contains("Invalid email format"));
        }

        [TestMethod]
        public void Validate_NegativeVolumeLimit_ReturnsError()
        {
            var merchant = CreateValidMerchant();
            merchant.MonthlyVolumeLimit = -1000;
            var result = _validator.Validate(merchant);
            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void Validate_SingleLimitExceedsMonthly_ReturnsError()
        {
            var merchant = CreateValidMerchant();
            merchant.SingleTransactionLimit = 200000;
            merchant.MonthlyVolumeLimit = 100000;
            var result = _validator.Validate(merchant);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Contains("Single transaction limit cannot exceed monthly volume limit"));
        }

        [TestMethod]
        public void ValidateForOnboarding_MissingEIN_ReturnsError()
        {
            var merchant = CreateValidMerchant();
            merchant.EIN = null;
            var result = _validator.ValidateForOnboarding(merchant);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Contains("EIN is required for onboarding"));
        }

        [TestMethod]
        public void ValidateForOnboarding_MissingWebsite_ReturnsError()
        {
            var merchant = CreateValidMerchant();
            merchant.Website = null;
            var result = _validator.ValidateForOnboarding(merchant);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Contains("Website is required for onboarding"));
        }

        private Merchant CreateValidMerchant()
        {
            return new Merchant
            {
                BusinessName = "Test Merchant LLC",
                DBA = "Test Shop",
                EIN = "12-3456789",
                Address1 = "123 Main St",
                City = "Anytown",
                State = "NY",
                ZipCode = "10001",
                Phone = "(555) 123-4567",
                Email = "test@merchant.com",
                Website = "https://testmerchant.com",
                MCC = "5411",
                MonthlyVolumeLimit = 500000,
                SingleTransactionLimit = 10000,
                ProcessingFeeRate = 2.75m,
                ContractStartDate = DateTime.Now
            };
        }
    }
}
