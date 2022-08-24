using Microsoft.EntityFrameworkCore;
using ResearchWebApi.Models;

namespace ResearchWebApi.Repository
{
    public class CommonResultDbContext : DbContext
    {
        public DbSet<CommonResult> CommonResult { get; set; }

        public CommonResultDbContext(DbContextOptions<CommonResultDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CommonResult>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ExecuteDate)
                    .IsRequired();
            });
        }

        public override int SaveChanges()
        {
            ChangeTracker.DetectChanges();
            return base.SaveChanges();
        }
    }
}

