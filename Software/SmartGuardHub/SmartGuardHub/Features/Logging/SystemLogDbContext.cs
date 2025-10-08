using Microsoft.EntityFrameworkCore;
using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Infrastructure;

namespace SmartGuardHub.Features.Logging
{
    public class SystemLogDbContext : DbContext
    {
        public SystemLogDbContext(DbContextOptions<SystemLogDbContext> options) : base(options)
        {
        }

        public DbSet<SystemLog> SystemLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SystemLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MessageKey).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Level).HasMaxLength(20).IsRequired();
                entity.Property(e => e.LogTime).IsRequired();
                entity.Property(e => e.Message).HasMaxLength(200);
                entity.Property(e => e.Exception).HasMaxLength(4000);
            });
        }
    }
}
