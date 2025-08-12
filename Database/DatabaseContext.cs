using MantoProxy.Models;
using Microsoft.EntityFrameworkCore;

namespace MantoProxy.Database
{
    class DatabaseContext : DbContext
    {
        public DbSet<Device> Devices { get; set; }
        public DbSet<DeviceLog> DeviceLogs { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseNpgsql("Host=127.0.0.1;Database=mantoproxy;Username=postgres;Password=admin")
                .UseSnakeCaseNamingConvention();
    }
}