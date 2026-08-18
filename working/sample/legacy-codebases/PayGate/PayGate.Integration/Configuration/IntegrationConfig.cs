using System.Configuration;

namespace PayGate.Integration.Configuration
{
    /// <summary>
    /// Configuration provider for external integration settings.
    /// Reads from Web.config appSettings section.
    /// 
    /// TODO: Migrate to a proper configuration service (Azure App Config or AWS SSM)
    /// as part of cloud migration effort. Current approach has issues:
    /// 1. Secrets in config files (even if encrypted with DPAPI)
    /// 2. Requires IIS restart for config changes
    /// 3. Config not shared across web farm nodes easily
    /// </summary>
    public static class IntegrationConfig
    {
        // Visa SFTP Configuration
        public static string VisaSftpHost => ConfigurationManager.AppSettings["Visa:SftpHost"];
        public static int VisaSftpPort => int.Parse(ConfigurationManager.AppSettings["Visa:SftpPort"] ?? "22");
        public static string VisaSftpUsername => ConfigurationManager.AppSettings["Visa:SftpUsername"];
        public static string VisaRemotePath => ConfigurationManager.AppSettings["Visa:RemotePath"];
        public static string VisaSshKeyFingerprint => ConfigurationManager.AppSettings["Visa:SshHostKeyFingerprint"];
        public static string VisaPrivateKeyPath => ConfigurationManager.AppSettings["Visa:PrivateKeyPath"];
        public static string VisaPgpPublicKeyPath => ConfigurationManager.AppSettings["Visa:PgpPublicKeyPath"];
        public static string VisaLocalWorkingDir => ConfigurationManager.AppSettings["Visa:LocalWorkingDir"]
            ?? @"C:\PayGate\Settlement\Visa";

        // Mastercard SFTP Configuration
        public static string MastercardSftpHost => ConfigurationManager.AppSettings["Mastercard:SftpHost"];
        public static int MastercardSftpPort => int.Parse(ConfigurationManager.AppSettings["Mastercard:SftpPort"] ?? "22");
        public static string MastercardSftpUsername => ConfigurationManager.AppSettings["Mastercard:SftpUsername"];
        public static string MastercardRemotePath => ConfigurationManager.AppSettings["Mastercard:RemotePath"];
        public static string MastercardSshKeyFingerprint => ConfigurationManager.AppSettings["Mastercard:SshHostKeyFingerprint"];
        public static string MastercardPrivateKeyPath => ConfigurationManager.AppSettings["Mastercard:PrivateKeyPath"];
        public static string MastercardPgpPublicKeyPath => ConfigurationManager.AppSettings["Mastercard:PgpPublicKeyPath"];
        public static string MastercardLocalWorkingDir => ConfigurationManager.AppSettings["Mastercard:LocalWorkingDir"]
            ?? @"C:\PayGate\Settlement\Mastercard";

        // PGP Configuration
        public static string PgpPublicKeyPath => ConfigurationManager.AppSettings["PGP:PublicKeyPath"];
        public static string PgpPrivateKeyPath => ConfigurationManager.AppSettings["PGP:PrivateKeyPath"];
        public static string PgpPassphrase => ConfigurationManager.AppSettings["PGP:PrivateKeyPassphrase"];

        // General Settlement Configuration
        public static int SettlementBatchSize => int.Parse(
            ConfigurationManager.AppSettings["PayGate:SettlementBatchSize"] ?? "5000");
        public static int MaxRetryAttempts => int.Parse(
            ConfigurationManager.AppSettings["PayGate:MaxRetryAttempts"] ?? "3");
        public static int SftpTimeoutSeconds => int.Parse(
            ConfigurationManager.AppSettings["PayGate:SftpTimeoutSeconds"] ?? "120");
    }
}
