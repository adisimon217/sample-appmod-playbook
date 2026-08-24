using System;
using System.Diagnostics;
using System.IO;

namespace PayGate.Integration.FileFormats
{
    /// <summary>
    /// PGP encryption/decryption helper for settlement file exchange.
    /// 
    /// Uses GnuPG (gpg.exe) installed on the server for encryption operations.
    /// Card network settlement files must be PGP encrypted before transmission
    /// per PCI DSS and network security requirements.
    /// 
    /// Key management:
    /// - PayGate's private key: used to decrypt incoming response files
    /// - Visa's public key: used to encrypt outgoing settlement files to Visa
    /// - Mastercard's public key: used to encrypt outgoing settlement files to MC
    /// - Keys are stored in C:\PayGate\Keys\ and managed by infra team
    /// - Key rotation happens quarterly per compliance requirements
    /// 
    /// NOTE: This implementation shells out to gpg.exe. A managed library
    /// (BouncyCastle) was evaluated but had compatibility issues with the
    /// specific PGP key format used by card networks.
    /// </summary>
    public class PgpEncryptionHelper
    {
        private const string GpgExecutablePath = @"C:\Program Files\GnuPG\bin\gpg.exe";

        /// <summary>
        /// Encrypt a file using the recipient's PGP public key.
        /// </summary>
        /// <param name="inputFilePath">Path to the plaintext file to encrypt</param>
        /// <param name="outputFilePath">Path for the encrypted output file</param>
        /// <param name="recipientPublicKeyPath">Path to the recipient's PGP public key (.asc)</param>
        public void EncryptFile(string inputFilePath, string outputFilePath, string recipientPublicKeyPath)
        {
            if (!File.Exists(inputFilePath))
                throw new FileNotFoundException("Input file not found.", inputFilePath);

            if (!File.Exists(recipientPublicKeyPath))
                throw new FileNotFoundException("Recipient public key not found.", recipientPublicKeyPath);

            // Import the public key if not already in keyring
            ExecuteGpgCommand($"--import \"{recipientPublicKeyPath}\"", allowFailure: true);

            // Get the key ID from the public key file
            var keyId = GetKeyIdFromFile(recipientPublicKeyPath);

            // Encrypt the file
            var args = $"--batch --yes --trust-model always --recipient {keyId} " +
                       $"--output \"{outputFilePath}\" --encrypt \"{inputFilePath}\"";

            var result = ExecuteGpgCommand(args);

            if (!File.Exists(outputFilePath))
            {
                throw new InvalidOperationException(
                    $"PGP encryption failed. GPG output: {result}");
            }

            EventLog.WriteEntry("PayGate",
                $"PGP encrypted: {Path.GetFileName(inputFilePath)} -> {Path.GetFileName(outputFilePath)}",
                EventLogEntryType.Information);
        }

        /// <summary>
        /// Decrypt a PGP encrypted file using PayGate's private key.
        /// </summary>
        /// <param name="inputFilePath">Path to the encrypted file (.pgp)</param>
        /// <param name="outputFilePath">Path for the decrypted output file</param>
        /// <param name="privateKeyPath">Path to PayGate's PGP private key</param>
        public void DecryptFile(string inputFilePath, string outputFilePath, string privateKeyPath)
        {
            if (!File.Exists(inputFilePath))
                throw new FileNotFoundException("Encrypted input file not found.", inputFilePath);

            // Import private key if needed
            if (!string.IsNullOrEmpty(privateKeyPath) && File.Exists(privateKeyPath))
            {
                ExecuteGpgCommand($"--import \"{privateKeyPath}\"", allowFailure: true);
            }

            // Passphrase is stored in Windows DPAPI-protected config
            var passphrase = GetPrivateKeyPassphrase();

            var args = $"--batch --yes --passphrase \"{passphrase}\" " +
                       $"--output \"{outputFilePath}\" --decrypt \"{inputFilePath}\"";

            var result = ExecuteGpgCommand(args);

            if (!File.Exists(outputFilePath))
            {
                throw new InvalidOperationException(
                    $"PGP decryption failed. GPG output: {result}");
            }

            EventLog.WriteEntry("PayGate",
                $"PGP decrypted: {Path.GetFileName(inputFilePath)} -> {Path.GetFileName(outputFilePath)}",
                EventLogEntryType.Information);
        }

        private string ExecuteGpgCommand(string arguments, bool allowFailure = false)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = GpgExecutablePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit(30000); // 30 second timeout

                if (process.ExitCode != 0 && !allowFailure)
                {
                    throw new InvalidOperationException(
                        $"GPG command failed (exit code {process.ExitCode}): {error}");
                }

                return output + error;
            }
        }

        private string GetKeyIdFromFile(string publicKeyPath)
        {
            var output = ExecuteGpgCommand($"--with-colons --import-options show-only --import \"{publicKeyPath}\"");
            // Parse key ID from GPG output (simplified)
            var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.StartsWith("pub:") || line.StartsWith("uid:"))
                {
                    var fields = line.Split(':');
                    if (fields.Length > 4 && !string.IsNullOrEmpty(fields[4]))
                        return fields[4];
                }
            }

            // Fallback: use filename-based convention
            var fileName = Path.GetFileNameWithoutExtension(publicKeyPath);
            return fileName;
        }

        private string GetPrivateKeyPassphrase()
        {
            // In production, this reads from Windows DPAPI-protected configuration
            // or from a Hardware Security Module (HSM)
            var encryptedPassphrase = System.Configuration.ConfigurationManager
                .AppSettings["PGP:PrivateKeyPassphrase"];

            if (!string.IsNullOrEmpty(encryptedPassphrase))
            {
                return PayGate.Security.EncryptionService.Decrypt(encryptedPassphrase);
            }

            return string.Empty;
        }
    }
}
