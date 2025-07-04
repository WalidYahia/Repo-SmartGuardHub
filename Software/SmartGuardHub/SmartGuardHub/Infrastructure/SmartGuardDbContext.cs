using Microsoft.EntityFrameworkCore;
using SmartGuardHub.Features.DeviceManagement;

namespace SmartGuardHub.Infrastructure
{
    public class SmartGuardDbContext : DbContext
    {
        public SmartGuardDbContext(DbContextOptions<SmartGuardDbContext> options) : base(options)
        {
        }

        public DbSet<Device> Devices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Device>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DeviceId).HasMaxLength(50);
                entity.HasIndex(e => new { e.DeviceId, e.SwitchNo })
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
}
