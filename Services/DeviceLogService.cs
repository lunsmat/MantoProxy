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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            context.DeviceLogs.Add(deviceLog);
            context.SaveChanges();

            return deviceLog;
        }
    }
}