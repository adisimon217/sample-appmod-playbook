using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using PayGate.Core.Models;
using PayGate.Data.Repositories;
using PayGate.Integration.Sftp;

namespace PayGate.Core.Services
{
    /// <summary>
    /// Handles settlement processing with card networks (Visa/Mastercard).
    /// Daily settlement batch: collects authorized transactions, generates settlement files,
    /// encrypts with PGP, and sends via SFTP.
    /// </summary>
    public class SettlementService
    {
        private readonly SettlementRepository _settlementRepo;
        private readonly VisaSettlementClient _visaClient;
        private readonly MastercardSettlementClient _mastercardClient;

        private static readonly int BatchSize =
            int.Parse(ConfigurationManager.AppSettings["PayGate:SettlementBatchSize"] ?? "5000");

        public SettlementService(
            SettlementRepository settlementRepo,
            VisaSettlementClient visaClient,
            MastercardSettlementClient mastercardClient)
        {
            _settlementRepo = settlementRepo;
            _visaClient = visaClient;
            _mastercardClient = mastercardClient;
        }

        /// <summary>
        /// Process daily settlement for all captured transactions on the given date.
        /// </summary>
        public async Task<SettlementResult> ProcessDailySettlementAsync(DateTime settlementDate)
        {
            var result = new SettlementResult();
            var errors = new List<string>();

            try
            {
                // Get all captured transactions for the settlement date, grouped by network
                var visaTransactions = await _settlementRepo.GetUnsettledTransactionsByNetworkAsync(
                    "Visa", settlementDate);
                var mastercardTransactions = await _settlementRepo.GetUnsettledTransactionsByNetworkAsync(
                    "Mastercard", settlementDate);

                result.VisaCount = visaTransactions?.Count ?? 0;
                result.MastercardCount = mastercardTransactions?.Count ?? 0;
                result.TotalSettlementAmount = (visaTransactions?.Sum(t => t.Amount) ?? 0) +
                                               (mastercardTransactions?.Sum(t => t.Amount) ?? 0);

                // Process Visa settlement
                if (visaTransactions != null && visaTransactions.Any())
                {
                    try
                    {
                        var visaBatchId = Guid.NewGuid();
                        await _visaClient.SendSettlementFileAsync(visaTransactions, visaBatchId, settlementDate);

                        var visaSettlement = new Settlement
                        {
                            SettlementId = Guid.NewGuid(),
                            BatchId = visaBatchId,
                            Network = "VISA",
                            SettlementDate = settlementDate,
                            TotalAmount = visaTransactions.Sum(t => t.Amount),
                            TransactionCount = visaTransactions.Count,
                            Status = SettlementStatus.FileSent,
                            FileName = $"VISA_SETTLE_{settlementDate:yyyyMMdd}_{visaBatchId:N}.pgp",
                            FileGeneratedDate = DateTime.UtcNow,
                            FileSentDate = DateTime.UtcNow
                        };

                        await _settlementRepo.CreateSettlementAsync(visaSettlement);
                        result.VisaFileGenerated = true;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Visa settlement failed: {ex.Message}");
                        result.VisaFileGenerated = false;
                    }
                }

                // Process Mastercard settlement
                if (mastercardTransactions != null && mastercardTransactions.Any())
                {
                    try
                    {
                        var mcBatchId = Guid.NewGuid();
                        await _mastercardClient.SendSettlementFileAsync(
                            mastercardTransactions, mcBatchId, settlementDate);

                        var mcSettlement = new Settlement
                        {
                            SettlementId = Guid.NewGuid(),
                            BatchId = mcBatchId,
                            Network = "MASTERCARD",
                            SettlementDate = settlementDate,
                            TotalAmount = mastercardTransactions.Sum(t => t.Amount),
                            TransactionCount = mastercardTransactions.Count,
                            Status = SettlementStatus.FileSent,
                            FileName = $"MC_SETTLE_{settlementDate:yyyyMMdd}_{mcBatchId:N}.pgp",
                            FileGeneratedDate = DateTime.UtcNow,
                            FileSentDate = DateTime.UtcNow
                        };

                        await _settlementRepo.CreateSettlementAsync(mcSettlement);
                        result.MastercardFileGenerated = true;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Mastercard settlement failed: {ex.Message}");
                        result.MastercardFileGenerated = false;
                    }
                }

                result.Errors = errors;
                result.Success = !errors.Any();
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Settlement processing error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Reconcile incoming settlement response files from card networks.
        /// </summary>
        public async Task<ReconciliationResult> ReconcileAsync(string network, DateTime settlementDate)
        {
            var result = new ReconciliationResult();

            if (string.Equals(network, "VISA", StringComparison.OrdinalIgnoreCase))
            {
                var responseFile = await _visaClient.DownloadResponseFileAsync(settlementDate);
                if (responseFile != null)
                {
                    result = await _settlementRepo.ReconcileSettlementAsync(
                        "VISA", settlementDate, responseFile.MatchedTransactionIds);
                }
            }
            else if (string.Equals(network, "MASTERCARD", StringComparison.OrdinalIgnoreCase))
            {
                var responseFile = await _mastercardClient.DownloadResponseFileAsync(settlementDate);
                if (responseFile != null)
                {
                    result = await _settlementRepo.ReconcileSettlementAsync(
                        "MASTERCARD", settlementDate, responseFile.MatchedTransactionIds);
                }
            }

            return result;
        }
    }
}
