using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using DbProxy.Client.Providers;
using DbProxy.Shared.Payloads;
using Org.BouncyCastle.Math.EC.Rfc8032;

namespace DbProxy.Client;

class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            var remoteHost = CommandArgsValueProvider.GetValue(args, "--remote-host");
            var remotePort = CommandArgsValueProvider.GetValue(args, "--remote-port");
            var localPort = CommandArgsValueProvider.GetValue(args, "--local-port", remotePort);
            var sshKeyPath = CommandArgsValueProvider.GetValue(args, "--ssh");
            var requester = CommandArgsValueProvider.GetValue(args, "--requester");

            Console.WriteLine($"Connecting to server... {remoteHost}:{remotePort} to local port {localPort}");

            using var tcp = new TcpClient();
            var localEndpoint = new IPEndPoint(IPAddress.Any, int.Parse(localPort));
            tcp.Client.Bind(localEndpoint);

            await tcp.ConnectAsync(remoteHost, int.Parse(remotePort));

            await using var sslStream = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);
            await sslStream.AuthenticateAsClientAsync(remoteHost);
            Console.WriteLine("TLS Handshake complete with server.");

            var payload = new ClientPayload
            {
                Requester = requester,
                Payload = Guid.NewGuid().ToString()
            };

            var privateKeyBytes = await File.ReadAllBytesAsync(sshKeyPath);
            var payloadBytesToSign = Encoding.UTF8.GetBytes(payload.Payload);
            var signature = new byte[Ed25519.SignatureSize];
            Ed25519.Sign(privateKeyBytes, 0, null, payloadBytesToSign, 0, payloadBytesToSign.Length, signature, 0);

            payload.Signature = Convert.ToBase64String(signature);

            string json = JsonSerializer.Serialize(payload);
            byte[] payloadBytes = Encoding.UTF8.GetBytes(json);
            await sslStream.WriteAsync(payloadBytes);

            var buffer = new byte[1024];
            var bytesRead = await sslStream.ReadAsync(buffer);
            var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"Server response: {response}");
        }
        catch (IOException)
        {
            Console.WriteLine("It seems that the server is not running. Please make sure it is running and try again.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }
    }
}