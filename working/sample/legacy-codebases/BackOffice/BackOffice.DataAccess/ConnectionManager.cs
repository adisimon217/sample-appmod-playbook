using System;
using System.Configuration;
using System.Data.SqlClient;
using Microsoft.Win32;
using log4net;

namespace BackOffice.DataAccess
{
    /// <summary>
    /// Manages database connection strings.
    /// Reads from ConfigurationManager with fallback to Windows Registry.
    /// </summary>
    public static class ConnectionManager
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ConnectionManager));
        private static string _cachedConnectionString;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the primary connection string from config or registry.
        /// </summary>
        public static string GetConnectionString()
        {
            if (_cachedConnectionString != null)
                return _cachedConnectionString;

            lock (_lock)
            {
                if (_cachedConnectionString != null)
                    return _cachedConnectionString;

                // Try ConfigurationManager first
                var connStringConfig = ConfigurationManager.ConnectionStrings["BackOfficeDB"];
                if (connStringConfig != null && !string.IsNullOrEmpty(connStringConfig.ConnectionString))
                {
                    _cachedConnectionString = connStringConfig.ConnectionString;
                    _log.Info("Connection string loaded from app config");
                    return _cachedConnectionString;
                }

                // Fallback: read from Windows Registry (legacy deployment model)
                _log.Warn("Connection string not in config, checking Windows Registry");
                _cachedConnectionString = GetConnectionStringFromRegistry();
                return _cachedConnectionString;
            }
        }

        /// <summary>
        /// Gets the read-only connection string for reporting queries.
        /// </summary>
        public static string GetReadOnlyConnectionString()
        {
            var connStringConfig = ConfigurationManager.ConnectionStrings["BackOfficeDB_Readonly"];
            if (connStringConfig != null && !string.IsNullOrEmpty(connStringConfig.ConnectionString))
            {
                return connStringConfig.ConnectionString;
            }

            // Fall back to primary if readonly not configured
            _log.Warn("Read-only connection string not configured, using primary connection");
            return GetConnectionString();
        }

        /// <summary>
        /// Reads connection string from Windows Registry.
        /// Legacy pattern from original TFS-deployed installation.
        /// Registry path: HKLM\SOFTWARE\FinancialServices\BackOffice
        /// </summary>
        private static string GetConnectionStringFromRegistry()
        {
            try
            {
                string registryPath = ConfigurationManager.AppSettings["RegistryConfigPath"]
                    ?? @"SOFTWARE\FinancialServices\BackOffice";

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath))
                {
                    if (key != null)
                    {
                        string server = key.GetValue("DatabaseServer", "") as string;
                        string database = key.GetValue("DatabaseName", "BackOfficeDB") as string;
                        string useIntegrated = key.GetValue("UseIntegratedSecurity", "true") as string;

                        if (string.IsNullOrEmpty(server))
                        {
                            throw new InvalidOperationException("Database server not configured in registry");
                        }

                        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                        builder.DataSource = server;
                        builder.InitialCatalog = database;
                        builder.IntegratedSecurity = bool.Parse(useIntegrated);
                        builder.MultipleActiveResultSets = true;
                        builder.ConnectTimeout = 30;

                        _log.InfoFormat("Connection string built from registry: Server={0}, Database={1}", server, database);
                        return builder.ConnectionString;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "Registry key not found: HKLM\\" + registryPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Fatal("Unable to read connection string from registry", ex);
                throw new InvalidOperationException(
                    "Database connection string not found in config or registry. " +
                    "Please ensure the application is properly configured.", ex);
            }
        }

        /// <summary>
        /// Tests the database connection.
        /// </summary>
        public static bool TestConnection()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT 1", conn))
                    {
                        cmd.ExecuteScalar();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Error("Database connection test failed", ex);
                return false;
            }
        }
    }
}
