using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MantoProxy.Database;

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
