using Microsoft.EntityFrameworkCore;
using ResearchWebApi.Models;

namespace ResearchWebApi.Repository
{
    public class StockModelOldDbContext : DbContext
    {
        public DbSet<StockModelOld> StockModelOld { get; set; }

        public StockModelOldDbContext(DbContextOptions<StockModelOldDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StockModelOld>()
                .HasKey(t => t.Id);
        }

        public override int SaveChanges()
        {
            ChangeTracker.DetectChanges();
            return base.SaveChanges();
        }
    }
}

