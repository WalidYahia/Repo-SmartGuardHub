using Microsoft.EntityFrameworkCore;
using SmartGuardHub.Features.DeviceManagement;
using static SmartGuardHub.Infrastructure.Enums;

namespace SmartGuardHub.Infrastructure
{
    public class SmartGuardDbContext : DbContext
    {
        public SmartGuardDbContext(DbContextOptions<SmartGuardDbContext> options) : base(options) { }

        public DbSet<DeviceConfigRecord> DeviceConfigs => Set<DeviceConfigRecord>();
        public DbSet<SensorReadingRecord> SensorReadings => Set<SensorReadingRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DeviceConfigRecord>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.ConfigType).HasConversion<int>();
                e.Property(x => x.UpdatedFrom).HasConversion<int>();
                e.HasIndex(x => x.ConfigType).IsUnique();  // one active row per ConfigType
                e.HasIndex(x => x.SyncedToCloud);
            });

            modelBuilder.Entity<SensorReadingRecord>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.UpdatedFrom).HasConversion<int>();
                e.HasIndex(x => x.SensorId);
                e.HasIndex(x => x.SyncedToCloud);
                e.HasIndex(x => new { x.SensorId, x.LogTime });
            });
        }
    }
}
