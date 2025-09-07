using System;
using System.Collections.Generic;
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

        private ConnectionHandler(TcpClient client, string ip, DeviceData device)
        {
            Client = client;
            Stream = client.GetStream();
            Reader = new StreamReader(Stream);
            RemoteIP = ip;
            Device = device;
        }

        public static async Task Handle(TcpClient client)
        {
            string ip = string.Empty;
            if (client.Client.RemoteEndPoint is IPEndPoint ep)
                ip = ep.Address.ToString();

            var data = await DeviceDataHandler.FromIP(ip);
            if (data == null)
            {
                client.Close();
                return;
            }

            var handler = new ConnectionHandler(client, ip, data);
            handler.GetLines();

            try
            {
                await handler.Run();
            }
            catch (SocketException ex)
            {
                switch (ex.SocketErrorCode)
                {
                    case SocketError.HostNotFound:
                        break;
                    default:
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
            }
            finally
            {
                handler.CloseClient();
            }
        }

        private async Task Run()
        {
            if (!await AllowedToRun()) return;

            Tokens = FirstLine.Split(' ');
            if (Tokens[0].Equals("CONNECT", StringComparison.CurrentCultureIgnoreCase))
            {
                await HandleConnect();
                return;
            }

            await HandleHTTPMethods();
        }

        private async Task<bool> AllowedToRun()
        {
            if (AuthenticationEnabled)
            {
                if (string.IsNullOrEmpty(AuthHeader))
                {
                    await SendResponse(ResponseCodes.ProxyAuthenticationRequired);
                    return false;
                }

                if (!AuthHandler.HasPermission(AuthHeader))
                {
                    await SendResponse(ResponseCodes.ProxyAuthenticationRequired);
                    return false;
                }
            }

            if (string.IsNullOrEmpty(FirstLine))
            {
                await SendResponse(ResponseCodes.ImATeapot);
                return false;
            }

            if (!Device.AllowConnection)
            {
                await SendResponse(ResponseCodes.PreconditionRequired);
                return false;
            }

            if (Device.FiltersList.Any(f => f.Contains(HttpUrl)))
            {
                await SendResponse(ResponseCodes.NotAcceptable);
                return false;
            }

            return true;
        }

        private async Task SendResponse(ResponseCodes code)
        {
            if (!Client.Connected) return;

            var data = ResponseHelper.HandleResponse(code);
            await Stream.WriteAsync(data);
        }

        private async Task HandleHTTPMethods()
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
                await SendResponse(ResponseCodes.ImATeapot);
                return;
            }

            if (!IPAddress.TryParse(host, out var ipAddress))
            {
                var addresses = await Dns.GetHostAddressesAsync(host);
                if (addresses.Length == 0)
                    throw new SocketException((int)SocketError.HostNotFound);

                ipAddress = addresses[0];
            }

            using TcpClient server = new();
            await server.ConnectAsync(ipAddress, 80);
            using NetworkStream serverStream = server.GetStream();

            byte[] requestBytes = Encoding.ASCII.GetBytes(requestBuilder.ToString() + "\r\n");
            await serverStream.WriteAsync(requestBytes);

            await Task.Run(() => Relay(serverStream, Stream));

            await Log();
        }

        private async Task HandleConnect()
        {
            string[] hostParts = Tokens[1].Split(':');
            string host = hostParts[0];
            int port = int.Parse(hostParts[1]);

            if (!IPAddress.TryParse(host, out var ipAddress))
            {
                var addresses = await Dns.GetHostAddressesAsync(host);
                if (addresses.Length == 0)
                {
                    await SendResponse(ResponseCodes.BadGateway);
                    return;
                }
                ipAddress = addresses[0];
            }

            using TcpClient server = new();
            await server.ConnectAsync(ipAddress, port);
            using NetworkStream serverStream = server.GetStream();

            byte[] okResponse = Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");
            await Stream.WriteAsync(okResponse);

            var clientToServer = Task.Run(() => Relay(Stream, serverStream));
            var serverToClient = Task.Run(() => Relay(serverStream, Stream));
            await Task.WhenAll(clientToServer, serverToClient);

            await Log();
        }

        private static void Relay(Stream input, Stream output)
        {
            byte[] buffer = new byte[4096];
            int bytesRead;
            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
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

        private async Task Log()
        {
            await DeviceLogService.Create(Device.Id, HttpMethod, HttpUrl, string.Join('\n', Lines));
        }

        private void CloseClient()
        {
            if (Client.Connected)
                Client.Close();
        }
    }
}
