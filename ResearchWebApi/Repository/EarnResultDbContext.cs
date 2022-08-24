using Microsoft.EntityFrameworkCore;
using ResearchWebApi.Models;

namespace ResearchWebApi.Repository
{
    public class EarnResultDbContext : DbContext
    {
        public DbSet<EarnResult> EarnResult { get; set; }

        public EarnResultDbContext(DbContextOptions<EarnResultDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EarnResult>()
                .HasKey(t => t.Id);
        }

        public override int SaveChanges()
        {
            ChangeTracker.DetectChanges();
            return base.SaveChanges();
        }
    }
}

