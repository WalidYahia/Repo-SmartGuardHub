using Microsoft.EntityFrameworkCore;
using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.Logging;
using SmartGuardHub.Features.SystemDevices;

namespace SmartGuardHub.Infrastructure
{
    public class SmartGuardDbContext : DbContext
    {
        public SmartGuardDbContext(DbContextOptions<SmartGuardDbContext> options) : base(options)
        {
        }

        public DbSet<Sensor> Sensors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Sensor>(entity =>
            {
                entity.HasKey(e => e.SensorId);
                entity.Property(e => e.UnitId).HasMaxLength(50);
                entity.HasIndex(e => new { e.UnitId, e.SwitchNo })
                    .IsUnique()
                    .HasDatabaseName("IX_Device_DeviceId_SwitchNo");
                entity.Property(e => e.Name).HasMaxLength(150);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Url).HasMaxLength(100);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.LastSeen).HasDefaultValueSql("datetime('now')");
            });
        }
    }

    public static class DatabaseSeeder
    {
        public static void SeedData(SmartGuardDbContext context)
        {
            //// Seed DeviceTypes
            //if (!context.Users.Any())
            //{
            //    context.DeviceTypes.AddRange(
            //        new DeviceType { Name = "Switch", Description = "Network Switch Device" },
            //        new DeviceType { Name = "Sensor", Description = "Temperature/Humidity Sensor" },
            //        new DeviceType { Name = "Camera", Description = "Security Camera" }
            //    );
            //    context.SaveChanges();
            //}
        }
    }
}
