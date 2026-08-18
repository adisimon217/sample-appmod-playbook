using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using log4net;
using MerchantHub.Core.Models;

namespace MerchantHub.Data.Repositories
{
    public interface IMerchantRepository
    {
        Merchant GetById(int merchantId);
        List<Merchant> GetAll();
        List<Merchant> Search(string query, string status, int page, int pageSize);
        int SearchCount(string query, string status);
        List<Merchant> GetTopMerchantsByVolume(int count);
        List<Merchant> GetHighChargebackMerchants(decimal threshold);
        int GetActiveCount();
        void Update(Merchant merchant);
        void AddDocument(int merchantId, string documentType, string fileName, string filePath, int fileSize);
        MerchantUser GetUserByUsername(string username);
        MerchantUser GetUserByEmail(string email);
        void UpdateLastLogin(int userId, string ipAddress);
        void SetPasswordResetToken(int userId, string token, DateTime expiry);
        object GetNotificationPreferences(int merchantId);
        object GetApiKeys(int merchantId);
        void AddApiKey(int merchantId, string keyName, string apiKey, string createdBy);
    }

    public class MerchantRepository : IMerchantRepository
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MerchantRepository));
        private readonly MerchantHubContext _context;

        public MerchantRepository(MerchantHubContext context)
        {
            _context = context;
        }

        public Merchant GetById(int merchantId)
        {
            return _context.Merchants.Find(merchantId);
        }

        public List<Merchant> GetAll()
        {
            return _context.Merchants.ToList();
        }

        public List<Merchant> Search(string query, string status, int page, int pageSize)
        {
            var queryable = _context.Merchants.AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                queryable = queryable.Where(m =>
                    m.BusinessName.Contains(query) ||
                    m.MerchantNumber.Contains(query) ||
                    m.Email.Contains(query) ||
                    m.DBA.Contains(query));
            }

            if (!string.IsNullOrEmpty(status))
            {
                queryable = queryable.Where(m => m.Status == status);
            }

            return queryable
                .OrderBy(m => m.BusinessName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public int SearchCount(string query, string status)
        {
            var queryable = _context.Merchants.AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                queryable = queryable.Where(m =>
                    m.BusinessName.Contains(query) ||
                    m.MerchantNumber.Contains(query) ||
                    m.Email.Contains(query));
            }

            if (!string.IsNullOrEmpty(status))
            {
                queryable = queryable.Where(m => m.Status == status);
            }

            return queryable.Count();
        }

        public List<Merchant> GetTopMerchantsByVolume(int count)
        {
            // TODO: This query is slow - needs optimization (JIRA-4102)
            return _context.Merchants
                .Where(m => m.Status == "Active")
                .OrderByDescending(m => m.Transactions.Count())
                .Take(count)
                .ToList();
        }

        public List<Merchant> GetHighChargebackMerchants(decimal threshold)
        {
            // Direct SQL because the percentage calculation is complex in LINQ
            return _context.Database.SqlQuery<Merchant>(@"
                SELECT m.* FROM MH_Merchants m
                INNER JOIN (
                    SELECT MerchantId,
                        CAST(SUM(CASE WHEN Status = 'Chargeback' THEN 1 ELSE 0 END) AS DECIMAL) / 
                        NULLIF(COUNT(*), 0) * 100 as ChargebackPct
                    FROM MH_Transactions
                    WHERE TransactionDate >= DATEADD(MONTH, -1, GETDATE())
                    GROUP BY MerchantId
                    HAVING CAST(SUM(CASE WHEN Status = 'Chargeback' THEN 1 ELSE 0 END) AS DECIMAL) / 
                           NULLIF(COUNT(*), 0) * 100 > @p0
                ) cb ON m.MerchantId = cb.MerchantId
                WHERE m.Status = 'Active'
                ORDER BY cb.ChargebackPct DESC", threshold).ToList();
        }

        public int GetActiveCount()
        {
            return _context.Merchants.Count(m => m.Status == "Active");
        }

        public void Update(Merchant merchant)
        {
            var entry = _context.Entry(merchant);
            if (entry.State == EntityState.Detached)
            {
                _context.Merchants.Attach(merchant);
                entry.State = EntityState.Modified;
            }
            _context.SaveChanges();
        }

        public void AddDocument(int merchantId, string documentType, string fileName, string filePath, int fileSize)
        {
            _context.Database.ExecuteSqlCommand(
                @"INSERT INTO MH_MerchantDocuments (MerchantId, DocumentType, FileName, FilePath, FileSize, UploadedDate)
                  VALUES (@p0, @p1, @p2, @p3, @p4, GETDATE())",
                merchantId, documentType, fileName, filePath, fileSize);
        }

        public MerchantUser GetUserByUsername(string username)
        {
            return _context.Users
                .Include(u => u.Merchant)
                .FirstOrDefault(u => u.Username == username);
        }

        public MerchantUser GetUserByEmail(string email)
        {
            return _context.Users.FirstOrDefault(u => u.Email == email);
        }

        public void UpdateLastLogin(int userId, string ipAddress)
        {
            _context.Database.ExecuteSqlCommand(
                @"UPDATE MH_Users SET LastLoginDate = GETDATE(), LastLoginIP = @p1, FailedLoginAttempts = 0 
                  WHERE UserId = @p0", userId, ipAddress);
        }

        public void SetPasswordResetToken(int userId, string token, DateTime expiry)
        {
            _context.Database.ExecuteSqlCommand(
                @"UPDATE MH_Users SET PasswordResetToken = @p1, PasswordResetExpiry = @p2 
                  WHERE UserId = @p0", userId, token, expiry);
        }

        public object GetNotificationPreferences(int merchantId)
        {
            return _context.Database.SqlQuery<dynamic>(
                "SELECT * FROM MH_NotificationPreferences WHERE MerchantId = @p0", merchantId).ToList();
        }

        public object GetApiKeys(int merchantId)
        {
            return _context.Database.SqlQuery<dynamic>(
                "SELECT KeyId, KeyName, LEFT(ApiKey, 7) + '...' as MaskedKey, CreatedDate, LastUsedDate, IsActive FROM MH_ApiKeys WHERE MerchantId = @p0", merchantId).ToList();
        }

        public void AddApiKey(int merchantId, string keyName, string apiKey, string createdBy)
        {
            _context.Database.ExecuteSqlCommand(
                @"INSERT INTO MH_ApiKeys (MerchantId, KeyName, ApiKey, CreatedBy, CreatedDate, IsActive)
                  VALUES (@p0, @p1, @p2, @p3, GETDATE(), 1)",
                merchantId, keyName, apiKey, createdBy);
        }
    }
}
