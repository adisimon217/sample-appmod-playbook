using System;
using System.Configuration;
using Microsoft.Win32;
using log4net;

namespace BackOffice.IISModules
{
    /// <summary>
    /// Reads module configuration from Windows Registry.
    /// Legacy pattern: application configuration stored in HKLM registry hive
    /// rather than config files. This creates a dependency on the Windows OS
    /// and requires admin privileges to configure.
    /// 
    /// Registry path: HKLM\SOFTWARE\FinancialServices\BackOffice\Modules
    /// </summary>
    public static class RegistryConfigReader
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(RegistryConfigReader));
        private static readonly string _baseRegistryPath;

        static RegistryConfigReader()
        {
            _baseRegistryPath = ConfigurationManager.AppSettings["RegistryConfigPath"]
                ?? @"SOFTWARE\FinancialServices\BackOffice";
        }

        /// <summary>
        /// Gets module-specific configuration from registry.
        /// Path: HKLM\SOFTWARE\FinancialServices\BackOffice\Modules\{moduleName}
        /// </summary>
        public static ModuleConfig GetModuleConfig(string moduleName)
        {
            var config = new ModuleConfig();

            try
            {
                string path = _baseRegistryPath + @"\Modules\" + moduleName;
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(path))
                {
                    if (key != null)
                    {
                        config.IsEnabled = Convert.ToBoolean(key.GetValue("Enabled", true));
                        config.AuditLevel = key.GetValue("AuditLevel", "Standard") as string;
                        config.LogPath = key.GetValue("LogPath", @"D:\Logs\BackOffice") as string;

                        _log.DebugFormat("Registry config loaded for module {0}: Enabled={1}, Level={2}",
                            moduleName, config.IsEnabled, config.AuditLevel);
                    }
                    else
                    {
                        // Default to enabled if registry key doesn't exist
                        config.IsEnabled = true;
                        config.AuditLevel = "Standard";
                        _log.WarnFormat("Registry key not found for module {0}, using defaults", moduleName);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Error reading registry for module {0}: {1}", moduleName, ex.Message);
                config.IsEnabled = true; // Default to enabled on error
                config.AuditLevel = "Standard";
            }

            return config;
        }

        /// <summary>
        /// Gets security-specific configuration from registry.
        /// Path: HKLM\SOFTWARE\FinancialServices\BackOffice\Security
        /// </summary>
        public static SecurityConfig GetSecurityConfig()
        {
            var config = new SecurityConfig();

            try
            {
                string path = _baseRegistryPath + @"\Security";
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(path))
                {
                    if (key != null)
                    {
                        config.FrameOptions = key.GetValue("X-Frame-Options", "DENY") as string;
                        config.ContentSecurityPolicy = key.GetValue("ContentSecurityPolicy",
                            "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'") as string;
                        config.EnforceHsts = Convert.ToBoolean(key.GetValue("EnforceHSTS", true));
                        config.HstsMaxAge = Convert.ToInt32(key.GetValue("HSTSMaxAge", 31536000));
                    }
                    else
                    {
                        // Defaults
                        config.FrameOptions = "DENY";
                        config.ContentSecurityPolicy = "default-src 'self'";
                        config.EnforceHsts = true;
                        config.HstsMaxAge = 31536000;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error reading security config from registry", ex);
                config.FrameOptions = "DENY";
                config.EnforceHsts = true;
                config.HstsMaxAge = 31536000;
            }

            return config;
        }

        /// <summary>
        /// Gets session validation configuration from registry.
        /// Path: HKLM\SOFTWARE\FinancialServices\BackOffice\Session
        /// </summary>
        public static SessionConfig GetSessionConfig()
        {
            var config = new SessionConfig();

            try
            {
                string path = _baseRegistryPath + @"\Session";
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(path))
                {
                    if (key != null)
                    {
                        config.MaxConcurrentSessions = Convert.ToInt32(key.GetValue("MaxConcurrentSessions", 3));
                        config.IdleTimeoutMinutes = Convert.ToInt32(key.GetValue("IdleTimeoutMinutes", 30));
                        config.ValidateClientIp = Convert.ToBoolean(key.GetValue("ValidateClientIP", true));
                    }
                    else
                    {
                        config.MaxConcurrentSessions = 3;
                        config.IdleTimeoutMinutes = 30;
                        config.ValidateClientIp = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error reading session config from registry", ex);
                config.MaxConcurrentSessions = 3;
                config.IdleTimeoutMinutes = 30;
                config.ValidateClientIp = true;
            }

            return config;
        }

        /// <summary>
        /// Gets a raw string value from registry.
        /// </summary>
        public static string GetRegistryValue(string subPath, string valueName, string defaultValue = null)
        {
            try
            {
                string fullPath = _baseRegistryPath + @"\" + subPath;
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(fullPath))
                {
                    if (key != null)
                    {
                        return key.GetValue(valueName, defaultValue) as string;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Error reading registry value {0}\\{1}: {2}", subPath, valueName, ex.Message);
            }

            return defaultValue;
        }
    }

    /// <summary>
    /// Configuration model for IIS modules.
    /// </summary>
    public class ModuleConfig
    {
        public bool IsEnabled { get; set; }
        public string AuditLevel { get; set; }
        public string LogPath { get; set; }
    }

    /// <summary>
    /// Configuration model for security headers.
    /// </summary>
    public class SecurityConfig
    {
        public string FrameOptions { get; set; }
        public string ContentSecurityPolicy { get; set; }
        public bool EnforceHsts { get; set; }
        public int HstsMaxAge { get; set; }
    }

    /// <summary>
    /// Configuration model for session validation.
    /// </summary>
    public class SessionConfig
    {
        public int MaxConcurrentSessions { get; set; }
        public int IdleTimeoutMinutes { get; set; }
        public bool ValidateClientIp { get; set; }
    }
}
