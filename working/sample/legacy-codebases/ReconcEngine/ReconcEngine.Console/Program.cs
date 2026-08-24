using System;
using System.Configuration;
using Serilog;
using ReconcEngine.Core.Data;
using ReconcEngine.Core.Engine;
using ReconcEngine.Core.FileProcessing;
using ReconcEngine.Core.Analysis;
using ReconcEngine.Core.Reporting;
using ReconcEngine.Core.Interfaces;

namespace ReconcEngine.Console
{
    /// <summary>
    /// Entry point for the ReconcEngine nightly batch reconciliation.
    /// Triggered by Windows Task Scheduler at 11 PM SGT.
    /// Service Account: svc_recon@voyager.local
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            // Configure Serilog from app settings
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .WriteTo.File(
                    ConfigurationManager.AppSettings["Serilog:WriteTo:File:Path"],
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.EventLog(
                    ConfigurationManager.AppSettings["Serilog:WriteTo:EventLog:Source"],
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning)
                .Enrich.WithProperty("Application", "ReconcEngine")
                .Enrich.WithProperty("Environment", "Production")
                .CreateLogger();

            try
            {
                Log.Information("ReconcEngine starting. Version {Version}, RunTime {RunTime}",
                    typeof(Program).Assembly.GetName().Version,
                    DateTime.UtcNow);

                // Manual dependency injection composition root
                var reconcDbConnectionString = ConfigurationManager.ConnectionStrings["ReconcDB"].ConnectionString;
                var payGateDbConnectionString = ConfigurationManager.ConnectionStrings["PayGateDB"].ConnectionString;

                var settlementFilePath = ConfigurationManager.AppSettings["SettlementFilePath"];
                var reportOutputPath = ConfigurationManager.AppSettings["ReportOutputPath"];
                var pgpPrivateKeyPath = ConfigurationManager.AppSettings["PgpPrivateKeyPath"];
                var pgpPassphrase = ConfigurationManager.AppSettings["PgpPassphrase"];
                var toleranceAmount = decimal.Parse(ConfigurationManager.AppSettings["ReconciliationToleranceAmount"]);
                var batchSizeLimit = int.Parse(ConfigurationManager.AppSettings["BatchSizeLimit"]);

                // Compose object graph
                IReconcRepository reconcRepository = new ReconcRepository(reconcDbConnectionString);
                ITransactionRepository transactionRepository = new TransactionRepository(payGateDbConnectionString);
                IBatchRunRepository batchRunRepository = new BatchRunRepository(reconcDbConnectionString);

                IPgpDecryptor pgpDecryptor = new PgpDecryptor(pgpPrivateKeyPath, pgpPassphrase);
                IFileFormatDetector fileFormatDetector = new FileFormatDetector();
                ISettlementFileParser settlementFileParser = new SettlementFileParser(pgpDecryptor, fileFormatDetector);

                ITransactionMatcher transactionMatcher = new TransactionMatcher(toleranceAmount);
                IDiscrepancyDetector discrepancyDetector = new DiscrepancyDetector(toleranceAmount);
                IReportGenerator reportGenerator = new CsvReportGenerator(reportOutputPath);

                IReconciliationEngine engine = new ReconciliationEngine(
                    transactionMatcher,
                    discrepancyDetector,
                    reconcRepository,
                    transactionRepository,
                    batchRunRepository,
                    Log.Logger);

                var orchestrator = new ReconciliationOrchestrator(
                    engine,
                    settlementFileParser,
                    reportGenerator,
                    batchRunRepository,
                    settlementFilePath,
                    batchSizeLimit,
                    Log.Logger);

                // Execute reconciliation
                var result = orchestrator.RunAsync().GetAwaiter().GetResult();

                if (result.Success)
                {
                    Log.Information("Reconciliation completed successfully. " +
                        "Matched: {MatchedCount}, Discrepancies: {DiscrepancyCount}, " +
                        "Duration: {Duration}ms",
                        result.MatchedCount, result.DiscrepancyCount,
                        result.Duration.TotalMilliseconds);
                    return 0;
                }
                else
                {
                    Log.Error("Reconciliation completed with errors. " +
                        "Matched: {MatchedCount}, Failed: {FailedCount}, " +
                        "Error: {ErrorMessage}",
                        result.MatchedCount, result.FailedCount,
                        result.ErrorMessage);
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "ReconcEngine terminated unexpectedly");
                return 2;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
