using System.Net;
using System.Net.Sockets;
using MantoProxy.Handlers;
using MantoProxy.Services;

namespace MantoProxy
{
    class Application(IPAddress iPAddress, int port)
    {
        private readonly IPAddress IPAddress = iPAddress;

        private readonly int ListenPort = port;

        private readonly TcpListener TcpListener = new TcpListener(iPAddress, port);

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

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    ConnectionHandler.Handle(client);
                }
                catch
                {
                }
            }).Start();

            // Task.Run(() => ConnectionHandler.Handle(client));
        }
    }
}