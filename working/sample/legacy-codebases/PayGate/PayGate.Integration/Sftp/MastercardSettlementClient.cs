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
    /// Handles settlement file exchange with Mastercard network via SFTP.
    /// 
    /// Mastercard uses CSV format for settlement files (IPM specification).
    /// Files are PGP encrypted before transmission.
    /// 
    /// Settlement window: T+1 for domestic, T+2 for cross-border.
    /// </summary>
    public class MastercardSettlementClient
    {
        private readonly string _sftpHost;
        private readonly int _sftpPort;
        private readonly string _sftpUsername;
        private readonly string _remotePath;
        private readonly string _localWorkingDir;
        private readonly string _sshKeyFingerprint;
        private readonly string _privateKeyPath;

        public MastercardSettlementClient()
        {
            _sftpHost = ConfigurationManager.AppSettings["Mastercard:SftpHost"];
            _sftpPort = int.Parse(ConfigurationManager.AppSettings["Mastercard:SftpPort"] ?? "22");
            _sftpUsername = ConfigurationManager.AppSettings["Mastercard:SftpUsername"];
            _remotePath = ConfigurationManager.AppSettings["Mastercard:RemotePath"] ?? "/files/settlement";
            _localWorkingDir = ConfigurationManager.AppSettings["Mastercard:LocalWorkingDir"] ?? @"C:\PayGate\Settlement\Mastercard";
            _sshKeyFingerprint = ConfigurationManager.AppSettings["Mastercard:SshHostKeyFingerprint"];
            _privateKeyPath = ConfigurationManager.AppSettings["Mastercard:PrivateKeyPath"];
        }

        /// <summary>
        /// Generate and send a settlement file to Mastercard.
        /// </summary>
        public async Task SendSettlementFileAsync(
            List<Transaction> transactions, Guid batchId, DateTime settlementDate)
        {
            if (transactions == null || !transactions.Any())
                throw new ArgumentException("No transactions to settle.");

            // Step 1: Generate CSV settlement file (IPM format)
            var parser = new MastercardSettlementParser();
            var fileContent = parser.GenerateSettlementFile(transactions, batchId, settlementDate);

            // Step 2: Write to local temp file
            var fileName = $"MC_SETTLE_{settlementDate:yyyyMMdd}_{batchId:N}.csv";
            var localFilePath = Path.Combine(_localWorkingDir, fileName);
            var encryptedFilePath = localFilePath + ".pgp";

            Directory.CreateDirectory(_localWorkingDir);
            File.WriteAllText(localFilePath, fileContent, Encoding.UTF8);

            // Step 3: PGP encrypt
            var pgpHelper = new PgpEncryptionHelper();
            var mcPgpPublicKey = ConfigurationManager.AppSettings["Mastercard:PgpPublicKeyPath"]
                ?? @"C:\PayGate\Keys\mastercard_public.asc";

            await Task.Run(() => pgpHelper.EncryptFile(localFilePath, encryptedFilePath, mcPgpPublicKey));

            // Step 4: Upload via SFTP
            using (var sftpClient = new SftpClientWrapper(
                _sftpHost, _sftpPort, _sftpUsername, _sshKeyFingerprint, _privateKeyPath))
            {
                sftpClient.Connect();
                var remoteFilePath = _remotePath + "/incoming/" + Path.GetFileName(encryptedFilePath);
                sftpClient.UploadFile(encryptedFilePath, remoteFilePath);
            }

            // Clean up local unencrypted file
            if (File.Exists(localFilePath))
                File.Delete(localFilePath);

            System.Diagnostics.EventLog.WriteEntry("PayGate",
                $"Mastercard settlement file sent: {fileName} ({transactions.Count} transactions, ${transactions.Sum(t => t.Amount):N2})",
                System.Diagnostics.EventLogEntryType.Information);
        }

        /// <summary>
        /// Download and parse the settlement response file from Mastercard.
        /// </summary>
        public async Task<SettlementResponseFile> DownloadResponseFileAsync(DateTime settlementDate)
        {
            var localDownloadPath = Path.Combine(_localWorkingDir, "responses");
            Directory.CreateDirectory(localDownloadPath);

            using (var sftpClient = new SftpClientWrapper(
                _sftpHost, _sftpPort, _sftpUsername, _sshKeyFingerprint, _privateKeyPath))
            {
                sftpClient.Connect();

                var responseRemotePath = _remotePath + "/outgoing";
                var files = sftpClient.ListDirectory(responseRemotePath);

                string matchedRemoteFile = null;
                foreach (var file in files.Files)
                {
                    if (file.Name.Contains($"MC_RESP_{settlementDate:yyyyMMdd}"))
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
                var decryptedPath = localResponseFile.Replace(".pgp", ".csv");
                var paygatePgpPrivateKey = ConfigurationManager.AppSettings["PGP:PrivateKeyPath"];

                await Task.Run(() => pgpHelper.DecryptFile(localResponseFile, decryptedPath, paygatePgpPrivateKey));

                // Parse CSV response file
                var parser = new MastercardSettlementParser();
                var response = parser.ParseResponseFile(File.ReadAllText(decryptedPath, Encoding.UTF8));

                File.Delete(decryptedPath);

                return response;
            }
        }
    }
}
