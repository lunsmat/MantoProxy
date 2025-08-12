using MantoProxy.Services;

namespace MantoProxy.Handlers
{
    partial class LogHandler
    {
        public const string CachePrefix = "device-id-from-mac-";

        public static void LogConnection(string macAddress, string httpMethod, string httpUrl, string httpHeaders)
        {
            var deviceId = GetDeviceId(macAddress);

            if (deviceId != null)
            {
                DeviceLogService.Create((int)deviceId, httpMethod, httpUrl, httpHeaders);
            }
        }

        private static int? GetDeviceId(string mac)
        {
            var cacheData = TryGetDeviceIdFromCache(mac);
            if (cacheData != null) return cacheData;

            var device = DeviceService.GetDeviceFromMac(mac);
            if (device == null) return null;

            StoreDeviceIdOnCache(mac, device.Id);
            return device.Id;
        }

        private static int? TryGetDeviceIdFromCache(string mac)
        {
            var data = CacheService.Retrieve(CachePrefix + mac);
            if (data == null) return null;

            var success = int.TryParse(data, out var result);

            if (success) return result;

            return null;
        }

        private static void StoreDeviceIdOnCache(string mac, int deviceId)
        {
            CacheService.Store(CachePrefix + mac, deviceId.ToString());
        }
    }
}