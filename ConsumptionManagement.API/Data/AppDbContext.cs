using ConsumptionManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsumptionManagement.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Site> Sites => Set<Site>();
        public DbSet<Meter> Meters => Set<Meter>();
        public DbSet<MeterReading> MeterReadings => Set<MeterReading>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Site -> Meters (1-to-Many)
            modelBuilder.Entity<Meter>(entity =>
            {
                entity.HasOne(m => m.Site)
                    .WithMany(s => s.Meters)
                    .HasForeignKey(m => m.SiteId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(m => m.MeterNumber)
                    .IsUnique();
            });

            // 2. Meter -> MeterReadings (1-to-Many) & Uniqueness Rule
            modelBuilder.Entity<MeterReading>(entity =>
            {
                entity.HasOne(r => r.Meter)
                    .WithMany(m => m.Readings)
                    .HasForeignKey(r => r.MeterId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Enforce: No two readings for the same meter share the same timestamp
                entity.HasIndex(r => new { r.MeterId, r.TReadingimeUtc })
                    .IsUnique();

                // Explicit precision for cumulative kWh values
                entity.Property(r => r.ReadingSum)
                    .HasPrecision(18, 4);
            });

            // 3. Minimal Seed Data (Optional - remove if you prefer an empty database)
            SeedData(modelBuilder);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<Site>().HasData(
                new Site
                {
                    Id = 1,
                    Name = "Main Facility",
                    Address = "100 Industrial Way"
                }
            );

            modelBuilder.Entity<Meter>().HasData(
                new Meter
                {
                    Id = 1,
                    SiteId = 1,
                    MeterNumber = "MTR-1001"
                }
            );

            modelBuilder.Entity<MeterReading>().HasData(
                new MeterReading { Id = 1, MeterId = 1, TReadingimeUtc = baseTime, ReadingSum = 1000.0000m },
                new MeterReading { Id = 2, MeterId = 1, TReadingimeUtc = baseTime.AddHours(1), ReadingSum = 1012.5000m },
                new MeterReading { Id = 3, MeterId = 1, TReadingimeUtc = baseTime.AddHours(2), ReadingSum = 1030.0000m }
            );
        }
    }
}
