using MantoProxy.Database;
using MantoProxy.Models;

namespace MantoProxy.Services
{
    class DeviceService
    {
        private static readonly DeviceContext Context = new();

        public static Device? GetDeviceFromMac(string mac)
        {
            var context = new DeviceContext();

            var device = context.Devices.FirstOrDefault(x => x.MacAddress == mac);

            return device;
        }
    }
}