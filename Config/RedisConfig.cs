using StackExchange.Redis;

namespace MantoProxy.Config
{
    public static class RedisConfig
    {
        public const string Host = "localhost";

        public const int Port = 6379;

        public const int DefaultTimeout = 300;
    }
}