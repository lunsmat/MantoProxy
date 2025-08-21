using MantoProxy.Services;

namespace MantoProxy.Handlers
{
    partial class FirewallHandler
    {
        public const string CachePrefix = "mac-to-filters-";

        public static bool HasPermission(string macAddress, string url)
        {
            try
            {
                var filters = GetFilters(macAddress);

                var blocked = IsUrlBlocked(url, filters);

                if (blocked) return false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static List<string> GetFilters(string macAddress)
        {
            var fromCache = TryFromCache(macAddress);
            if (fromCache != null) return FormatFromCache(fromCache);

            var deviceFilters = DeviceFilterService.GetDeviceFiltersFromMac(macAddress);

            var result = new List<string>();
            foreach (var deviceFilter in deviceFilters)
            {
                var splittedFilters = deviceFilter.Filters.Split('\n');
                foreach (var filter in splittedFilters) result.Add(filter.Trim());
            }

            StoreFiltersInCache(macAddress, string.Join(',', result));

            return result;
        }

        private static string? TryFromCache(string macAddress)
        {
            try
            {
                var filters = CacheService.Retrieve(CachePrefix + macAddress);

                if (String.IsNullOrEmpty(filters)) return null;

                return filters;
            }
            catch
            {
                return null;
            }
        }

        private static void StoreFiltersInCache(string macAddress, string value)
        {
            try
            {
                CacheService.Store(CachePrefix + macAddress, value, TimeSpan.FromHours(2));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to set in cache: {ex.ToString()}");
                return;
            }
        }

        private static List<string> FormatFromCache(string filters)
        {
            return [.. filters.Split(',')];
        }



        private static bool IsUrlBlocked(string url, List<string> Filters)
        {
            foreach (string filter in Filters)
            {
                if (url.Contains(filter)) return true;
            }

            return false;
        }
    }
}