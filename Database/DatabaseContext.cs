using MantoProxy.Models;
using Microsoft.EntityFrameworkCore;

namespace MantoProxy.Database
{
    class DatabaseContext : DbContext
    {
        public DbSet<DeviceLog> DeviceLogs { get; set; }

        public DbSet<DeviceData> DeviceData { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseNpgsql("Host=127.0.0.1;Database=manto_proxy;Username=postgres;Password=admin")
                .UseSnakeCaseNamingConvention();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<DeviceData>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("device_data");
            });
        }
    }
}
