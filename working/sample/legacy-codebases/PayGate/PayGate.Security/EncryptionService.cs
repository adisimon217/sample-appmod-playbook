using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PayGate.Security
{
    /// <summary>
    /// Encryption utilities for PayGate.
    /// 
    /// Database-level encryption is handled by SQL Server TDE (Transparent Data Encryption).
    /// This class provides application-level encryption for sensitive data in transit
    /// and configuration helpers for TDE status monitoring.
    /// </summary>
    public class EncryptionService
    {
        // AES-256 key from machine-level DPAPI-protected config
        private static readonly byte[] _encryptionKey;
        private static readonly byte[] _iv;

        static EncryptionService()
        {
            // In production, these come from Windows DPAPI or Azure Key Vault
            var keyBase64 = ConfigurationManager.AppSettings["PayGate:EncryptionKey"];
            var ivBase64 = ConfigurationManager.AppSettings["PayGate:EncryptionIV"];

            if (!string.IsNullOrEmpty(keyBase64))
            {
                _encryptionKey = Convert.FromBase64String(keyBase64);
                _iv = Convert.FromBase64String(ivBase64);
            }
            else
            {
                // Fallback: derive from machine key (not ideal but works for dev)
                using (var deriveBytes = new Rfc2898DeriveBytes("PayGateDefaultKey", 
                    Encoding.UTF8.GetBytes("PayGateSalt"), 10000))
                {
                    _encryptionKey = deriveBytes.GetBytes(32); // AES-256
                    _iv = deriveBytes.GetBytes(16);
                }
            }
        }

        /// <summary>
        /// Encrypt a string value using AES-256.
        /// Used for encrypting sensitive config values and API keys at rest.
        /// </summary>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            using (var aes = Aes.Create())
            {
                aes.Key = _encryptionKey;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var writer = new StreamWriter(cs))
                    {
                        writer.Write(plainText);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        /// <summary>
        /// Decrypt a string value encrypted with Encrypt().
        /// </summary>
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            var cipherBytes = Convert.FromBase64String(cipherText);

            using (var aes = Aes.Create())
            {
                aes.Key = _encryptionKey;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(cipherBytes))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var reader = new StreamReader(cs))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Check if TDE (Transparent Data Encryption) is enabled on PaymentsDB.
        /// Used by health check and admin monitoring endpoints.
        /// </summary>
        public static bool IsTdeEnabled(string connectionString)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(@"
                        SELECT db.is_encrypted 
                        FROM sys.databases db 
                        WHERE db.name = DB_NAME()", conn))
                    {
                        var result = cmd.ExecuteScalar();
                        return result != null && (bool)result;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get TDE encryption status details for monitoring.
        /// </summary>
        public static TdeStatus GetTdeStatus(string connectionString)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(@"
                        SELECT 
                            dek.encryption_state,
                            dek.percent_complete,
                            dek.key_algorithm,
                            dek.key_length,
                            c.name AS certificate_name,
                            c.expiry_date
                        FROM sys.dm_database_encryption_keys dek
                        INNER JOIN sys.certificates c ON dek.encryptor_thumbprint = c.thumbprint
                        WHERE dek.database_id = DB_ID()", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new TdeStatus
                                {
                                    IsEnabled = true,
                                    EncryptionState = (int)reader["encryption_state"],
                                    PercentComplete = reader.IsDBNull(reader.GetOrdinal("percent_complete"))
                                        ? 100 : (decimal)reader["percent_complete"],
                                    Algorithm = reader["key_algorithm"]?.ToString(),
                                    KeyLength = (int)reader["key_length"],
                                    CertificateName = reader["certificate_name"]?.ToString(),
                                    CertificateExpiry = reader.IsDBNull(reader.GetOrdinal("expiry_date"))
                                        ? (DateTime?)null : (DateTime)reader["expiry_date"]
                                };
                            }
                        }
                    }
                }
            }
            catch
            {
                // If we can't query TDE status, report as unknown
            }

            return new TdeStatus { IsEnabled = false };
        }
    }

    public class TdeStatus
    {
        public bool IsEnabled { get; set; }
        public int EncryptionState { get; set; } // 0=none, 1=unencrypted, 2=in progress, 3=encrypted
        public decimal PercentComplete { get; set; }
        public string Algorithm { get; set; }
        public int KeyLength { get; set; }
        public string CertificateName { get; set; }
        public DateTime? CertificateExpiry { get; set; }
    }
}
