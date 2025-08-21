using MantoProxy.Services;

namespace MantoProxy.Handlers
{
    partial class NetworkPermissionHandler
    {
        public const string CachePrefix = "mac-to-permission-";

        public static bool HasPermission(string macAddress)
        {
            try
            {
                var fromCache = TryFromCache(macAddress);
                if (fromCache.HasValue) return fromCache.Value;

                var fromDB = TryFromDatabase(macAddress);

                if (!fromDB.HasValue) return false;

                StorePermissionInCache(macAddress, fromDB.Value);

                return fromDB.Value;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool? TryFromCache(string macAddress)
        {
            try
            {
                var permission = CacheService.Retrieve(CachePrefix + macAddress);

                if (String.IsNullOrEmpty(permission)) return null;

                if (Int32.TryParse(permission, out var result))
                {
                    if (result == 1) return true;
                    return false;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private static bool? TryFromDatabase(string macAddress)
        {
            try
            {
                var device = DeviceService.GetDeviceFromMac(macAddress);

                if (device == null) return null;

                return device.AllowConnection;
            }
            catch
            {
                return null;
            }
        }

        private static void StorePermissionInCache(string macAddress, bool value)
        {
            try
            {
                CacheService.Store(CachePrefix + macAddress, value == true ? 1.ToString() : 0.ToString(), TimeSpan.FromMinutes(3));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to set in cache: {ex.ToString()}");
                return;
            }
        }
    }
}