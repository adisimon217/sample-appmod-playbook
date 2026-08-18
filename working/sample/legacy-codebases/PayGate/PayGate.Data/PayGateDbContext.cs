using System.Data.Entity;
using PayGate.Core.Models;

namespace PayGate.Data
{
    /// <summary>
    /// Entity Framework 6 DbContext for PaymentsDB.
    /// Used for non-performance-critical queries (merchant lookups, reporting).
    /// 
    /// NOTE: For high-throughput transaction inserts, use TransactionRepository
    /// with raw ADO.NET/SqlBulkCopy instead. EF overhead is too high for 
    /// our 50K/hr transaction volume target.
    /// </summary>
    public class PayGateDbContext : DbContext
    {
        public PayGateDbContext() : base("name=PaymentsDB")
        {
            // Disable lazy loading for API scenarios
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;

            // Disable automatic migrations in production
            Database.SetInitializer<PayGateDbContext>(null);
        }

        public PayGateDbContext(string connectionString) : base(connectionString)
        {
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
            Database.SetInitializer<PayGateDbContext>(null);
        }

        public virtual DbSet<Transaction> Transactions { get; set; }
        public virtual DbSet<Settlement> Settlements { get; set; }
        public virtual DbSet<Merchant> Merchants { get; set; }
        public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Transactions table mapping
            modelBuilder.Entity<Transaction>()
                .ToTable("Transactions", "dbo")
                .HasKey(t => t.TransactionId);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.ProcessingFee)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transaction>()
                .HasOptional(t => t.Merchant)
                .WithMany(m => m.Transactions)
                .HasForeignKey(t => t.MerchantId);

            // Settlements table mapping
            modelBuilder.Entity<Settlement>()
                .ToTable("Settlements", "dbo")
                .HasKey(s => s.SettlementId);

            modelBuilder.Entity<Settlement>()
                .Property(s => s.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Settlement>()
                .Property(s => s.ChargebackAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Settlement>()
                .Property(s => s.NetSettlementAmount)
                .HasPrecision(18, 2);

            // Merchants table mapping
            modelBuilder.Entity<Merchant>()
                .ToTable("Merchants", "dbo")
                .HasKey(m => m.MerchantId);

            modelBuilder.Entity<Merchant>()
                .Property(m => m.ProcessingFeePercent)
                .HasPrecision(5, 4);

            modelBuilder.Entity<Merchant>()
                .Property(m => m.MonthlyMinimumFee)
                .HasPrecision(18, 2);

            // PaymentMethods table mapping
            modelBuilder.Entity<PaymentMethod>()
                .ToTable("PaymentMethods", "dbo")
                .HasKey(p => p.PaymentMethodId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
