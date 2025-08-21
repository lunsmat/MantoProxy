using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MantoProxy.Handlers
{
    class ConnectionHandler
    {
        private readonly TcpClient Client;

        private readonly NetworkStream Stream;

        private readonly StreamReader Reader;

        private string[] Tokens = [];

        private readonly string RemoteIP;

        private readonly string MacAddress;

        private string? AuthHeader;

        private string FirstLine => Lines.FirstOrDefault(String.Empty);

        public string HttpMethod => FirstLine.Split(' ').FirstOrDefault(String.Empty);

        public string HttpUrl => FirstLine.Split(' ').ElementAtOrDefault(1) ?? String.Empty;

    
        private readonly List<string> Lines = [];

        ConnectionHandler(TcpClient client)
        {
            Client = client;
            Stream = Client.GetStream();
            #pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            #pragma warning disable CS8602 // Dereference of a possibly null reference.
            RemoteIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            MacAddress = MacHandler.FromIP(RemoteIP);

            Reader = new StreamReader(Stream);
            GetLines();
        }

        public void GetLines()
        {
            Lines.Clear();
            string? line;

            while (!String.IsNullOrEmpty(line = Reader.ReadLine()))
            {
                line?.Replace("127.0.0.1", RemoteIP);
                line?.Replace("localhost", RemoteIP);
                Lines.Add(line ?? String.Empty);
                CheckAuthHeader(line ?? String.Empty);
            }
        }

        public void CheckAuthHeader(string line)
        {
            if (line.StartsWith("Proxy-Authorization:", StringComparison.OrdinalIgnoreCase))
            {
                AuthHeader = line;
                return;
            }
        }

        public static void Handle(TcpClient client)
        {
            var handler = new ConnectionHandler(client);

            try
            {
                handler.Run();
            }
            catch (Exception ex)
            {
                var error = String.Empty;
                error += "-------------------------- Request --------------------------\n";
                error += $"[ERRO] Erro no Handle: {ex.ToString()}\n\n";
                error += $"Request: {String.Join('\n', handler.Lines)}\n";
                Console.WriteLine(error);
            }
            finally
            {
                handler.CloseClient();
            }
        }

        private void Run()
        {
            // if (String.IsNullOrEmpty(AuthHeader))
            // {
            //     Console.WriteLine("No Auth Header");
            //     SendAuthRequiredResponse();
            //     return;
            // }
            // if (!AuthHandler.HasPermission(AuthHeader))
            // {
            //     Console.WriteLine("Not allowed");
            //     SendAuthRequiredResponse();
            //     return;
            // }

            if (String.IsNullOrEmpty(FirstLine)) return;

            if (!NetworkPermissionHandler.HasPermission(MacAddress))
            {
                // Console.WriteLine("Bloqueado por Falta de Permissão: " + MacAddress + " " + HttpUrl);
                SendUnauthorizedResponse();
                return;
            }

            if (!FirewallHandler.HasPermission(MacAddress, HttpUrl))
            {
                // Console.WriteLine("Bloqueado pelo Proxy: " + MacAddress + " " + HttpUrl);
                SendNotAcceptableResponse();
                return;
            }

            Tokens = FirstLine.Split(' ');
            if (Tokens[0].Equals("CONNECT", StringComparison.CurrentCultureIgnoreCase))
            {
                HandleConnect();
                return;
            }

            HandleHTTPMethods();
        }

        private void SendAuthRequiredResponse()
        {
            string response =
                "HTTP/1.1 407 Proxy Authentication Required\r\n" +
                "Proxy-Authenticate: Basic realm=\"MantoProxy\"\r\n" +
                "Content-Length: 0\r\n\r\n";

            byte[] responseBytes = Encoding.ASCII.GetBytes(response);
            Stream.Write(responseBytes, 0, responseBytes.Length);
        }

        private void SendNotAcceptableResponse()
        {
            string response =
                "HTTP/1.1 406 Not Acceptable Required\r\n" +
                "Content-Length: 0\r\n\r\n";

            byte[] responseBytes = Encoding.ASCII.GetBytes(response);
            Stream.Write(responseBytes, 0, responseBytes.Length);
        }

        private void SendUnauthorizedResponse()
        {
            string response =
                "HTTP/1.1 428 Precondition Required\r\n" +
                "Content-Length: 0\r\n\r\n";

            byte[] responseBytes = Encoding.ASCII.GetBytes(response);
            Stream.Write(responseBytes, 0, responseBytes.Length);
        }

        private void HandleHTTPMethods()
        {
            string host = String.Empty;
            var requestBuilder = new StringBuilder();

            foreach (var line in Lines)
            {
                requestBuilder.AppendLine(line);

                if (line.StartsWith("Host: "))
                    host = line[6..].Trim();
            }

            if (string.IsNullOrEmpty(host))
            {
                // Console.WriteLine("[ERRO] Host não encontrado.");
                return;
            }
            // Console.WriteLine($"[INFO] Requisição HTTP para: {host}");

            TcpClient server = new(host, 80);
            NetworkStream serverStream = server.GetStream();

            byte[] requestBytes = Encoding.ASCII.GetBytes(requestBuilder.ToString() + "\r\n");
            serverStream.Write(requestBytes, 0, requestBytes.Length);


            var connThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                Relay(serverStream, Stream);
            });
            var logThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                LogHandler.LogConnection(MacAddress, HttpMethod, HttpUrl, String.Join('\n', Lines));
            });
            connThread.Start();
            connThread.Join();
            logThread.Start();
            logThread.Join();
        }

        private void HandleConnect()
        {
            string[] hostParts = Tokens[1].Split(':');
            string host = hostParts[0];
            int port = int.Parse(hostParts[1]);

            // Console.WriteLine($"[INFO] Túnel HTTP para {host}:{port}");

            var server = new TcpClient(host, port);
            var serverStream = server.GetStream();

            byte[] okResponse = Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");
            Stream.Write(okResponse, 0, okResponse.Length);

            // Task clientToServer = Task.Run(() => Relay(Stream, serverStream));
            // Task serverToClient = Task.Run(() => Relay(serverStream, Stream));
            // Task.WaitAll(clientToServer, serverToClient);

            var clientThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                Relay(Stream, serverStream);
            });
            var serverThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                Relay(serverStream, Stream);
            });
            var logThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                LogHandler.LogConnection(MacAddress, HttpMethod, HttpUrl, String.Join('\n', Lines));
            });
            clientThread.Start();
            serverThread.Start();
            logThread.Start();
            clientThread.Join();
            serverThread.Join();
            logThread.Join();
        }

        private static void Relay(Stream input, Stream output)
        {
            byte[] buffer = new byte[4096];
            int bytesRead;
            try
            {
                while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, bytesRead);
                }
            }
            catch { }
        }

        private void CloseClient()
        {
            Client.Close();
        }
    }
}