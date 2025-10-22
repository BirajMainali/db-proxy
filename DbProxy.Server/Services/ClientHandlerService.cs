using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DbProxy.Server.Models;

namespace DbProxy.Server.Services;

public class ClientHandlerService(EmailService emailService, ServerConfiguration configuration)
{
    public async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            await using var sslStream = new SslStream(client.GetStream(), false);
            var serverCert =
                X509CertificateLoader.LoadPkcs12FromFile("Certificates/proxy-cert.pfx",
                    configuration.CertificatePassword);

            await sslStream.AuthenticateAsServerAsync(serverCert, clientCertificateRequired: false,
                enabledSslProtocols: System.Security.Authentication.SslProtocols.Tls13,
                checkCertificateRevocation: true);

            Console.WriteLine("TLS Handshake complete. Waiting for payload...");

            var buffer = new byte[1024];
            var bytesRead = await sslStream.ReadAsync(buffer);
            var payload = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            if (payload == "connect")
            {
                await emailService.SendConnectionNotificationAsync(payload);
                Console.WriteLine("Connection notification sent.");
                return;
            }

            const string response = "Token accepted!";
            var responseBytes = Encoding.UTF8.GetBytes(response);
            await sslStream.WriteAsync(responseBytes);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }
        finally
        {
            client.Close();
        }
    }
}