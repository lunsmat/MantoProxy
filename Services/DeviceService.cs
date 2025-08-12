using MantoProxy.Database;
using MantoProxy.Models;

namespace MantoProxy.Services
{
    class DeviceService
    {
        public static Device? GetDeviceFromMac(string mac)
        {
            var context = new DatabaseContext();

            var device = context.Devices.FirstOrDefault(x => x.MacAddress == mac);

            return device;
        }
    }
}