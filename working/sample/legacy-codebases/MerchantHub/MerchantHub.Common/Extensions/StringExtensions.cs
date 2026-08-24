using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MerchantHub.Common.Extensions
{
    /// <summary>
    /// String extension methods used throughout the application.
    /// Some of these predate .NET 4.8 features and could be simplified.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Mask a credit card number showing only last 4 digits.
        /// </summary>
        public static string MaskCardNumber(this string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber)) return string.Empty;
            var clean = cardNumber.Replace(" ", "").Replace("-", "");
            if (clean.Length < 4) return "****";
            return new string('*', clean.Length - 4) + clean.Substring(clean.Length - 4);
        }

        /// <summary>
        /// Mask an API key showing only first 7 characters.
        /// </summary>
        public static string MaskApiKey(this string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey) || apiKey.Length < 7) return "***";
            return apiKey.Substring(0, 7) + "...";
        }

        /// <summary>
        /// Format currency value. Always USD for now.
        /// </summary>
        public static string ToCurrency(this decimal value)
        {
            return value.ToString("C", CultureInfo.CreateSpecificCulture("en-US"));
        }

        /// <summary>
        /// Truncate string to specified length with ellipsis.
        /// </summary>
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// Convert string to URL-friendly slug.
        /// </summary>
        public static string ToSlug(this string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var slug = value.ToLower().Trim();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            return slug;
        }

        /// <summary>
        /// Generate a SHA256 hash of the input string.
        /// Used for card fingerprinting.
        /// </summary>
        public static string ToSHA256(this string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// Check if string is a valid email format.
        /// </summary>
        public static bool IsValidEmail(this string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Format EIN with dash (XX-XXXXXXX).
        /// </summary>
        public static string FormatEIN(this string ein)
        {
            if (string.IsNullOrEmpty(ein)) return string.Empty;
            var clean = ein.Replace("-", "");
            if (clean.Length != 9) return ein;
            return clean.Substring(0, 2) + "-" + clean.Substring(2);
        }

        /// <summary>
        /// Format phone number as (XXX) XXX-XXXX.
        /// </summary>
        public static string FormatPhone(this string phone)
        {
            if (string.IsNullOrEmpty(phone)) return string.Empty;
            var clean = new string(phone.Where(char.IsDigit).ToArray());
            if (clean.Length == 10)
                return string.Format("({0}) {1}-{2}", clean.Substring(0, 3), clean.Substring(3, 3), clean.Substring(6));
            if (clean.Length == 11 && clean[0] == '1')
                return string.Format("({0}) {1}-{2}", clean.Substring(1, 3), clean.Substring(4, 3), clean.Substring(7));
            return phone;
        }
    }
}
