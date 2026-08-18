using System;
using System.Configuration;
using System.Diagnostics;
using System.ServiceProcess;
using System.Timers;
using ComplianceReporter.Core;
using ComplianceReporter.Core.ReportGeneration;
using ComplianceReporter.Data;

namespace ComplianceReporter.Service
{
    /// <summary>
    /// Windows Service that runs weekly (Sunday 02:00 SGT) to generate
    /// MAS regulatory compliance reports and upload them via SFTP.
    /// 
    /// WARNING: This service has been running since 2014 without issues.
    /// Do not refactor. Do not upgrade. Do not touch unless absolutely necessary.
    /// Last modified: 2019-03-15 (added quarterly report)
    /// </summary>
    public partial class ComplianceService : ServiceBase
    {
        private Timer _timer;
        private readonly string _scheduleDayOfWeek;
        private readonly int _scheduleHour;
        private readonly int _scheduleMinute;
        private bool _isRunning;

        public ComplianceService()
        {
            this.ServiceName = ConfigurationManager.AppSettings["ServiceName"] ?? "VoyagerComplianceReporter";
            this.CanStop = true;
            this.CanPauseAndContinue = false;
            this.AutoLog = true;

            _scheduleDayOfWeek = ConfigurationManager.AppSettings["ScheduleDayOfWeek"] ?? "Sunday";
            _scheduleHour = int.Parse(ConfigurationManager.AppSettings["ScheduleHour"] ?? "2");
            _scheduleMinute = int.Parse(ConfigurationManager.AppSettings["ScheduleMinute"] ?? "0");
            _isRunning = false;
        }

        protected override void OnStart(string[] args)
        {
            EventLogger.WriteInfo("ComplianceReporter service starting...");

            try
            {
                int timerInterval = int.Parse(
                    ConfigurationManager.AppSettings["TimerIntervalMs"] ?? "60000");

                _timer = new Timer(timerInterval);
                _timer.Elapsed += new ElapsedEventHandler(OnTimerElapsed);
                _timer.Start();

                EventLogger.WriteInfo(
                    string.Format("ComplianceReporter service started. Schedule: {0} at {1:D2}:{2:D2}",
                        _scheduleDayOfWeek, _scheduleHour, _scheduleMinute));
            }
            catch (Exception ex)
            {
                EventLogger.WriteError("Failed to start ComplianceReporter service: " + ex.Message);
                throw;
            }
        }

        protected override void OnStop()
        {
            EventLogger.WriteInfo("ComplianceReporter service stopping...");

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }

            EventLogger.WriteInfo("ComplianceReporter service stopped.");
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Check if it's the scheduled time to run
            if (!IsScheduledTime())
                return;

            // Prevent overlapping runs
            if (_isRunning)
            {
                EventLogger.WriteWarning("Previous report generation still in progress. Skipping this cycle.");
                return;
            }

            _isRunning = true;

            try
            {
                EventLogger.WriteInfo("Starting scheduled compliance report generation...");
                ExecuteReportGeneration();
                EventLogger.WriteInfo("Compliance report generation completed successfully.");
            }
            catch (Exception ex)
            {
                EventLogger.WriteError(
                    string.Format("CRITICAL: Compliance report generation failed. Error: {0}\r\nStack: {1}",
                        ex.Message, ex.StackTrace));
            }
            finally
            {
                _isRunning = false;
            }
        }

        private bool IsScheduledTime()
        {
            DateTime now = DateTime.Now;

            // Check day of week
            DayOfWeek targetDay;
            if (!Enum.TryParse<DayOfWeek>(_scheduleDayOfWeek, true, out targetDay))
            {
                targetDay = DayOfWeek.Sunday;
            }

            if (now.DayOfWeek != targetDay)
                return false;

            // Check hour and minute (within the timer interval window)
            if (now.Hour != _scheduleHour)
                return false;

            if (now.Minute != _scheduleMinute)
                return false;

            return true;
        }

        private void ExecuteReportGeneration()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["PayGateDB"].ConnectionString;
            string linkedServer = ConfigurationManager.AppSettings["LinkedServerName"];
            string linkedDatabase = ConfigurationManager.AppSettings["LinkedServerDatabase"];
            string outputPath = ConfigurationManager.AppSettings["ReportOutputPath"];
            string archivePath = ConfigurationManager.AppSettings["ReportArchivePath"];

            // Determine which reports to generate based on current date
            DateTime reportDate = DateTime.Now;
            bool isMonthEnd = reportDate.Day >= 28;
            bool isQuarterEnd = (reportDate.Month % 3 == 0) && isMonthEnd;
            bool isYearEnd = (reportDate.Month == 12) && isMonthEnd;

            // Initialize data access
            PayGateDataAccess dataAccess = new PayGateDataAccess(connectionString, linkedServer, linkedDatabase);
            MasReportGenerator reportGenerator = new MasReportGenerator(dataAccess, outputPath);

            // Always generate monthly transaction summary
            EventLogger.WriteInfo("Generating Monthly Transaction Summary report...");
            string monthlyReportPath = reportGenerator.GenerateMonthlyTransactionSummary(reportDate);
            EventLogger.WriteInfo("Monthly report generated: " + monthlyReportPath);

            // Generate quarterly compliance report if applicable
            string quarterlyReportPath = null;
            if (isQuarterEnd)
            {
                EventLogger.WriteInfo("Generating Quarterly Compliance report...");
                quarterlyReportPath = reportGenerator.GenerateQuarterlyComplianceReport(reportDate);
                EventLogger.WriteInfo("Quarterly report generated: " + quarterlyReportPath);
            }

            // Generate annual audit report if applicable
            string annualReportPath = null;
            if (isYearEnd)
            {
                EventLogger.WriteInfo("Generating Annual Audit report...");
                annualReportPath = reportGenerator.GenerateAnnualAuditReport(reportDate);
                EventLogger.WriteInfo("Annual report generated: " + annualReportPath);
            }

            // Upload reports via SFTP to MAS
            UploadReportsToMas(monthlyReportPath, quarterlyReportPath, annualReportPath);

            // Archive reports
            ArchiveReports(outputPath, archivePath);
        }

        private void UploadReportsToMas(string monthlyPath, string quarterlyPath, string annualPath)
        {
            string sftpHost = ConfigurationManager.AppSettings["SftpHost"];
            int sftpPort = int.Parse(ConfigurationManager.AppSettings["SftpPort"] ?? "22");
            string sftpUser = ConfigurationManager.AppSettings["SftpUsername"];
            string sftpPassword = ConfigurationManager.AppSettings["SftpPassword"];
            string sftpRemotePath = ConfigurationManager.AppSettings["SftpRemotePath"];
            int sftpTimeout = int.Parse(ConfigurationManager.AppSettings["SftpTimeoutSeconds"] ?? "120");

            SftpUploader uploader = new SftpUploader(sftpHost, sftpPort, sftpUser, sftpPassword, sftpTimeout);

            EventLogger.WriteInfo("Uploading reports to MAS SFTP endpoint: " + sftpHost);

            uploader.UploadFile(monthlyPath, sftpRemotePath);

            if (quarterlyPath != null)
            {
                uploader.UploadFile(quarterlyPath, sftpRemotePath);
            }

            if (annualPath != null)
            {
                uploader.UploadFile(annualPath, sftpRemotePath);
            }

            EventLogger.WriteInfo("All reports uploaded successfully to MAS.");
        }

        private void ArchiveReports(string outputPath, string archivePath)
        {
            try
            {
                // Move generated reports to archive with date-stamped folder
                string archiveFolder = System.IO.Path.Combine(archivePath,
                    DateTime.Now.ToString("yyyy-MM-dd"));

                if (!System.IO.Directory.Exists(archiveFolder))
                {
                    System.IO.Directory.CreateDirectory(archiveFolder);
                }

                foreach (string file in System.IO.Directory.GetFiles(outputPath, "*.pdf"))
                {
                    string destFile = System.IO.Path.Combine(archiveFolder,
                        System.IO.Path.GetFileName(file));
                    System.IO.File.Move(file, destFile);
                }

                EventLogger.WriteInfo("Reports archived to: " + archiveFolder);

                // Clean up old archives based on retention policy
                int retentionDays = int.Parse(
                    ConfigurationManager.AppSettings["ReportRetentionDays"] ?? "2555");
                CleanupOldArchives(archivePath, retentionDays);
            }
            catch (Exception ex)
            {
                // Archive failure is non-critical - log but don't fail the service
                EventLogger.WriteWarning("Failed to archive reports: " + ex.Message);
            }
        }

        private void CleanupOldArchives(string archivePath, int retentionDays)
        {
            DateTime cutoff = DateTime.Now.AddDays(-retentionDays);

            foreach (string dir in System.IO.Directory.GetDirectories(archivePath))
            {
                System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(dir);
                if (dirInfo.CreationTime < cutoff)
                {
                    dirInfo.Delete(true);
                    EventLogger.WriteInfo("Deleted expired archive: " + dir);
                }
            }
        }
    }
}
