using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using log4net;

namespace MerchantHub.Common.Helpers
{
    /// <summary>
    /// Encryption utilities for sensitive data.
    /// Used for API key storage, password hashing, etc.
    /// WARNING: The AES key is stored in web.config machineKey section.
    ///          If the machineKey changes, existing encrypted data becomes unreadable.
    /// </summary>
    public static class EncryptionHelper
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(EncryptionHelper));

        // Hardcoded IV - this should be random per encryption operation but
        // changing it would break existing encrypted data in the database.
        // TODO: Migrate to proper per-record IV storage (JIRA-4891)
        private static readonly byte[] IV = { 0x49, 0x76, 0x61, 0x6E, 0x20, 0x4D, 0x65, 0x64,
                                              0x76, 0x65, 0x64, 0x65, 0x76, 0x00, 0x00, 0x00 };

        /// <summary>
        /// Encrypt a string using AES. Key derived from machineKey.
        /// </summary>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            try
            {
                var key = GetEncryptionKey();
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = IV;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var encryptor = aes.CreateEncryptor())
                    using (var ms = new MemoryStream())
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        var plainBytes = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Encryption failed", ex);
                throw;
            }
        }

        /// <summary>
        /// Decrypt a string using AES. Key derived from machineKey.
        /// </summary>
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            try
            {
                var key = GetEncryptionKey();
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = IV;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream(Convert.FromBase64String(cipherText)))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var reader = new StreamReader(cs))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Decryption failed", ex);
                throw;
            }
        }

        /// <summary>
        /// Hash password with salt using PBKDF2.
        /// </summary>
        public static string HashPassword(string password, string salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt), 10000))
            {
                var hash = pbkdf2.GetBytes(32);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Generate a random salt for password hashing.
        /// </summary>
        public static string GenerateSalt()
        {
            var salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return Convert.ToBase64String(salt);
        }

        /// <summary>
        /// Verify a password against its hash and salt.
        /// </summary>
        public static bool VerifyPassword(string password, string hash, string salt)
        {
            var computedHash = HashPassword(password, salt);
            return computedHash == hash;
        }

        private static byte[] GetEncryptionKey()
        {
            // Derive key from machineKey decryptionKey in web.config
            var machineKey = ConfigurationManager.AppSettings["MerchantHub.EncryptionKey"]
                ?? "8A9BE8FD67AF6979E7D20198CFEA50DD3D987947B4760C2AB82048";

            // Take first 32 bytes (256 bits) for AES-256
            var keyBytes = Encoding.UTF8.GetBytes(machineKey);
            var key = new byte[32];
            Array.Copy(keyBytes, key, Math.Min(keyBytes.Length, 32));
            return key;
        }
    }
}
