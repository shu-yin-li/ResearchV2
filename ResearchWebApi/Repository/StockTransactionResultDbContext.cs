using Microsoft.EntityFrameworkCore;
using ResearchWebApi.Models.Results;

namespace ResearchWebApi.Repository
{
    public class StockTransactionResultDbContext : DbContext
    {
        public DbSet<StockTransactionResult> StockTransactionResult { get; set; }

        public StockTransactionResultDbContext(DbContextOptions<StockTransactionResultDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StockTransactionResult>()
                .HasKey(t => t.Id);
        }

        public override int SaveChanges()
        {
            ChangeTracker.DetectChanges();
            return base.SaveChanges();
        }
    }
}

