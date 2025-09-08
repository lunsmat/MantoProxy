using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.RegularExpressions;
using MantoProxy.Models;
using MantoProxy.Services;

namespace MantoProxy.Handlers
{
    partial class DeviceDataHandler
    {
        private const string MacFromIPCachePrefix = "mac-from-ip-";

        private const string DataFromMacCachePrefix = "data-from-mac";

        public async static Task<DeviceData?> FromIP(string ip)
        {
            try
            {
                if (string.IsNullOrEmpty(ip)) return null;

                var mac = await RecoverMacFromIP(ip);
                if (String.IsNullOrEmpty(mac)) return null;

                var data = await RecoverDataFromMac(mac);

                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO] Falha ao dados do dispositivo: {ex.ToString()}");
                return null;
            }
        }

        private async static Task<string> TryFromCache(string key)
        {
            try
            {
                var data = await CacheService.Retrieve(key);
                if (String.IsNullOrEmpty(data)) return String.Empty;

                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get from cache: {ex.ToString()}");
                return String.Empty;
            }
        }

        private async static Task<string> RecoverMacFromIP(string ip)
        {

            string mac = await TryFromCache(MacFromIPCachePrefix + ip);

            if (String.IsNullOrEmpty(mac))
            {
                var watch = Stopwatch.StartNew();
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "arp",
                        Arguments = "-a",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();

                await process.WaitForExitAsync();
                Application.CommandLatency.Record(watch.Elapsed.TotalMilliseconds, KeyValuePair.Create<string, object?>("command", "arp"));
                string output = process.StandardOutput.ReadToEnd();

                mac = GetMacFromARPOutput(output, ip);
                if (!String.IsNullOrEmpty(mac))
                    StoreInCache(MacFromIPCachePrefix + ip, mac);
            }

            return mac;
        }

        private async static Task<DeviceData?> RecoverDataFromMac(string mac)
        {
            string json = await TryFromCache(DataFromMacCachePrefix + mac);
            if (!String.IsNullOrEmpty(json))
            {
                var jsonData = JsonSerializer.Deserialize<DeviceData>(json);
                return jsonData;
            }

            var data = await DeviceDataService.GetDeviceDataFromMac(mac);
            if (data != null) StoreInCache(DataFromMacCachePrefix + mac, JsonSerializer.Serialize(data));
            return data;
        }

        private static void StoreInCache(string key, string value)
        {
            try
            {
                CacheService.Store(key, value, TimeSpan.FromMinutes(3));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to set in cache: {ex.ToString()}");
                return;
            }
        }

        private static string GetMacFromARPOutput(string output, string ip)
        {
            char[] breakLines = ['\r', '\n'];
            var lines = output.Split(breakLines, StringSplitOptions.RemoveEmptyEntries);
            var macRegex = MacAddressRegex();

            foreach (var line in lines)
            {
                if (line.Contains(ip))
                {
                    var parts = SpaceRegex().Split(line.Trim());
                    if (parts.Length >= 2)
                    {
                        foreach (var part in parts)
                        {
                            if (!macRegex.IsMatch(part)) continue;
                            char separator = part[2];

                            return part
                                .ToUpper()
                                .Replace(separator, '-');
                        }
                    }
                }
            }

            return String.Empty;
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex SpaceRegex();

        [GeneratedRegex("^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$")]
        private static partial Regex MacAddressRegex();
    }
}
