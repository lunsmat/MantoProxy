using MantoProxy.Database;
using MantoProxy.Models;
using Microsoft.EntityFrameworkCore;

namespace MantoProxy.Services
{
    class DeviceFilterService
    {
        public static List<DeviceFilter> GetDeviceFiltersFromMac(string mac)
        {
            var context = new DatabaseContext();

            var result = context
                .Database
                .SqlQuery<DeviceFilter>(
                    $"SELECT device_id, mac_address, filters FROM device_filters WHERE mac_address = {mac}"
                );

            return [.. result];
        }
    }
}