using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using DbProxy.Shared.Payloads;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Math.EC.Rfc8032;

namespace DbProxy.Server;

class Program
{
    static async Task Main()
    {
        try
        {
            var configuration = BuildConfiguration();
            var port = configuration.GetValue<int>("Target:Port");
            var host = configuration.GetValue<string>("Target:Host");
            var password = configuration.GetValue<string>("CertificatePassword");

            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine($"Server listening on port {port}...");

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(async () =>
                {
                    await using var sslStream = new SslStream(client.GetStream(), false);
                    var serverCert = X509CertificateLoader.LoadPkcs12FromFile("Certificates/proxy-cert.pfx", password);

                    await sslStream.AuthenticateAsServerAsync(serverCert,
                        clientCertificateRequired: false,
                        enabledSslProtocols: SslProtocols.Tls13,
                        checkCertificateRevocation: true);

                    Console.WriteLine("TLS Handshake complete. Waiting for payload...");

                    var buffer = new byte[4096];
                    var bytesRead = await sslStream.ReadAsync(buffer);
                    var receivedJsonPayload = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    var clientPayload = JsonSerializer.Deserialize<ClientPayload>(receivedJsonPayload);
                    var publicKeyPath = Path.Combine("/keys", clientPayload!.Requester, ".pub");

                    if (!File.Exists(publicKeyPath))
                    {
                        await sslStream.WriteAsync("Authentication failed! Cannot read your public key."u8.ToArray());
                        return;
                    }

                    var publicKeyBytes = await File.ReadAllBytesAsync(publicKeyPath);
                    var payloadBytes = Encoding.UTF8.GetBytes(clientPayload.Payload);
                    var signatureBytes = Convert.FromBase64String(clientPayload.Signature);

                    var verified = Ed25519.Verify(
                        signatureBytes,
                        0,
                        publicKeyBytes,
                        0,
                        payloadBytes,
                        0,
                        payloadBytes.Length);

                    if (!verified)
                    {
                        await sslStream.WriteAsync("Authentication failed!"u8.ToArray());
                        return;
                    }

                    await ProxyToDatabase(sslStream, host!, port);
                });
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Shutting down the server due to an error: {e.Message}");
            Environment.Exit(1);
        }
    }

    private static async Task ProxyToDatabase(SslStream clientStream, string targetHost, int targetPort)
    {
        try
        {
            var clientToTarget = Task.Run(async () =>
            {
                try
                {
                    using var targetClient = new TcpClient();
                    await targetClient.ConnectAsync(targetHost, targetPort);
                    await using var targetStream = targetClient.GetStream();
                    Console.WriteLine("Proxying connection..., Client -> Target");
                    await clientStream.CopyToAsync(targetStream);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in client -> target direction: {ex.Message}");
                }
            });

            var targetToClient = Task.Run(async () =>
            {
                try
                {
                    using var targetClient = new TcpClient();
                    await using var targetStream = targetClient.GetStream();
                    await targetClient.ConnectAsync(targetHost, targetPort);
                    Console.WriteLine("Proxying connection..., Target -> Client");
                    await targetStream.CopyToAsync(clientStream);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in target -> client direction: {ex.Message}");
                }
            });

            await Task.WhenAny(clientToTarget, targetToClient);
            Console.WriteLine("Proxy connection closed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in proxy: {ex.Message}");
        }
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }
}