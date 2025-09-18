using System.Diagnostics;
using MantoProxy.Database;
using MantoProxy.Models;

namespace MantoProxy.Services
{
    class DeviceLogService
    {
        public static DeviceLog Create(int deviceId, string httpMethod, string httpUrl, string httpHeaders)
        {
            var context = new DatabaseContext();

            var deviceLog = new DeviceLog
            {
                DeviceId = deviceId,
                HttpMethod = httpMethod,
                HttpUrl = httpUrl,
                HttpHeaders = httpHeaders,
                HttpBody = String.Empty,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
            };

            var watch = Stopwatch.StartNew();
            context.DeviceLogs.Add(deviceLog);
            context.SaveChanges();
            watch.Stop();
            Application.DatabaseLatency.Record(watch.Elapsed.TotalMilliseconds, KeyValuePair.Create<string, object?>("operation", "insert.device_log"));

            return deviceLog;
        }
    }
}
