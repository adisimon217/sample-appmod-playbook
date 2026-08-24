using System;
using System.Configuration;
using log4net;

namespace MerchantHub.Common.Helpers
{
    /// <summary>
    /// Helper for reading configuration values from web.config.
    /// Provides defaults and type-safe access.
    /// NOTE: This reads from ConfigurationManager.AppSettings directly,
    ///       not from any external configuration service.
    /// </summary>
    public static class ConfigurationHelper
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ConfigurationHelper));

        public static string GetString(string key, string defaultValue = "")
        {
            var value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrEmpty(value))
            {
                _log.WarnFormat("Configuration key '{0}' not found, using default: '{1}'", key, defaultValue);
                return defaultValue;
            }
            return value;
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            var value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrEmpty(value)) return defaultValue;

            int result;
            if (int.TryParse(value, out result))
                return result;

            _log.WarnFormat("Configuration key '{0}' has invalid int value: '{1}'", key, value);
            return defaultValue;
        }

        public static bool GetBool(string key, bool defaultValue = false)
        {
            var value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrEmpty(value)) return defaultValue;

            bool result;
            if (bool.TryParse(value, out result))
                return result;

            _log.WarnFormat("Configuration key '{0}' has invalid bool value: '{1}'", key, value);
            return defaultValue;
        }

        public static decimal GetDecimal(string key, decimal defaultValue = 0m)
        {
            var value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrEmpty(value)) return defaultValue;

            decimal result;
            if (decimal.TryParse(value, out result))
                return result;

            _log.WarnFormat("Configuration key '{0}' has invalid decimal value: '{1}'", key, value);
            return defaultValue;
        }

        public static string GetConnectionString(string name)
        {
            var cs = ConfigurationManager.ConnectionStrings[name];
            if (cs == null)
            {
                throw new ConfigurationErrorsException(
                    string.Format("Connection string '{0}' not found in configuration.", name));
            }
            return cs.ConnectionString;
        }

        /// <summary>
        /// Get environment name (Production, Staging, Development).
        /// </summary>
        public static string Environment
        {
            get { return GetString("MerchantHub.Environment", "Production"); }
        }

        public static bool IsProduction
        {
            get { return Environment.Equals("Production", StringComparison.OrdinalIgnoreCase); }
        }

        public static string ReportOutputPath
        {
            get { return GetString("MerchantHub.ReportOutputPath", @"D:\MerchantHub\Reports\"); }
        }

        public static string LogPath
        {
            get { return GetString("MerchantHub.LogPath", @"D:\MerchantHub\Logs\"); }
        }

        public static string UploadPath
        {
            get { return GetString("MerchantHub.UploadPath", @"D:\MerchantHub\Uploads\"); }
        }
    }
}
