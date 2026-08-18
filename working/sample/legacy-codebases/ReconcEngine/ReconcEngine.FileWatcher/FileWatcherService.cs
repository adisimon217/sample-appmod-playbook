using System;
using System.Configuration;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using Serilog;

namespace ReconcEngine.FileWatcher
{
    /// <summary>
    /// Windows Service that monitors the settlement file share for new files.
    /// When a new settlement file arrives, it logs the event and optionally triggers
    /// the reconciliation engine via a queue mechanism.
    /// </summary>
    public class FileWatcherService : ServiceBase
    {
        private FileSystemWatcher _watcher;
        private readonly ILogger _logger;
        private readonly string _watchPath;
        private readonly string _fileFilter;
        private Timer _healthCheckTimer;

        public FileWatcherService()
        {
            ServiceName = "ReconcEngine.FileWatcher";
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;

            _logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .WriteTo.File(
                    ConfigurationManager.AppSettings["Serilog:WriteTo:File:Path"],
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.EventLog(
                    ConfigurationManager.AppSettings["Serilog:WriteTo:EventLog:Source"],
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning)
                .Enrich.WithProperty("Application", "ReconcEngine.FileWatcher")
                .CreateLogger();

            _watchPath = ConfigurationManager.AppSettings["WatchPath"];
            _fileFilter = ConfigurationManager.AppSettings["FileFilter"];
        }

        protected override void OnStart(string[] args)
        {
            _logger.Information("FileWatcher service starting. Monitoring: {Path} for {Filter}",
                _watchPath, _fileFilter);

            try
            {
                _watcher = new FileSystemWatcher(_watchPath, _fileFilter)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                    EnableRaisingEvents = true,
                    IncludeSubdirectories = false
                };

                _watcher.Created += OnFileCreated;
                _watcher.Renamed += OnFileRenamed;
                _watcher.Error += OnWatcherError;

                // Health check every 5 minutes to ensure the UNC path is still accessible
                _healthCheckTimer = new Timer(PerformHealthCheck, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));

                _logger.Information("FileWatcher service started successfully");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to start FileWatcher service");
                throw;
            }
        }

        protected override void OnStop()
        {
            _logger.Information("FileWatcher service stopping");

            _healthCheckTimer?.Dispose();

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Created -= OnFileCreated;
                _watcher.Renamed -= OnFileRenamed;
                _watcher.Error -= OnWatcherError;
                _watcher.Dispose();
            }

            _logger.Information("FileWatcher service stopped");
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            _logger.Information("New settlement file detected: {FileName}, Size: {FileSize} bytes",
                e.Name, GetFileSize(e.FullPath));

            // Wait briefly for file to be fully written
            WaitForFileReady(e.FullPath, TimeSpan.FromSeconds(30));

            NotifyReconcEngine(e.FullPath);
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            _logger.Information("Settlement file renamed: {OldName} -> {NewName}", e.OldName, e.Name);

            if (Path.GetExtension(e.Name).Equals(".pgp", StringComparison.OrdinalIgnoreCase))
            {
                NotifyReconcEngine(e.FullPath);
            }
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            var exception = e.GetException();
            _logger.Error(exception, "FileSystemWatcher error occurred. Attempting recovery.");

            // Attempt to restart the watcher
            try
            {
                _watcher.EnableRaisingEvents = false;
                Thread.Sleep(5000);
                _watcher.EnableRaisingEvents = true;
                _logger.Information("FileSystemWatcher recovered successfully");
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "FileSystemWatcher recovery failed. Service needs manual restart.");
            }
        }

        private void NotifyReconcEngine(string filePath)
        {
            try
            {
                var notificationPath = ConfigurationManager.AppSettings["NotificationQueuePath"];
                var notificationFile = Path.Combine(notificationPath,
                    $"settlement_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.notify");

                File.WriteAllText(notificationFile, filePath);
                _logger.Debug("Notification created: {NotificationFile}", notificationFile);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to create notification for file {FilePath}", filePath);
            }
        }

        private void PerformHealthCheck(object state)
        {
            try
            {
                if (!Directory.Exists(_watchPath))
                {
                    _logger.Warning("Watch path is not accessible: {Path}", _watchPath);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Health check failed for watch path: {Path}", _watchPath);
            }
        }

        private bool WaitForFileReady(string filePath, TimeSpan timeout)
        {
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < timeout)
            {
                try
                {
                    using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        return true;
                    }
                }
                catch (IOException)
                {
                    Thread.Sleep(500);
                }
            }

            _logger.Warning("File {FilePath} was not ready within timeout {Timeout}s",
                filePath, timeout.TotalSeconds);
            return false;
        }

        private long GetFileSize(string filePath)
        {
            try
            {
                return new FileInfo(filePath).Length;
            }
            catch
            {
                return -1;
            }
        }
    }
}
