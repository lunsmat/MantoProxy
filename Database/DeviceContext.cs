using MantoProxy.Models;
using Microsoft.EntityFrameworkCore;

namespace MantoProxy.Database
{
    class DeviceContext: DbContext
    {
        public DbSet<Device> Devices { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseNpgsql("Host=127.0.0.1;Database=manto_proxy;Username=postgres;Password=admin")
                .UseSnakeCaseNamingConvention();
    }
}