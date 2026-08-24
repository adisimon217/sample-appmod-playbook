using System;
using System.Collections.Generic;
using log4net;
using MerchantHub.Core.Models;
using MerchantHub.Data.Repositories;

namespace MerchantHub.Core.Services
{
    public interface IDisputeService
    {
        Dispute GetDispute(int disputeId);
        List<Dispute> GetOpenDisputes(int merchantId);
        bool SubmitResponse(int disputeId, string responseText, List<string> evidenceFiles, string submittedBy);
        void CheckExpiredDisputes();
    }

    public class DisputeService : IDisputeService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DisputeService));
        private readonly IDisputeRepository _disputeRepo;
        private readonly ITransactionRepository _transactionRepo;

        public DisputeService(IDisputeRepository disputeRepo, ITransactionRepository transactionRepo)
        {
            _disputeRepo = disputeRepo;
            _transactionRepo = transactionRepo;
        }

        public Dispute GetDispute(int disputeId)
        {
            return _disputeRepo.GetById(disputeId);
        }

        public List<Dispute> GetOpenDisputes(int merchantId)
        {
            return _disputeRepo.GetOpenDisputesByMerchant(merchantId);
        }

        /// <summary>
        /// Submit merchant response to a dispute/chargeback.
        /// </summary>
        public bool SubmitResponse(int disputeId, string responseText, List<string> evidenceFiles, string submittedBy)
        {
            try
            {
                var dispute = _disputeRepo.GetById(disputeId);
                if (dispute == null)
                {
                    _log.WarnFormat("Dispute not found: {0}", disputeId);
                    return false;
                }

                if (dispute.Status != "Open" && dispute.Status != "UnderReview")
                {
                    _log.WarnFormat("Cannot respond to dispute {0} in status {1}", disputeId, dispute.Status);
                    return false;
                }

                if (dispute.ResponseDeadline < DateTime.Now)
                {
                    _log.WarnFormat("Response deadline passed for dispute {0}", disputeId);
                    return false;
                }

                // Update dispute
                dispute.MerchantResponse = responseText;
                dispute.Status = "Responded";
                dispute.RespondedDate = DateTime.Now;

                _disputeRepo.Update(dispute);

                // Add history entry
                _disputeRepo.AddHistory(new DisputeHistoryEntry
                {
                    DisputeId = disputeId,
                    Action = "MerchantResponse",
                    Details = "Response submitted with " + (evidenceFiles?.Count ?? 0) + " evidence file(s)",
                    PerformedBy = submittedBy,
                    ActionDate = DateTime.Now
                });

                // Add evidence documents
                if (evidenceFiles != null)
                {
                    foreach (var filePath in evidenceFiles)
                    {
                        _disputeRepo.AddDocument(new DisputeDocument
                        {
                            DisputeId = disputeId,
                            FileName = System.IO.Path.GetFileName(filePath),
                            FilePath = filePath,
                            DocumentType = "Evidence",
                            FileSize = new System.IO.FileInfo(filePath).Length,
                            UploadedDate = DateTime.Now,
                            UploadedBy = submittedBy
                        });
                    }
                }

                _log.InfoFormat("Dispute {0} response submitted by {1}", disputeId, submittedBy);
                return true;
            }
            catch (Exception ex)
            {
                _log.Error("Error submitting dispute response for " + disputeId, ex);
                return false;
            }
        }

        /// <summary>
        /// Check for disputes past their response deadline and mark as expired.
        /// Called daily by Hangfire.
        /// </summary>
        public void CheckExpiredDisputes()
        {
            _log.Info("Checking for expired disputes...");

            var openDisputes = _disputeRepo.GetAllOpen();
            int expiredCount = 0;

            foreach (var dispute in openDisputes)
            {
                if (dispute.ResponseDeadline < DateTime.Now && dispute.Status == "Open")
                {
                    dispute.Status = "Expired";
                    dispute.ResolvedDate = DateTime.Now;
                    dispute.Resolution = "Expired - no merchant response";
                    _disputeRepo.Update(dispute);

                    _disputeRepo.AddHistory(new DisputeHistoryEntry
                    {
                        DisputeId = dispute.DisputeId,
                        Action = "Expired",
                        Details = "Response deadline passed without merchant response",
                        PerformedBy = "SYSTEM",
                        ActionDate = DateTime.Now
                    });

                    expiredCount++;
                }
            }

            if (expiredCount > 0)
            {
                _log.WarnFormat("{0} disputes expired due to no response", expiredCount);
            }
        }
    }
}
