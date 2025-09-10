using StackExchange.Redis;
using MantoProxy.Config;
using System.Diagnostics;

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

        public static void Store(string key, string value, TimeSpan? expiry = null)
        {
            if (!Configured || Database == null) return;

            try
            {
                var watch = Stopwatch.StartNew();
                Database.StringSet(key, value, expiry);
                Application.DebugLog($"Salvo {key} no cache como: {value} e expirando em {expiry}");
                watch.Stop();
                Application.CacheLatency.Record(watch.Elapsed.TotalMilliseconds, KeyValuePair.Create<string, object?>("operation", "SET"));
            }
            catch (Exception ex)
            {
                Application.DebugLog($"Erro ao salvar {key} no cache como: {value} por: " + ex);
                return;
            }
        }

        public static string Retrieve(string key)
        {
            if (!Configured || Database == null) return String.Empty;

            try
            {
                var watch = Stopwatch.StartNew();
                var data = Database.StringGet(key);
                watch.Stop();
                Application.DebugLog($"Recuperado {key} do cache!");
                Application.CacheLatency.Record(watch.Elapsed.TotalMilliseconds, KeyValuePair.Create<string, object?>("operation", "GET"));

                if (String.IsNullOrEmpty(data)) return String.Empty;

                return data.ToString();
            }
            catch (Exception ex)
            {
                Application.DebugLog($"Erro ao recuperar {key} do cache: " + ex);
                return String.Empty;
            }
        }
    }
}
