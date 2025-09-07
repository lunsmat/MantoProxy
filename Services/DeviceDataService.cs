using MantoProxy.Database;
using MantoProxy.Models;
using Microsoft.EntityFrameworkCore;

namespace MantoProxy.Services
{
    class DeviceDataService
    {
        public async static Task<DeviceData?> GetDeviceDataFromMac(string mac)
        {
            var context = new DatabaseContext();

            var query = $"SELECT id, name, mac_address, allow_connection, filters FROM device_data WHERE mac_address = {mac} LIMIT = 1";

            var result = await context
                .DeviceData
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MacAddress == mac);


            return result;
        }
    }
}