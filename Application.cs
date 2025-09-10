using System.Net;
using System.Net.Sockets;
using MantoProxy.Handlers;
using MantoProxy.Services;
using System.Diagnostics.Metrics;
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace MantoProxy
{
    class Application
    {
        public static bool DEBUG_MODE = false;

        private readonly IPAddress IPAddress;

        private readonly int ListenPort;

        private readonly TcpListener TcpListener;

        private readonly MeterProvider MeterProvider;

        public static Meter Meters = new("MantoProxy");

        public static Counter<int> RequestsTotal = Meters.CreateCounter<int>("MantoProxy.RequestsTotal");

        public static Counter<int> Requests = Meters.CreateCounter<int>("MantoProxy.Requests");

        public static Histogram<double> RequestsDuration = Meters.CreateHistogram<double>("MantoProxy.RequestsDuration");

        public static Histogram<double> RequestsDurationNoNetwork = Meters.CreateHistogram<double>("MantoProxy.RequestsDurationNoNetwork");

        public static Histogram<double> CommandLatency = Meters.CreateHistogram<double>(name: "MantoProxy.CommandLatency", unit: "ms");

        public static Histogram<double> CacheLatency = Meters.CreateHistogram<double>(name: "MantoProxy.CacheLatency", unit: "ms");

        public static Histogram<double> DatabaseLatency = Meters.CreateHistogram<double>(name: "MantoProxy.DatabaseLatency", unit: " ms");

        public Application(IPAddress iPAddress, int port)
        {
            IPAddress = iPAddress;
            ListenPort = port;
            TcpListener = new TcpListener(IPAddress, ListenPort);
            MeterProvider = Sdk.CreateMeterProviderBuilder()
                .AddMeter("MantoProxy")
                .AddPrometheusHttpListener(options => options.UriPrefixes = ["http://localhost:9184"])
                .Build();
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

            var workerThread = new Thread(() =>
            {
                var watch = Stopwatch.StartNew();
                DebugLog("Recebida Requisição do client: " + client.ToString());
                RequestsTotal.Add(1);
                ConnectionHandler.Handle(client);
                watch.Stop();
                DebugLog("Finalizada Requisição do client: " + client.ToString());
                RequestsDuration.Record(watch.Elapsed.TotalMilliseconds);
            });
            workerThread.Start();
        }

        public static void SetDebugMode(bool debugMode)
        {
            DEBUG_MODE = debugMode;
        }

        public static void DebugLog(string message)
        {
            if (!DEBUG_MODE) return;

            Console.WriteLine($"Debug: " + message);
        }
    }
}
