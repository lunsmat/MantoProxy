using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using MantoProxy.Services;

namespace MantoProxy.Handlers
{
    partial class MacHandler
    {
        public const string CachePrefix = "mac-from-ip-";

        public static string FromIP(string ip)
        {
            try
            {
                var fromCache = TryFromCache(ip);
                if (!String.IsNullOrEmpty(fromCache))
                {
                    return fromCache;
                }

                new Ping().Send(ip, 100);

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

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var mac = GetMacFromARPOutput(output, ip);

                if (!String.IsNullOrEmpty(mac))
                {
                    StoreInCache(ip, mac);
                }

                return mac;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO] Falha ao obter MAC: {ex.Message}");
                return String.Empty;
            }
        }

        private static string TryFromCache(string ip)
        {
            try
            {
                var mac = CacheService.Retrieve(CachePrefix + ip);

                if (String.IsNullOrEmpty(mac)) return String.Empty;
                return mac;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get from cache: {ex}");
                return String.Empty;
            }
        }

        private static void StoreInCache(string ip, string mac)
        {
            try
            {
                CacheService.Store(CachePrefix + ip, mac, TimeSpan.FromMinutes(5));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to set in cache: {ex}");
                return;
            }
        }

        private static string GetMacFromARPOutput(string output, string ip)
        {
            var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
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