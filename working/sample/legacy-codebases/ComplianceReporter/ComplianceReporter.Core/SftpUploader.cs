using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ComplianceReporter.Core
{
    /// <summary>
    /// Uploads compliance reports to the MAS SFTP endpoint.
    /// Uses raw FTP/SFTP via .NET's built-in FtpWebRequest.
    /// 
    /// NOTE: This uses FTP over SSL (FTPS) since .NET 4.0 doesn't have native SFTP.
    /// The MAS endpoint accepts both SFTP and FTPS. We use FTPS for simplicity.
    /// 
    /// Last verified working: 2023-11-12 (MAS renewed their SSL cert)
    /// Contact for MAS SFTP issues: compliance-tech@mas.gov.sg
    /// </summary>
    public class SftpUploader
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly int _timeoutSeconds;

        public SftpUploader(string host, int port, string username, string password, int timeoutSeconds)
        {
            _host = host;
            _port = port;
            _username = username;
            _password = password;
            _timeoutSeconds = timeoutSeconds;
        }

        /// <summary>
        /// Uploads a file to the remote SFTP/FTPS endpoint.
        /// Synchronous - blocks until upload completes or times out.
        /// </summary>
        public void UploadFile(string localFilePath, string remotePath)
        {
            if (!File.Exists(localFilePath))
            {
                throw new FileNotFoundException("Report file not found for upload: " + localFilePath);
            }

            string fileName = Path.GetFileName(localFilePath);
            string remoteUri = string.Format("ftp://{0}:{1}{2}{3}", _host, _port, remotePath, fileName);

            EventLogger.WriteInfo(string.Format("Uploading: {0} -> {1}", fileName, remoteUri));

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(remoteUri);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential(_username, DecryptPassword(_password));
            request.EnableSsl = true;
            request.UsePassive = true;
            request.UseBinary = true;
            request.KeepAlive = false;
            request.Timeout = _timeoutSeconds * 1000;

            // Accept all certificates - MAS uses internal CA
            // TODO: This should validate against MAS CA certificate
            ServicePointManager.ServerCertificateValidationCallback =
                delegate { return true; };

            byte[] fileContents;
            using (FileStream fs = File.OpenRead(localFilePath))
            {
                fileContents = new byte[fs.Length];
                fs.Read(fileContents, 0, fileContents.Length);
            }

            request.ContentLength = fileContents.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(fileContents, 0, fileContents.Length);
            }

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                EventLogger.WriteInfo(string.Format("Upload complete. Status: {0} ({1})",
                    response.StatusDescription, response.StatusCode));

                if (response.StatusCode != FtpStatusCode.ClosingData &&
                    response.StatusCode != FtpStatusCode.FileActionOK)
                {
                    throw new Exception("SFTP upload failed. Status: " + response.StatusDescription);
                }
            }
        }

        /// <summary>
        /// Verifies connectivity to the MAS SFTP endpoint.
        /// Called during service startup to fail fast if network is unavailable.
        /// </summary>
        public bool TestConnection()
        {
            try
            {
                string remoteUri = string.Format("ftp://{0}:{1}/", _host, _port);

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(remoteUri);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.Credentials = new NetworkCredential(_username, DecryptPassword(_password));
                request.EnableSsl = true;
                request.Timeout = 30000; // 30 second timeout for connectivity test

                ServicePointManager.ServerCertificateValidationCallback =
                    delegate { return true; };

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    return response.StatusCode == FtpStatusCode.OpeningData ||
                           response.StatusCode == FtpStatusCode.DataAlreadyOpen;
                }
            }
            catch (Exception ex)
            {
                EventLogger.WriteWarning("SFTP connectivity test failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Decrypts the stored password. Uses basic DPAPI protection.
        /// Password in config is prefixed with "encrypted:" to indicate it's encrypted.
        /// </summary>
        private string DecryptPassword(string encryptedPassword)
        {
            if (string.IsNullOrEmpty(encryptedPassword))
                return string.Empty;

            if (encryptedPassword.StartsWith("encrypted:"))
            {
                string encrypted = encryptedPassword.Substring("encrypted:".Length);

                // Uses DPAPI (System.Security.Cryptography.ProtectedData) for decryption
                // Encrypted with machine-level scope so only this server can decrypt
                byte[] encryptedBytes = Convert.FromBase64String(encrypted);
                byte[] decryptedBytes = System.Security.Cryptography.ProtectedData.Unprotect(
                    encryptedBytes, null,
                    System.Security.Cryptography.DataProtectionScope.LocalMachine);

                return Encoding.UTF8.GetString(decryptedBytes);
            }

            // Fallback: plain text password (legacy, should be migrated)
            return encryptedPassword;
        }
    }
}
