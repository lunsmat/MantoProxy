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

            Connection = ConnectionMultiplexer.Connect($"{RedisConfig.Host}");
            Database = Connection.GetDatabase();
            Configured = true;
            Console.WriteLine("Configured Redis Cache!");
        }

        public static void Store(string key, string value, TimeSpan? expiry = null)
        {
            if (!Configured || Database == null) return;

            Database.StringSet(key, value, expiry);
        }

        public static string Retrieve(string key)
        {
            if (!Configured || Database == null) return String.Empty;

            var data = Database.StringGet(key);
            if (String.IsNullOrEmpty(data)) return String.Empty;

            return data.ToString();
        }
    }
}