using System.Net;
using System.Net.Sockets;
using MantoProxy.Handlers;
using MantoProxy.Services;

namespace MantoProxy
{
    class Application
    {
        private readonly IPAddress IPAddress;

        private readonly int ListenPort;

        private readonly TcpListener TcpListener;

        public Application(IPAddress iPAddress, int port)
        {
            IPAddress = iPAddress;
            ListenPort = port;
            TcpListener = new TcpListener(IPAddress, ListenPort);
        }

        public void Start()
        {
            CacheService.Configure();
            StartListen();

            while (true) HandleConnection();
        }

        private void StartListen()
        {
            TcpListener.Start();
            Console.WriteLine($"[INFO] Proxy escutando na porta {ListenPort}");
        }

        private void HandleConnection()
        {
            TcpClient client = TcpListener.AcceptTcpClient();

            var workerThread = new Thread(async () => await ConnectionHandler.Handle(client));
            workerThread.Start();
        }
    }
}