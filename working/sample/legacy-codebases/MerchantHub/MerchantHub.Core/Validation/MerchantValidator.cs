using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MerchantHub.Core.Models;

namespace MerchantHub.Core.Validation
{
    /// <summary>
    /// Validation rules for merchant data.
    /// Used in both Web and Api controllers.
    /// </summary>
    public class MerchantValidator
    {
        private static readonly string[] ValidStates = {
            "AL","AK","AZ","AR","CA","CO","CT","DE","FL","GA","HI","ID","IL","IN","IA",
            "KS","KY","LA","ME","MD","MA","MI","MN","MS","MO","MT","NE","NV","NH","NJ",
            "NM","NY","NC","ND","OH","OK","OR","PA","RI","SC","SD","TN","TX","UT","VT",
            "VA","WA","WV","WI","WY","DC","PR","VI"
        };

        private static readonly Regex EinRegex = new Regex(@"^\d{2}-?\d{7}$", RegexOptions.Compiled);
        private static readonly Regex ZipRegex = new Regex(@"^\d{5}(-\d{4})?$", RegexOptions.Compiled);
        private static readonly Regex PhoneRegex = new Regex(@"^\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}$", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        public ValidationResult Validate(Merchant merchant)
        {
            var errors = new List<string>();

            if (merchant == null)
            {
                return new ValidationResult { IsValid = false, Errors = new List<string> { "Merchant data is required" } };
            }

            // Business name
            if (string.IsNullOrWhiteSpace(merchant.BusinessName))
                errors.Add("Business name is required");
            else if (merchant.BusinessName.Length > 200)
                errors.Add("Business name cannot exceed 200 characters");

            // EIN validation
            if (!string.IsNullOrEmpty(merchant.EIN))
            {
                if (!EinRegex.IsMatch(merchant.EIN))
                    errors.Add("Invalid EIN format. Expected: XX-XXXXXXX");
            }

            // Address validation
            if (string.IsNullOrWhiteSpace(merchant.Address1))
                errors.Add("Address is required");
            if (string.IsNullOrWhiteSpace(merchant.City))
                errors.Add("City is required");
            if (string.IsNullOrWhiteSpace(merchant.State))
                errors.Add("State is required");
            else if (!ValidStates.Contains(merchant.State.ToUpper()))
                errors.Add("Invalid state code");
            if (string.IsNullOrWhiteSpace(merchant.ZipCode))
                errors.Add("ZIP code is required");
            else if (!ZipRegex.IsMatch(merchant.ZipCode))
                errors.Add("Invalid ZIP code format");

            // Contact info
            if (!string.IsNullOrEmpty(merchant.Phone) && !PhoneRegex.IsMatch(merchant.Phone))
                errors.Add("Invalid phone number format");
            if (string.IsNullOrWhiteSpace(merchant.Email))
                errors.Add("Email is required");
            else if (!EmailRegex.IsMatch(merchant.Email))
                errors.Add("Invalid email format");

            // Business rules
            if (merchant.MonthlyVolumeLimit <= 0)
                errors.Add("Monthly volume limit must be greater than zero");
            if (merchant.SingleTransactionLimit <= 0)
                errors.Add("Single transaction limit must be greater than zero");
            if (merchant.SingleTransactionLimit > merchant.MonthlyVolumeLimit)
                errors.Add("Single transaction limit cannot exceed monthly volume limit");
            if (merchant.ProcessingFeeRate < 0 || merchant.ProcessingFeeRate > 10)
                errors.Add("Processing fee rate must be between 0 and 10 percent");

            // MCC validation
            if (!string.IsNullOrEmpty(merchant.MCC))
            {
                if (merchant.MCC.Length != 4 || !merchant.MCC.All(char.IsDigit))
                    errors.Add("MCC must be a 4-digit code");
            }

            return new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        /// <summary>
        /// Validate merchant for onboarding (stricter rules)
        /// </summary>
        public ValidationResult ValidateForOnboarding(Merchant merchant)
        {
            var result = Validate(merchant);
            var errors = result.Errors.ToList();

            // Additional onboarding requirements
            if (string.IsNullOrWhiteSpace(merchant.EIN))
                errors.Add("EIN is required for onboarding");
            if (string.IsNullOrWhiteSpace(merchant.MCC))
                errors.Add("MCC is required for onboarding");
            if (merchant.ContractStartDate == default(DateTime))
                errors.Add("Contract start date is required");
            if (string.IsNullOrWhiteSpace(merchant.Website))
                errors.Add("Website is required for onboarding");

            return new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
