using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration.Conventions;
using MerchantHub.Core.Models;

namespace MerchantHub.Data
{
    /// <summary>
    /// Primary EF6 DbContext for MerchantHub database.
    /// Generated from EDMX (Database First) then manually customized.
    /// WARNING: Do not run migrations against this - schema is managed by DBA team via scripts.
    /// </summary>
    [DbConfigurationType(typeof(MerchantHubDbConfiguration))]
    public class MerchantHubContext : DbContext
    {
        public MerchantHubContext()
            : base("name=MerchantHubDB")
        {
            // Disable lazy loading for performance
            this.Configuration.LazyLoadingEnabled = false;
            this.Configuration.ProxyCreationEnabled = false;

            // Set command timeout for long-running reports
            ((IObjectContextAdapter)this).ObjectContext.CommandTimeout = 120;
        }

        public virtual DbSet<Merchant> Merchants { get; set; }
        public virtual DbSet<MerchantUser> Users { get; set; }
        public virtual DbSet<Transaction> Transactions { get; set; }
        public virtual DbSet<Dispute> Disputes { get; set; }
        public virtual DbSet<DisputeHistoryEntry> DisputeHistory { get; set; }
        public virtual DbSet<DisputeDocument> DisputeDocuments { get; set; }
        public virtual DbSet<MonthlyStatement> MonthlyStatements { get; set; }
        public virtual DbSet<GeneratedReport> GeneratedReports { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            // Table mappings (match existing database schema)
            modelBuilder.Entity<Merchant>().ToTable("MH_Merchants");
            modelBuilder.Entity<MerchantUser>().ToTable("MH_Users");
            modelBuilder.Entity<Transaction>().ToTable("MH_Transactions");
            modelBuilder.Entity<Dispute>().ToTable("MH_Disputes");
            modelBuilder.Entity<DisputeHistoryEntry>().ToTable("MH_DisputeHistory");
            modelBuilder.Entity<DisputeDocument>().ToTable("MH_DisputeDocuments");
            modelBuilder.Entity<MonthlyStatement>().ToTable("MH_MonthlyStatements");
            modelBuilder.Entity<GeneratedReport>().ToTable("MH_GeneratedReports");

            // Keys
            modelBuilder.Entity<Merchant>().HasKey(m => m.MerchantId);
            modelBuilder.Entity<MerchantUser>().HasKey(u => u.UserId);
            modelBuilder.Entity<Transaction>().HasKey(t => t.TransactionId);
            modelBuilder.Entity<Dispute>().HasKey(d => d.DisputeId);
            modelBuilder.Entity<DisputeHistoryEntry>().HasKey(h => h.HistoryId);
            modelBuilder.Entity<DisputeDocument>().HasKey(d => d.DocumentId);
            modelBuilder.Entity<MonthlyStatement>().HasKey(s => s.StatementId);
            modelBuilder.Entity<GeneratedReport>().HasKey(r => r.GeneratedReportId);

            // Relationships
            modelBuilder.Entity<Transaction>()
                .HasRequired(t => t.Merchant)
                .WithMany(m => m.Transactions)
                .HasForeignKey(t => t.MerchantId);

            modelBuilder.Entity<Dispute>()
                .HasRequired(d => d.Merchant)
                .WithMany(m => m.Disputes)
                .HasForeignKey(d => d.MerchantId);

            modelBuilder.Entity<Dispute>()
                .HasRequired(d => d.Transaction)
                .WithMany()
                .HasForeignKey(d => d.TransactionId);

            modelBuilder.Entity<MerchantUser>()
                .HasRequired(u => u.Merchant)
                .WithMany(m => m.Users)
                .HasForeignKey(u => u.MerchantId);

            // Indexes (matching database)
            modelBuilder.Entity<Transaction>()
                .Property(t => t.TransactionDate)
                .HasColumnType("datetime2");

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Dispute>()
                .Property(d => d.Amount)
                .HasPrecision(18, 2);

            base.OnModelCreating(modelBuilder);
        }
    }

    /// <summary>
    /// EF6 configuration for MerchantHub context
    /// </summary>
    public class MerchantHubDbConfiguration : DbConfiguration
    {
        public MerchantHubDbConfiguration()
        {
            SetExecutionStrategy("System.Data.SqlClient",
                () => new System.Data.Entity.SqlServer.SqlAzureExecutionStrategy(5, TimeSpan.FromSeconds(30)));
        }
    }
}
