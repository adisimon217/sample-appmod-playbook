using System;
using System.Configuration;
using System.Diagnostics;

namespace ComplianceReporter.Core
{
    /// <summary>
    /// Wrapper around Windows Event Log for the ComplianceReporter service.
    /// Uses EventLog.WriteEntry directly - no third party logging frameworks.
    /// 
    /// Note: The Event Log source "ComplianceReporter" must be registered before first use.
    /// This is handled by ProjectInstaller during service installation.
    /// </summary>
    public static class EventLogger
    {
        private static readonly string _source;
        private static readonly string _logName;

        static EventLogger()
        {
            _source = ConfigurationManager.AppSettings["EventLogSource"] ?? "ComplianceReporter";
            _logName = ConfigurationManager.AppSettings["EventLogName"] ?? "Application";
        }

        public static void WriteInfo(string message)
        {
            WriteEntry(message, EventLogEntryType.Information);
        }

        public static void WriteWarning(string message)
        {
            WriteEntry(message, EventLogEntryType.Warning);
        }

        public static void WriteError(string message)
        {
            WriteEntry(message, EventLogEntryType.Error);
        }

        public static void WriteError(string message, Exception ex)
        {
            string fullMessage = string.Format(
                "{0}\r\n\r\nException: {1}\r\nStack Trace: {2}\r\n\r\nInner Exception: {3}",
                message,
                ex.Message,
                ex.StackTrace,
                ex.InnerException != null ? ex.InnerException.Message : "(none)");

            WriteEntry(fullMessage, EventLogEntryType.Error);
        }

        private static void WriteEntry(string message, EventLogEntryType entryType)
        {
            try
            {
                if (!EventLog.SourceExists(_source))
                {
                    // This will fail if not running as admin - service account should have this
                    EventLog.CreateEventSource(_source, _logName);
                }

                EventLog.WriteEntry(_source, message, entryType);
            }
            catch (Exception)
            {
                // If Event Log writing fails, we can't do much - swallow the error
                // In production this might indicate a permissions issue with the service account
            }
        }
    }
}
