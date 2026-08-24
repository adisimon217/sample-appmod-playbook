using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using log4net;
using MerchantHub.Core.Models;
using MerchantHub.Data.Repositories;

namespace MerchantHub.Core.Services
{
    public interface IMerchantService
    {
        Merchant GetMerchant(int merchantId);
        void UpdateMerchant(Merchant merchant);
        List<Merchant> GetActiveMerchants();
        void RefreshMerchantCache();
        void PerformDailyCleanup();
        MerchantDashboardData GetDashboardData(int merchantId);
    }

    /// <summary>
    /// Merchant business logic service.
    /// NOTE: Some methods access HttpContext directly - makes unit testing difficult.
    ///       Caching is done via Application state (not distributed).
    /// </summary>
    public class MerchantService : IMerchantService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MerchantService));
        private readonly IMerchantRepository _merchantRepo;
        private readonly ITransactionRepository _transactionRepo;

        // In-memory cache (not distributed - problematic in web farm)
        private static Dictionary<int, MerchantCacheEntry> _merchantCache = new Dictionary<int, MerchantCacheEntry>();
        private static DateTime _lastCacheRefresh = DateTime.MinValue;
        private static readonly object _cacheLock = new object();

        public MerchantService(IMerchantRepository merchantRepo, ITransactionRepository transactionRepo)
        {
            _merchantRepo = merchantRepo;
            _transactionRepo = transactionRepo;
        }

        public Merchant GetMerchant(int merchantId)
        {
            // Check cache first
            lock (_cacheLock)
            {
                if (_merchantCache.ContainsKey(merchantId))
                {
                    var entry = _merchantCache[merchantId];
                    if ((DateTime.Now - entry.CachedAt).TotalMinutes < 15)
                    {
                        return entry.Merchant;
                    }
                }
            }

            var merchant = _merchantRepo.GetById(merchantId);
            if (merchant != null)
            {
                lock (_cacheLock)
                {
                    _merchantCache[merchantId] = new MerchantCacheEntry
                    {
                        Merchant = merchant,
                        CachedAt = DateTime.Now
                    };
                }
            }

            return merchant;
        }

        public void UpdateMerchant(Merchant merchant)
        {
            _merchantRepo.Update(merchant);

            // Invalidate cache
            lock (_cacheLock)
            {
                if (_merchantCache.ContainsKey(merchant.MerchantId))
                {
                    _merchantCache.Remove(merchant.MerchantId);
                }
            }
        }

        public List<Merchant> GetActiveMerchants()
        {
            return _merchantRepo.GetAll().Where(m => m.Status == "Active").ToList();
        }

        /// <summary>
        /// Hourly cache refresh job (called by Hangfire).
        /// Refreshes statistics for the top 100 merchants by volume.
        /// </summary>
        public void RefreshMerchantCache()
        {
            _log.Info("Starting hourly merchant cache refresh...");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var topMerchants = _merchantRepo.GetTopMerchantsByVolume(100);

                lock (_cacheLock)
                {
                    foreach (var merchant in topMerchants)
                    {
                        _merchantCache[merchant.MerchantId] = new MerchantCacheEntry
                        {
                            Merchant = merchant,
                            CachedAt = DateTime.Now
                        };
                    }
                    _lastCacheRefresh = DateTime.Now;
                }

                stopwatch.Stop();
                _log.InfoFormat("Merchant cache refresh completed in {0}ms. {1} merchants cached.",
                    stopwatch.ElapsedMilliseconds, topMerchants.Count);
            }
            catch (Exception ex)
            {
                _log.Error("Error during merchant cache refresh", ex);
                throw; // Let Hangfire handle retry
            }
        }

        /// <summary>
        /// Daily cleanup job (called by Hangfire at 3 AM).
        /// Purges temporary files, expired sessions data, old log entries.
        /// </summary>
        public void PerformDailyCleanup()
        {
            _log.Info("Starting daily cleanup...");

            try
            {
                // Clean up temp report files older than 7 days
                var reportPath = System.Configuration.ConfigurationManager.AppSettings["MerchantHub.ReportOutputPath"];
                if (System.IO.Directory.Exists(reportPath))
                {
                    var tempFiles = System.IO.Directory.GetFiles(reportPath, "*.tmp");
                    var deletedCount = 0;
                    foreach (var file in tempFiles)
                    {
                        var fileInfo = new System.IO.FileInfo(file);
                        if (fileInfo.CreationTime < DateTime.Now.AddDays(-7))
                        {
                            System.IO.File.Delete(file);
                            deletedCount++;
                        }
                    }
                    _log.InfoFormat("Cleaned up {0} temp files from report directory", deletedCount);
                }

                // Clean up old upload temp files
                var uploadPath = System.Configuration.ConfigurationManager.AppSettings["MerchantHub.UploadPath"];
                if (System.IO.Directory.Exists(uploadPath))
                {
                    var tempDir = System.IO.Path.Combine(uploadPath, "temp");
                    if (System.IO.Directory.Exists(tempDir))
                    {
                        var oldFiles = System.IO.Directory.GetFiles(tempDir)
                            .Where(f => new System.IO.FileInfo(f).CreationTime < DateTime.Now.AddDays(-1));
                        foreach (var file in oldFiles)
                        {
                            System.IO.File.Delete(file);
                        }
                    }
                }

                _log.Info("Daily cleanup completed successfully");
            }
            catch (Exception ex)
            {
                _log.Error("Error during daily cleanup", ex);
                throw;
            }
        }

        public MerchantDashboardData GetDashboardData(int merchantId)
        {
            // This method was added later but the controller still calculates everything inline
            // TODO: Move dashboard logic from MerchantController here (JIRA-4521)
            throw new NotImplementedException("Dashboard logic is still in controller");
        }

        private class MerchantCacheEntry
        {
            public Merchant Merchant { get; set; }
            public DateTime CachedAt { get; set; }
        }
    }

    public class MerchantDashboardData
    {
        public decimal MonthlyVolume { get; set; }
        public int MonthlyCount { get; set; }
        public decimal TodayVolume { get; set; }
        public decimal ApprovalRate { get; set; }
        public int OpenDisputeCount { get; set; }
        public decimal ChargebackRatio { get; set; }
    }
}
