using Microsoft.EntityFrameworkCore;
using ResearchWebApi.Models;

namespace ResearchWebApi.Repository
{
    public class TrainDetailsDbContext : DbContext
    {
        public DbSet<TrainDetails> TrainDetails { get; set; }
        public DbSet<CommonResult> CommonResult { get; set; }

        public TrainDetailsDbContext(DbContextOptions<TrainDetailsDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TrainDetails>()
                .HasKey(t => t.Id);

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

