namespace MerchantHub.Common
{
    /// <summary>
    /// Application-wide constants.
    /// NOTE: Some of these are duplicated in appSettings - need to consolidate.
    /// </summary>
    public static class Constants
    {
        // Application
        public const string ApplicationName = "MerchantHub";
        public const string ApplicationVersion = "4.2.1";

        // Merchant Status
        public const string MerchantStatusActive = "Active";
        public const string MerchantStatusSuspended = "Suspended";
        public const string MerchantStatusClosed = "Closed";
        public const string MerchantStatusPendingReview = "PendingReview";

        // Transaction Status
        public const string TransactionApproved = "Approved";
        public const string TransactionDeclined = "Declined";
        public const string TransactionPending = "Pending";
        public const string TransactionRefunded = "Refunded";
        public const string TransactionChargeback = "Chargeback";
        public const string TransactionVoided = "Voided";
        public const string TransactionPartialRefund = "PartialRefund";

        // Dispute Status
        public const string DisputeOpen = "Open";
        public const string DisputeUnderReview = "UnderReview";
        public const string DisputeResponded = "Responded";
        public const string DisputeWon = "Won";
        public const string DisputeLost = "Lost";
        public const string DisputeExpired = "Expired";

        // User Roles
        public const string RoleAdmin = "Admin";
        public const string RoleInternalSupport = "InternalSupport";
        public const string RoleMerchantOwner = "MerchantOwner";
        public const string RoleMerchantUser = "MerchantUser";
        public const string RoleMerchantReadOnly = "MerchantReadOnly";

        // Merchant Tiers
        public const string TierStandard = "Standard";
        public const string TierPremium = "Premium";
        public const string TierEnterprise = "Enterprise";

        // Card Types
        public const string CardVisa = "Visa";
        public const string CardMastercard = "Mastercard";
        public const string CardAmex = "Amex";
        public const string CardDiscover = "Discover";

        // File paths (these should be in config but are hardcoded in some places)
        public const string DefaultReportPath = @"D:\MerchantHub\Reports\";
        public const string DefaultLogPath = @"D:\MerchantHub\Logs\";
        public const string DefaultUploadPath = @"D:\MerchantHub\Uploads\";

        // Limits
        public const int MaxUploadSizeMB = 25;
        public const int MaxPageSize = 500;
        public const int DefaultPageSize = 50;
        public const int SessionTimeoutMinutes = 30;
        public const int PasswordResetExpiryHours = 24;
        public const int MaxLoginAttempts = 5;
        public const int AccountLockoutMinutes = 30;
        public const int RefundWindowDays = 120;
        public const decimal ChargebackThresholdPercent = 1.0m;

        // Report generation
        public const int MaxConcurrentReports = 5;
        public const int ReportTimeoutSeconds = 300;
    }
}
