using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using WinSCP;

namespace PayGate.Integration.Sftp
{
    /// <summary>
    /// Wrapper around WinSCP .NET library for SFTP file transfers.
    /// Used for exchanging settlement files with Visa and Mastercard networks.
    /// 
    /// WinSCP was chosen over SSH.NET because:
    /// 1. Better support for SFTP host key verification
    /// 2. Built-in support for resumable transfers
    /// 3. Required by compliance for FIPS-140 compliant SSH implementation
    /// 
    /// NOTE: WinSCP requires winscp.exe to be deployed alongside the application.
    /// The executable path must be set in configuration or it will look in the bin directory.
    /// </summary>
    public class SftpClientWrapper : IDisposable
    {
        private Session _session;
        private readonly SessionOptions _sessionOptions;
        private readonly string _winScpExecutablePath;

        public SftpClientWrapper(string host, int port, string username, 
            string sshHostKeyFingerprint, string privateKeyPath = null)
        {
            _winScpExecutablePath = ConfigurationManager.AppSettings["WinSCP:ExecutablePath"] 
                ?? @"C:\PayGate\Tools\WinSCP\winscp.exe";

            _sessionOptions = new SessionOptions
            {
                Protocol = Protocol.Sftp,
                HostName = host,
                PortNumber = port,
                UserName = username,
                SshHostKeyFingerprint = sshHostKeyFingerprint
            };

            if (!string.IsNullOrEmpty(privateKeyPath))
            {
                _sessionOptions.SshPrivateKeyPath = privateKeyPath;
            }
        }

        /// <summary>
        /// Connect to the SFTP server.
        /// </summary>
        public void Connect()
        {
            _session = new Session();
            _session.ExecutablePath = _winScpExecutablePath;
            _session.Open(_sessionOptions);
        }

        /// <summary>
        /// Upload a file to the remote server.
        /// </summary>
        public TransferOperationResult UploadFile(string localPath, string remotePath)
        {
            if (_session == null || !_session.Opened)
                throw new InvalidOperationException("SFTP session is not connected.");

            var transferOptions = new TransferOptions
            {
                TransferMode = TransferMode.Binary,
                OverwriteMode = OverwriteMode.Overwrite
            };

            var result = _session.PutFiles(localPath, remotePath, false, transferOptions);
            result.Check(); // Throws on failure

            return result;
        }

        /// <summary>
        /// Download a file from the remote server.
        /// </summary>
        public TransferOperationResult DownloadFile(string remotePath, string localPath)
        {
            if (_session == null || !_session.Opened)
                throw new InvalidOperationException("SFTP session is not connected.");

            var transferOptions = new TransferOptions
            {
                TransferMode = TransferMode.Binary
            };

            var result = _session.GetFiles(remotePath, localPath, false, transferOptions);
            result.Check();

            return result;
        }

        /// <summary>
        /// List files in a remote directory.
        /// </summary>
        public RemoteDirectoryInfo ListDirectory(string remotePath)
        {
            if (_session == null || !_session.Opened)
                throw new InvalidOperationException("SFTP session is not connected.");

            return _session.ListDirectory(remotePath);
        }

        /// <summary>
        /// Check if a remote file exists.
        /// </summary>
        public bool FileExists(string remotePath)
        {
            try
            {
                if (_session == null || !_session.Opened) return false;
                return _session.FileExists(remotePath);
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (_session != null)
            {
                if (_session.Opened)
                    _session.Close();
                _session.Dispose();
                _session = null;
            }
        }
    }
}
