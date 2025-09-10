using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MantoProxy.Enums;
using MantoProxy.Helpers;
using MantoProxy.Models;
using MantoProxy.Services;

namespace MantoProxy.Handlers
{
    class ConnectionHandler
    {
        private bool AuthenticationEnabled = false;

        private readonly TcpClient Client;

        private readonly NetworkStream Stream;

        private readonly StreamReader Reader;

        private string[] Tokens = Array.Empty<string>();

        private readonly string RemoteIP;

        private string? AuthHeader;

        private readonly List<string> Lines = new();

        private readonly DeviceData Device;

        private string FirstLine => Lines.FirstOrDefault(String.Empty);

        private string HttpMethod => FirstLine.Split(' ').FirstOrDefault(String.Empty);

        private string HttpUrl => FirstLine.Split(' ').ElementAtOrDefault(1) ?? String.Empty;

        private Stopwatch Watch;

        private ConnectionHandler(TcpClient client, string ip, DeviceData device, Stopwatch? watch = null)
        {
            Client = client;
            Stream = client.GetStream();
            Reader = new StreamReader(Stream);
            RemoteIP = ip;
            Device = device;
            Watch = watch ?? Stopwatch.StartNew();
        }

        public static void Handle(TcpClient client)
        {
            var watch = Stopwatch.StartNew();
            string ip = string.Empty;
            if (client.Client.RemoteEndPoint is IPEndPoint ep)
                ip = ep.Address.ToString();

            var data = DeviceDataHandler.FromIP(ip);
            if (data == null)
            {
                Application.Requests.Add(
                    1,
                    KeyValuePair.Create<string, object?>("Requests", "Failed"),
                    KeyValuePair.Create<string, object?>("Requests", "Failed.DataNotFound")
                );
                client.Close();
                return;
            }

            var handler = new ConnectionHandler(client, ip, data, watch);
            handler.GetLines();

            try
            {
                handler.Run();
            }
            catch (SocketException ex)
            {
                switch (ex.SocketErrorCode)
                {
                    case SocketError.HostNotFound:
                    case SocketError.ConnectionReset:
                        break;
                    default:
                        Console.WriteLine("Error: " + ex.SocketErrorCode);
                        throw;
                }
            }
            catch (Exception ex)
            {
                var error = new StringBuilder();
                error.AppendLine("-------------------------- Request --------------------------");
                error.AppendLine($"[ERRO] {ex}");
                error.AppendLine($"Request: {string.Join('\n', handler.Lines)}");
                Console.WriteLine(error.ToString());
                Application.Requests.Add(
                    1,
                    KeyValuePair.Create<string, object?>("Requests", "Failed"),
                    KeyValuePair.Create<string, object?>("Requests", "Failed.Exception")
                );
            }
            finally
            {
                handler.CloseClient();
            }
        }

        private void Run()
        {
            if (!AllowedToRun())
            {
                NoNetWorkMetricRegister();
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

        private bool AllowedToRun()
        {
            if (AuthenticationEnabled)
            {
                if (string.IsNullOrEmpty(AuthHeader))
                {
                    SendResponse(ResponseCodes.ProxyAuthenticationRequired);
                    return false;
                }

                if (!AuthHandler.HasPermission(AuthHeader))
                {
                    SendResponse(ResponseCodes.ProxyAuthenticationRequired);
                    return false;
                }
            }

            if (string.IsNullOrEmpty(FirstLine))
            {
                SendResponse(ResponseCodes.ImATeapot);
                return false;
            }

            if (!Device.AllowConnection)
            {
                SendResponse(ResponseCodes.PreconditionRequired);
                return false;
            }

            if (Device.FiltersList.Any(f => f.Contains(HttpUrl)))
            {
                SendResponse(ResponseCodes.NotAcceptable);
                return false;
            }

            return true;
        }

        private void SendResponse(ResponseCodes code)
        {
            if (!Client.Connected) return;

            var data = ResponseHelper.HandleResponse(code);
            Application.Requests.Add(
                1,
                KeyValuePair.Create<string, object?>("Requests", "Failed"),
                KeyValuePair.Create<string, object?>("Response", $"Code.{(int)code}")
            );
            Stream.Write(data);
        }

        private void HandleHTTPMethods()
        {
            string host = string.Empty;
            var requestBuilder = new StringBuilder();

            foreach (var line in Lines)
            {
                requestBuilder.AppendLine(line);
                if (line.StartsWith("Host: ", StringComparison.OrdinalIgnoreCase))
                    host = line[6..].Trim();
            }

            if (string.IsNullOrEmpty(host))
            {
                SendResponse(ResponseCodes.ImATeapot);
                return;
            }

            if (!IPAddress.TryParse(host, out var ipAddress))
            {
                var addresses = Dns.GetHostAddresses(host);
                if (addresses.Length == 0)
                    throw new SocketException((int)SocketError.HostNotFound);

                ipAddress = addresses[0];
            }

            NoNetWorkMetricRegister();

            using TcpClient server = new();
            server.Connect(ipAddress, 80);
            using NetworkStream serverStream = server.GetStream();

            byte[] requestBytes = Encoding.ASCII.GetBytes(requestBuilder.ToString() + "\r\n");
            serverStream.Write(requestBytes);

            var thread = new Thread(() => Relay(serverStream, Stream));
            thread.Start();
            thread.Join();
            CloseClient();
            Application.Requests.Add(1, KeyValuePair.Create<string, object?>("Requests", "Succeeded"));

            Log();
        }

        private void HandleConnect()
        {
            string[] hostParts = Tokens[1].Split(':');
            string host = hostParts[0];
            int port = int.Parse(hostParts[1]);

            if (!IPAddress.TryParse(host, out var ipAddress))
            {
                var addresses = Dns.GetHostAddresses(host);
                if (addresses.Length == 0)
                {
                    SendResponse(ResponseCodes.BadGateway);
                    return;
                }
                ipAddress = addresses[0];
            }

            NoNetWorkMetricRegister();

            using TcpClient server = new();
            server.Connect(ipAddress, port);
            using NetworkStream serverStream = server.GetStream();

            byte[] okResponse = Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");
            Stream.Write(okResponse);

            var clientToServer = new Thread(() => Relay(Stream, serverStream));
            var serverToClient = new Thread(() => Relay(serverStream, Stream));

            clientToServer.Start();
            serverToClient.Start();

            clientToServer.Join();
            serverToClient.Join();

            CloseClient();
            Application.Requests.Add(1, KeyValuePair.Create<string, object?>("Requests", "Succeeded"));

            Log();
        }

        private void Relay(Stream input, Stream output)
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
            catch (IOException)
            {
            }
            catch (Exception ex)
            {
                var error = new StringBuilder();
                error.AppendLine("-------------------------- Request --------------------------");
                error.AppendLine($"[ERRO] {ex}");
                error.AppendLine($"Request: {string.Join('\n', Lines)}");
                Console.WriteLine(error.ToString());
                Application.Requests.Add(
                    1,
                    KeyValuePair.Create<string, object?>("Requests", "Failed"),
                    KeyValuePair.Create<string, object?>("Requests", "Failed.Exception")
                );
            }
        }

        private void GetLines()
        {
            Lines.Clear();
            string? line;
            while (!string.IsNullOrEmpty(line = Reader.ReadLine()))
            {
                line?.Replace("127.0.0.1", RemoteIP);
                line?.Replace("localhost", RemoteIP);
                Lines.Add(line ?? string.Empty);
                CheckAuthHeader(line ?? string.Empty);
            }
        }

        private void CheckAuthHeader(string line)
        {
            if (line.StartsWith("Proxy-Authorization:", StringComparison.OrdinalIgnoreCase))
                AuthHeader = line;
        }

        private void Log()
        {
            DeviceLogService.Create(Device.Id, HttpMethod, HttpUrl, string.Join('\n', Lines));
        }

        private void CloseClient()
        {
            if (Client.Connected)
                Client.Close();
        }

        private void NoNetWorkMetricRegister()
        {
            Watch.Stop();
            Application.RequestsDurationNoNetwork.Record(Watch.Elapsed.TotalMilliseconds);
        }
    }
}
