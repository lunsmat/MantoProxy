using System.Net;

namespace MantoProxy
{
    class MantoProxy
    {
        private const int ListenPort = 8080;

        private static readonly Application APP = new(IPAddress.Any, ListenPort);

        public static void Main()
        {
            APP.Start();
        }

    }
}
