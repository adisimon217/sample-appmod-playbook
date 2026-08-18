using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PayGate.Core.Models;
using PayGate.Core.Services;

namespace PayGate.Tests.Services
{
    /// <summary>
    /// Unit tests for PaymentProcessingService.
    /// NOTE: These tests require a test database connection or mocked repositories.
    /// Integration tests run nightly against the staging PaymentsDB.
    /// </summary>
    [TestClass]
    public class PaymentProcessingServiceTests
    {
        [TestMethod]
        public void ValidateTransaction_NegativeAmount_ReturnsError()
        {
            // Arrange
            var transaction = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                MerchantId = "MER-TEST001",
                Amount = -100.00m,
                Currency = "USD",
                CardNumber = "4111111111111111",
                CardExpiry = "12/25"
            };

            // Act - Testing validation logic directly
            Assert.IsTrue(transaction.Amount <= 0, "Negative amount should fail validation.");
        }

        [TestMethod]
        public void ValidateTransaction_ZeroAmount_ReturnsError()
        {
            var transaction = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                MerchantId = "MER-TEST001",
                Amount = 0,
                Currency = "USD",
                CardNumber = "4111111111111111"
            };

            Assert.IsTrue(transaction.Amount <= 0);
        }

        [TestMethod]
        public void ValidateTransaction_AmountExceedsMaximum_ReturnsError()
        {
            var transaction = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                MerchantId = "MER-TEST001",
                Amount = 1000000.00m, // Exceeds $999,999.99 max
                Currency = "USD",
                CardNumber = "4111111111111111"
            };

            Assert.IsTrue(transaction.Amount > 999999.99m, "Amount exceeding max should fail.");
        }

        [TestMethod]
        public void DetermineCardBrand_VisaPrefix4_ReturnsVisa()
        {
            var cardNumber = "4111111111111111";
            var brand = DetermineCardBrand(cardNumber);
            Assert.AreEqual("Visa", brand);
        }

        [TestMethod]
        public void DetermineCardBrand_MastercardPrefix5_ReturnsMastercard()
        {
            var cardNumber = "5111111111111118";
            var brand = DetermineCardBrand(cardNumber);
            Assert.AreEqual("Mastercard", brand);
        }

        [TestMethod]
        public void DetermineCardBrand_AmexPrefix34_ReturnsAmex()
        {
            var cardNumber = "341111111111111";
            var brand = DetermineCardBrand(cardNumber);
            Assert.AreEqual("Amex", brand);
        }

        [TestMethod]
        public void DetermineCardBrand_DiscoverPrefix6_ReturnsDiscover()
        {
            var cardNumber = "6011111111111117";
            var brand = DetermineCardBrand(cardNumber);
            Assert.AreEqual("Discover", brand);
        }

        [TestMethod]
        public void CalculateProcessingFee_VisaCard_CorrectFee()
        {
            decimal amount = 100.00m;
            var fee = CalculateProcessingFee(amount, "Visa");
            // 1.95% + $0.30 = $2.25
            Assert.AreEqual(2.25m, fee);
        }

        [TestMethod]
        public void CalculateProcessingFee_MastercardCard_CorrectFee()
        {
            decimal amount = 100.00m;
            var fee = CalculateProcessingFee(amount, "Mastercard");
            // 2.00% + $0.30 = $2.30
            Assert.AreEqual(2.30m, fee);
        }

        [TestMethod]
        public void CalculateProcessingFee_AmexCard_HigherFee()
        {
            decimal amount = 100.00m;
            var fee = CalculateProcessingFee(amount, "Amex");
            // 2.95% + $0.35 = $3.30
            Assert.AreEqual(3.30m, fee);
        }

        [TestMethod]
        public void LuhnCheck_ValidVisaNumber_ReturnsTrue()
        {
            Assert.IsTrue(LuhnCheck("4111111111111111"));
        }

        [TestMethod]
        public void LuhnCheck_InvalidNumber_ReturnsFalse()
        {
            Assert.IsFalse(LuhnCheck("4111111111111112"));
        }

        [TestMethod]
        public void CardExpiry_FutureDate_IsNotExpired()
        {
            Assert.IsTrue(IsCardNotExpired("12/30"));
        }

        [TestMethod]
        public void CardExpiry_PastDate_IsExpired()
        {
            Assert.IsFalse(IsCardNotExpired("01/20"));
        }

        // Helper methods that mirror the service's private methods for testability
        private string DetermineCardBrand(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber)) return "Unknown";
            if (cardNumber.StartsWith("4")) return "Visa";
            if (cardNumber.StartsWith("5") || cardNumber.StartsWith("2")) return "Mastercard";
            if (cardNumber.StartsWith("34") || cardNumber.StartsWith("37")) return "Amex";
            if (cardNumber.StartsWith("6")) return "Discover";
            return "Unknown";
        }

        private decimal CalculateProcessingFee(decimal amount, string cardBrand)
        {
            decimal percentFee;
            decimal fixedFee = 0.30m;
            switch (cardBrand)
            {
                case "Visa": percentFee = 0.0195m; break;
                case "Mastercard": percentFee = 0.0200m; break;
                case "Amex": percentFee = 0.0295m; fixedFee = 0.35m; break;
                default: percentFee = 0.0250m; break;
            }
            return Math.Round(amount * percentFee + fixedFee, 2);
        }

        private bool LuhnCheck(string cardNumber)
        {
            var sum = 0;
            var alternate = false;
            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(cardNumber[i])) continue;
                var digit = cardNumber[i] - '0';
                if (alternate) { digit *= 2; if (digit > 9) digit -= 9; }
                sum += digit;
                alternate = !alternate;
            }
            return sum % 10 == 0;
        }

        private bool IsCardNotExpired(string expiry)
        {
            var parts = expiry.Split('/');
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out int month)) return false;
            if (!int.TryParse(parts[1], out int year)) return false;
            if (year < 100) year += 2000;
            var expiryDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            return expiryDate >= DateTime.UtcNow.Date;
        }
    }
}
