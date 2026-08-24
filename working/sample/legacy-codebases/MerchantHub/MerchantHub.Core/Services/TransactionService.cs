using System;
using System.Configuration;
using System.Data.SqlClient;
using log4net;
using MerchantHub.Core.Models;
using MerchantHub.Data;
using MerchantHub.Data.Repositories;

namespace MerchantHub.Core.Services
{
    public interface ITransactionService
    {
        RefundResult ProcessRefund(long transactionId, decimal amount, string reason, string processedBy);
        void RunWeeklyReconciliation();
        Transaction GetTransaction(long transactionId);
    }

    /// <summary>
    /// Transaction processing service.
    /// Contains business rules for refunds, voids, and reconciliation.
    /// </summary>
    public class TransactionService : ITransactionService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(TransactionService));
        private readonly ITransactionRepository _transactionRepo;
        private readonly PayGateReadContext _payGateContext;

        public TransactionService(ITransactionRepository transactionRepo, PayGateReadContext payGateContext)
        {
            _transactionRepo = transactionRepo;
            _payGateContext = payGateContext;
        }

        public Transaction GetTransaction(long transactionId)
        {
            return _transactionRepo.GetById(transactionId);
        }

        /// <summary>
        /// Process a refund for a given transaction.
        /// Business rules:
        /// - Transaction must be in Approved status
        /// - Refund amount cannot exceed original amount
        /// - Transactions older than 120 days cannot be refunded
        /// - Partial refunds allowed (up to remaining amount)
        /// </summary>
        public RefundResult ProcessRefund(long transactionId, decimal amount, string reason, string processedBy)
        {
            _log.InfoFormat("Processing refund: TxnId={0}, Amount={1:C}, By={2}", transactionId, amount, processedBy);

            try
            {
                var transaction = _transactionRepo.GetById(transactionId);
                if (transaction == null)
                {
                    return new RefundResult { Success = false, ErrorMessage = "Transaction not found" };
                }

                // Business rules validation
                if (transaction.Status != "Approved" && transaction.Status != "PartialRefund")
                {
                    return new RefundResult { Success = false, ErrorMessage = "Transaction is not in a refundable state" };
                }

                var remainingAmount = transaction.Amount - (transaction.RefundAmount ?? 0);
                if (amount > remainingAmount)
                {
                    return new RefundResult
                    {
                        Success = false,
                        ErrorMessage = string.Format("Refund amount ({0:C}) exceeds remaining refundable amount ({1:C})",
                            amount, remainingAmount)
                    };
                }

                if ((DateTime.Now - transaction.TransactionDate).TotalDays > 120)
                {
                    return new RefundResult { Success = false, ErrorMessage = "Transaction is past the 120-day refund window" };
                }

                // Process the refund - update transaction record
                var refundRef = "RF" + DateTime.Now.ToString("yyyyMMddHHmmss") + transactionId.ToString().PadLeft(6, '0');
                var newRefundTotal = (transaction.RefundAmount ?? 0) + amount;

                // Direct SQL for the refund update (mixed pattern - sometimes uses EF, sometimes raw SQL)
                var connectionString = ConfigurationManager.ConnectionStrings["MerchantHubDB"].ConnectionString;
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(@"
                        UPDATE MH_Transactions 
                        SET RefundAmount = @RefundAmount,
                            Status = CASE WHEN @RefundAmount >= Amount THEN 'Refunded' ELSE 'PartialRefund' END,
                            ModifiedDate = GETDATE(),
                            ModifiedBy = @ProcessedBy
                        WHERE TransactionId = @TransactionId;

                        INSERT INTO MH_Refunds (TransactionId, RefundAmount, ReferenceNumber, Reason, ProcessedBy, ProcessedDate)
                        VALUES (@TransactionId, @Amount, @RefundRef, @Reason, @ProcessedBy, GETDATE());

                        INSERT INTO MH_AuditLog (EntityType, EntityId, Action, UserId, Details, CreatedDate)
                        VALUES ('Transaction', @TransactionId, 'Refund', @ProcessedBy, @Details, GETDATE());
                    ", conn))
                    {
                        cmd.Parameters.AddWithValue("@TransactionId", transactionId);
                        cmd.Parameters.AddWithValue("@RefundAmount", newRefundTotal);
                        cmd.Parameters.AddWithValue("@Amount", amount);
                        cmd.Parameters.AddWithValue("@RefundRef", refundRef);
                        cmd.Parameters.AddWithValue("@Reason", reason ?? "");
                        cmd.Parameters.AddWithValue("@ProcessedBy", processedBy);
                        cmd.Parameters.AddWithValue("@Details",
                            string.Format("Refund {0:C} processed. Ref: {1}. Reason: {2}", amount, refundRef, reason));
                        cmd.ExecuteNonQuery();
                    }
                }

                _log.InfoFormat("Refund processed successfully: TxnId={0}, RefundRef={1}, Amount={2:C}",
                    transactionId, refundRef, amount);

                return new RefundResult
                {
                    Success = true,
                    ReferenceNumber = refundRef
                };
            }
            catch (Exception ex)
            {
                _log.Error("Error processing refund for transaction " + transactionId, ex);
                return new RefundResult { Success = false, ErrorMessage = "Internal error processing refund" };
            }
        }

        /// <summary>
        /// Weekly reconciliation job (called by Hangfire every Monday at 4 AM).
        /// Compares MerchantHub transactions with PayGate records.
        /// </summary>
        public void RunWeeklyReconciliation()
        {
            _log.Info("Starting weekly transaction reconciliation...");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var startDate = DateTime.Today.AddDays(-7);
                var endDate = DateTime.Today;

                // Get our transactions
                var ourTransactions = _transactionRepo.GetByDateRange(startDate, endDate);
                _log.InfoFormat("Found {0} transactions in MerchantHub for reconciliation period", ourTransactions.Count);

                // Compare with PayGate
                var payGateTransactions = _payGateContext.PayGateTransactions
                    .Where(t => t.ProcessedDate >= startDate && t.ProcessedDate < endDate)
                    .ToList();
                _log.InfoFormat("Found {0} transactions in PayGate for reconciliation period", payGateTransactions.Count);

                int mismatches = 0;
                int missing = 0;

                foreach (var ourTxn in ourTransactions)
                {
                    if (ourTxn.PayGateTransactionId.HasValue)
                    {
                        var pgTxn = payGateTransactions.FirstOrDefault(
                            t => t.PayGateTransactionId == ourTxn.PayGateTransactionId);
                        if (pgTxn == null)
                        {
                            missing++;
                            _log.WarnFormat("Transaction {0} references PayGate ID {1} which was not found",
                                ourTxn.TransactionId, ourTxn.PayGateTransactionId);
                        }
                        else if (pgTxn.Amount != ourTxn.Amount)
                        {
                            mismatches++;
                            _log.WarnFormat("Amount mismatch for TxnId {0}: MerchantHub={1:C}, PayGate={2:C}",
                                ourTxn.TransactionId, ourTxn.Amount, pgTxn.Amount);
                        }
                    }
                }

                stopwatch.Stop();
                _log.InfoFormat("Weekly reconciliation completed in {0}ms. Mismatches: {1}, Missing: {2}",
                    stopwatch.ElapsedMilliseconds, mismatches, missing);

                if (mismatches > 0 || missing > 0)
                {
                    _log.WarnFormat("Reconciliation issues found: {0} mismatches, {1} missing. Review required.",
                        mismatches, missing);
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error during weekly reconciliation", ex);
                throw;
            }
        }
    }
}
