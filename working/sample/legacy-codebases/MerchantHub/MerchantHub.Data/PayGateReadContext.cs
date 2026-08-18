using System;
using System.Data.Entity;
using System.Linq;

namespace MerchantHub.Data
{
    /// <summary>
    /// Read-only EF6 context for PayGate database.
    /// Used to query transaction data from the payment gateway system.
    /// NOTE: This is READ-ONLY. Do not attempt to save changes against this context.
    ///       The mhub_readonly user only has SELECT permissions.
    /// </summary>
    public class PayGateReadContext : DbContext
    {
        public PayGateReadContext()
            : base("name=PayGateDB")
        {
            this.Configuration.LazyLoadingEnabled = false;
            this.Configuration.ProxyCreationEnabled = false;
            this.Configuration.AutoDetectChangesEnabled = false;

            // Read-only context - disable change tracking
            Database.SetInitializer<PayGateReadContext>(null);
        }

        public virtual DbSet<PayGateTransaction> PayGateTransactions { get; set; }
        public virtual DbSet<PayGateSettlement> Settlements { get; set; }
        public virtual DbSet<PayGateMerchantAccount> MerchantAccounts { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // PayGate uses different schema/table naming
            modelBuilder.Entity<PayGateTransaction>().ToTable("PG_Transactions", "dbo");
            modelBuilder.Entity<PayGateSettlement>().ToTable("PG_Settlements", "dbo");
            modelBuilder.Entity<PayGateMerchantAccount>().ToTable("PG_MerchantAccounts", "dbo");

            modelBuilder.Entity<PayGateTransaction>().HasKey(t => t.PayGateTransactionId);
            modelBuilder.Entity<PayGateSettlement>().HasKey(s => s.SettlementId);
            modelBuilder.Entity<PayGateMerchantAccount>().HasKey(a => a.AccountId);

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// Override SaveChanges to prevent accidental writes
        /// </summary>
        public override int SaveChanges()
        {
            throw new InvalidOperationException(
                "PayGateReadContext is read-only. Cannot save changes to PayGate database.");
        }
    }

    // PayGate entity models (read-only)

    public class PayGateTransaction
    {
        public long PayGateTransactionId { get; set; }
        public string MerchantAccountNumber { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string CardBrand { get; set; }
        public string CardLast4 { get; set; }
        public string AuthCode { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public DateTime ProcessedDate { get; set; }
        public DateTime? SettledDate { get; set; }
        public string BatchId { get; set; }
        public string TerminalId { get; set; }
        public string EntryMode { get; set; }
    }

    public class PayGateSettlement
    {
        public long SettlementId { get; set; }
        public string MerchantAccountNumber { get; set; }
        public string BatchId { get; set; }
        public DateTime SettlementDate { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal Fees { get; set; }
        public decimal NetAmount { get; set; }
        public int TransactionCount { get; set; }
        public string Status { get; set; }
    }

    public class PayGateMerchantAccount
    {
        public int AccountId { get; set; }
        public string MerchantAccountNumber { get; set; }
        public string BusinessName { get; set; }
        public string Status { get; set; }
        public decimal ProcessingRate { get; set; }
        public DateTime ActivatedDate { get; set; }
    }
}
