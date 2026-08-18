using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PayGate.Core.Models;
using PayGate.Integration.FileFormats;

namespace PayGate.Integration.Sftp
{
    /// <summary>
    /// Handles settlement file exchange with Visa network via SFTP.
    /// 
    /// Visa settlement uses fixed-width file format (TC33 specification).
    /// Files are PGP encrypted before transmission.
    /// 
    /// Daily process:
    /// 1. Generate settlement file from captured transactions
    /// 2. Encrypt with Visa's PGP public key
    /// 3. Upload via SFTP to Visa's settlement server
    /// 4. Download and parse response file next business day
    /// </summary>
    public class VisaSettlementClient
    {
        private readonly string _sftpHost;
        private readonly int _sftpPort;
        private readonly string _sftpUsername;
        private readonly string _remotePath;
        private readonly string _localWorkingDir;
        private readonly string _sshKeyFingerprint;
        private readonly string _privateKeyPath;

        public VisaSettlementClient()
        {
            _sftpHost = ConfigurationManager.AppSettings["Visa:SftpHost"];
            _sftpPort = int.Parse(ConfigurationManager.AppSettings["Visa:SftpPort"] ?? "22");
            _sftpUsername = ConfigurationManager.AppSettings["Visa:SftpUsername"];
            _remotePath = ConfigurationManager.AppSettings["Visa:RemotePath"] ?? "/settlement/incoming";
            _localWorkingDir = ConfigurationManager.AppSettings["Visa:LocalWorkingDir"] ?? @"C:\PayGate\Settlement\Visa";
            _sshKeyFingerprint = ConfigurationManager.AppSettings["Visa:SshHostKeyFingerprint"];
            _privateKeyPath = ConfigurationManager.AppSettings["Visa:PrivateKeyPath"];
        }

        /// <summary>
        /// Generate and send a settlement file to Visa.
        /// </summary>
        public async Task SendSettlementFileAsync(
            List<Transaction> transactions, Guid batchId, DateTime settlementDate)
        {
            if (transactions == null || !transactions.Any())
                throw new ArgumentException("No transactions to settle.");

            // Step 1: Generate the fixed-width settlement file (TC33 format)
            var parser = new VisaSettlementParser();
            var fileContent = parser.GenerateSettlementFile(transactions, batchId, settlementDate);

            // Step 2: Write to local temp file
            var fileName = $"VISA_SETTLE_{settlementDate:yyyyMMdd}_{batchId:N}.txt";
            var localFilePath = Path.Combine(_localWorkingDir, fileName);
            var encryptedFilePath = localFilePath + ".pgp";

            Directory.CreateDirectory(_localWorkingDir);
            File.WriteAllText(localFilePath, fileContent, Encoding.ASCII);

            // Step 3: PGP encrypt the file
            var pgpHelper = new PgpEncryptionHelper();
            var visaPgpPublicKey = ConfigurationManager.AppSettings["Visa:PgpPublicKeyPath"]
                ?? @"C:\PayGate\Keys\visa_public.asc";

            await Task.Run(() => pgpHelper.EncryptFile(localFilePath, encryptedFilePath, visaPgpPublicKey));

            // Step 4: Upload encrypted file via SFTP
            using (var sftpClient = new SftpClientWrapper(
                _sftpHost, _sftpPort, _sftpUsername, _sshKeyFingerprint, _privateKeyPath))
            {
                sftpClient.Connect();
                var remoteFilePath = _remotePath + "/" + Path.GetFileName(encryptedFilePath);
                sftpClient.UploadFile(encryptedFilePath, remoteFilePath);
            }

            // Step 5: Clean up local unencrypted file (keep encrypted copy for audit)
            if (File.Exists(localFilePath))
                File.Delete(localFilePath);

            System.Diagnostics.EventLog.WriteEntry("PayGate",
                $"Visa settlement file sent: {fileName} ({transactions.Count} transactions, ${transactions.Sum(t => t.Amount):N2})",
                System.Diagnostics.EventLogEntryType.Information);
        }

        /// <summary>
        /// Download and parse the settlement response file from Visa.
        /// Returns matched transaction IDs for reconciliation.
        /// </summary>
        public async Task<SettlementResponseFile> DownloadResponseFileAsync(DateTime settlementDate)
        {
            var expectedFileName = $"VISA_RESP_{settlementDate:yyyyMMdd}*.pgp";
            var localDownloadPath = Path.Combine(_localWorkingDir, "responses");
            Directory.CreateDirectory(localDownloadPath);

            using (var sftpClient = new SftpClientWrapper(
                _sftpHost, _sftpPort, _sftpUsername, _sshKeyFingerprint, _privateKeyPath))
            {
                sftpClient.Connect();

                var responseRemotePath = _remotePath.Replace("incoming", "outgoing");
                var files = sftpClient.ListDirectory(responseRemotePath);

                // Look for response file matching our settlement date
                // Visa response files typically arrive T+1
                string matchedRemoteFile = null;
                foreach (var file in files.Files)
                {
                    if (file.Name.Contains($"VISA_RESP_{settlementDate:yyyyMMdd}"))
                    {
                        matchedRemoteFile = responseRemotePath + "/" + file.Name;
                        break;
                    }
                }

                if (matchedRemoteFile == null)
                    return null;

                var localResponseFile = Path.Combine(localDownloadPath, Path.GetFileName(matchedRemoteFile));
                sftpClient.DownloadFile(matchedRemoteFile, localResponseFile);

                // Decrypt PGP
                var pgpHelper = new PgpEncryptionHelper();
                var decryptedPath = localResponseFile.Replace(".pgp", ".txt");
                var paygatePgpPrivateKey = ConfigurationManager.AppSettings["PGP:PrivateKeyPath"];

                await Task.Run(() => pgpHelper.DecryptFile(localResponseFile, decryptedPath, paygatePgpPrivateKey));

                // Parse response file
                var parser = new VisaSettlementParser();
                var response = parser.ParseResponseFile(File.ReadAllText(decryptedPath, Encoding.ASCII));

                // Clean up decrypted file
                File.Delete(decryptedPath);

                return response;
            }
        }
    }

    /// <summary>
    /// Parsed settlement response from a card network.
    /// </summary>
    public class SettlementResponseFile
    {
        public List<Guid> MatchedTransactionIds { get; set; } = new List<Guid>();
        public List<Guid> RejectedTransactionIds { get; set; } = new List<Guid>();
        public decimal TotalSettledAmount { get; set; }
        public int ChargebackCount { get; set; }
        public decimal ChargebackAmount { get; set; }
    }
}
