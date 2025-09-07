using MantoProxy.Database;
using MantoProxy.Models;

namespace MantoProxy.Services
{
    class DeviceLogService
    {
        public async static Task<DeviceLog> Create(int deviceId, string httpMethod, string httpUrl, string httpHeaders)
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

            await context.DeviceLogs.AddAsync(deviceLog);
            await context.SaveChangesAsync();

            return deviceLog;
        }
    }
}