using StackExchange.Redis;
using MantoProxy.Config;

namespace MantoProxy.Services
{
    class CacheService
    {
        private static ConnectionMultiplexer? Connection;

        private  static IDatabase? Database;

        private static bool Configured = false;

        public static void Configure()
        {
            if (Configured) return;

            var options = new ConfigurationOptions()
            {
                EndPoints =  {
                    { RedisConfig.Host, RedisConfig.Port }
                },
                AsyncTimeout = RedisConfig.DefaultTimeout,
                SyncTimeout = RedisConfig.DefaultTimeout,
            };

            do
            {
                try
                {
                    Connection = ConnectionMultiplexer.Connect(options);
                    Database = Connection.GetDatabase();
                    Configured = true;
                    Console.WriteLine("Configured Redis Cache!");
                }
                catch
                {
                    Console.WriteLine("Redis not connected! Trying in 5s!");
                    var task = Task.Run(() =>
                    {
                        Task.Delay(5000).Wait();
                    });
                    task.Wait();
                }
            } while (!Configured);

        }

        public async static void Store(string key, string value, TimeSpan? expiry = null)
        {
            if (!Configured || Database == null) return;

            try
            {
                await Database.StringSetAsync(key, value, expiry);
            }
            catch (Exception)
            {
                return;
            }
        }

        public async static Task<string> Retrieve(string key)
        {
            if (!Configured || Database == null) return String.Empty;

            try
            {
                var data = await Database.StringGetAsync(key);
                if (String.IsNullOrEmpty(data)) return String.Empty;

                return data.ToString();
            }
            catch (Exception)
            {
                return String.Empty;
            }
        }
    }
}