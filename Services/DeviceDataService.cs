using System.Diagnostics;
using MantoProxy.Database;
using MantoProxy.Models;
using Microsoft.EntityFrameworkCore;

namespace MantoProxy.Services
{
    class DeviceDataService
    {
        public static DeviceData? GetDeviceDataFromMac(string mac)
        {
            var context = new DatabaseContext();

            var query = $"SELECT id, name, mac_address, allow_connection, filters FROM device_data WHERE mac_address = {mac} LIMIT = 1";

            var watch = Stopwatch.StartNew();
            var result = context
                .DeviceData
                .AsNoTracking()
                .FirstOrDefault(x => x.MacAddress == mac);
            watch.Stop();
            Application.DatabaseLatency.Record(watch.Elapsed.TotalMilliseconds, KeyValuePair.Create<string, object?>("operation", "select.device_data"));


            return result;
        }
    }
}
