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

        private const string DataFromMacCachePrefix = "data-from-mac-";

        public static DeviceData? FromIP(string ip)
        {
            try
            {
                if (string.IsNullOrEmpty(ip))
                {
                    Application.DebugLog("IP não preenchido");
                    return null;
                }

                var mac = RecoverMacFromIP(ip);
                if (String.IsNullOrEmpty(mac)) return null;

                var data = RecoverDataFromMac(mac);

                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO] Falha ao dados do dispositivo: {ex.ToString()}");
                return null;
            }
        }

        private static string TryFromCache(string key)
        {
            try
            {
                var data = CacheService.Retrieve(key);
                if (String.IsNullOrEmpty(data)) return String.Empty;

                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get from cache: {ex.ToString()}");
                return String.Empty;
            }
        }

        private static string RecoverMacFromIP(string ip)
        {

            string mac = TryFromCache(MacFromIPCachePrefix + ip);

            if (String.IsNullOrEmpty(mac))
            {
                Application.DebugLog("Mac não encontrado no Cache: " + ip);
                new Ping().Send(ip, 100);
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

                process.WaitForExit();
                Application.CommandLatency.Record(watch.Elapsed.TotalMilliseconds, KeyValuePair.Create<string, object?>("command", "arp"));
                string output = process.StandardOutput.ReadToEnd();
                Application.DebugLog($"{ip} - {output}");

                mac = GetMacFromARPOutput(output, ip);
                if (!String.IsNullOrEmpty(mac))
                    StoreInCache(MacFromIPCachePrefix + ip, mac);
            }

            return mac;
        }

        private static DeviceData? RecoverDataFromMac(string mac)
        {
            string json = TryFromCache(DataFromMacCachePrefix + mac);
            if (!String.IsNullOrEmpty(json))
            {
                Application.DebugLog("Dispositivo encontrado no cache: " + json);
                var jsonData = JsonSerializer.Deserialize<DeviceData>(json);
                return jsonData;
            }

            var data = DeviceDataService.GetDeviceDataFromMac(mac);
            Application.DebugLog("Dispostivo não encontrado no Cache, pego do banco de dados: " + data?.ToString());
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
