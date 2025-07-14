using Microsoft.EntityFrameworkCore;
using SmartGuardHub.Features.DeviceManagement;
using SmartGuardHub.Features.SystemDevices;
using SmartGuardHub.Features.Users;

namespace SmartGuardHub.Infrastructure
{
    public class SmartGuardDbContext : DbContext
    {
        public SmartGuardDbContext(DbContextOptions<SmartGuardDbContext> options) : base(options)
        {
        }

        public DbSet<Device> Devices { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<User_Device> User_Devices { get; set; }

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


            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserName).HasMaxLength(100).IsRequired();
                entity.HasIndex(e => e.UserName).IsUnique();
                entity.Property(e => e.Password).HasMaxLength(50);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            });

            // User_Device configuration (Junction table)
            modelBuilder.Entity<User_Device>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Foreign key relationships
                entity.HasOne(ud => ud.User)
                    .WithMany(u => u.UserDevices)
                    .HasForeignKey(ud => ud.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ud => ud.Device)
                    .WithMany(d => d.UserDevices)
                    .HasForeignKey(ud => ud.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint to prevent duplicate user-device assignments
                entity.HasIndex(e => new { e.UserId, e.DeviceId })
                    .IsUnique()
                    .HasDatabaseName("IX_User_Device_UserId_DeviceId");
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
