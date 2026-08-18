using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using log4net;
using MerchantHub.Core.Models;

namespace MerchantHub.Data.Repositories
{
    public interface IDisputeRepository
    {
        Dispute GetById(int disputeId);
        List<Dispute> GetByMerchant(int merchantId, string status, int page, int pageSize);
        int GetCountByMerchant(int merchantId, string status);
        List<Dispute> GetOpenDisputesByMerchant(int merchantId);
        List<Dispute> GetAllOpen();
        int GetPendingCount();
        List<DisputeHistoryEntry> GetDisputeHistory(int disputeId);
        List<DisputeDocument> GetDisputeDocuments(int disputeId);
        void Update(Dispute dispute);
        void AddHistory(DisputeHistoryEntry entry);
        void AddDocument(DisputeDocument document);
    }

    public class DisputeRepository : IDisputeRepository
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DisputeRepository));
        private readonly MerchantHubContext _context;

        public DisputeRepository(MerchantHubContext context)
        {
            _context = context;
        }

        public Dispute GetById(int disputeId)
        {
            return _context.Disputes
                .Include(d => d.Merchant)
                .Include(d => d.Transaction)
                .FirstOrDefault(d => d.DisputeId == disputeId);
        }

        public List<Dispute> GetByMerchant(int merchantId, string status, int page, int pageSize)
        {
            var query = _context.Disputes.Where(d => d.MerchantId == merchantId);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(d => d.Status == status);
            }

            return query
                .OrderByDescending(d => d.FiledDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public int GetCountByMerchant(int merchantId, string status)
        {
            var query = _context.Disputes.Where(d => d.MerchantId == merchantId);
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(d => d.Status == status);
            }
            return query.Count();
        }

        public List<Dispute> GetOpenDisputesByMerchant(int merchantId)
        {
            return _context.Disputes
                .Where(d => d.MerchantId == merchantId &&
                           (d.Status == "Open" || d.Status == "UnderReview"))
                .OrderByDescending(d => d.FiledDate)
                .ToList();
        }

        public List<Dispute> GetAllOpen()
        {
            return _context.Disputes
                .Where(d => d.Status == "Open" || d.Status == "UnderReview")
                .ToList();
        }

        public int GetPendingCount()
        {
            return _context.Disputes.Count(d => d.Status == "Open" || d.Status == "UnderReview");
        }

        public List<DisputeHistoryEntry> GetDisputeHistory(int disputeId)
        {
            return _context.DisputeHistory
                .Where(h => h.DisputeId == disputeId)
                .OrderByDescending(h => h.ActionDate)
                .ToList();
        }

        public List<DisputeDocument> GetDisputeDocuments(int disputeId)
        {
            return _context.DisputeDocuments
                .Where(d => d.DisputeId == disputeId)
                .OrderByDescending(d => d.UploadedDate)
                .ToList();
        }

        public void Update(Dispute dispute)
        {
            var entry = _context.Entry(dispute);
            if (entry.State == EntityState.Detached)
            {
                _context.Disputes.Attach(dispute);
                entry.State = EntityState.Modified;
            }
            _context.SaveChanges();
        }

        public void AddHistory(DisputeHistoryEntry historyEntry)
        {
            _context.DisputeHistory.Add(historyEntry);
            _context.SaveChanges();
        }

        public void AddDocument(DisputeDocument document)
        {
            _context.DisputeDocuments.Add(document);
            _context.SaveChanges();
        }
    }
}
