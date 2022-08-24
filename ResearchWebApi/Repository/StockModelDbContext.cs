using Microsoft.EntityFrameworkCore;
using ResearchWebApi.Models;

namespace ResearchWebApi.Repository
{
    public class StockModelDbContext : DbContext
    {
        public DbSet<StockModel> StockModel { get; set; }

        public StockModelDbContext(DbContextOptions<StockModelDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StockModel>()
                .HasKey(t => t.Id);
        }

        public override int SaveChanges()
        {
            ChangeTracker.DetectChanges();
            return base.SaveChanges();
        }
    }
}

