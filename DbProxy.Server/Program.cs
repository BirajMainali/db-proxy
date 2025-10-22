using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using MimeKit;
using Microsoft.Extensions.Configuration;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace DbProxy.Server;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting server...");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var port = configuration.GetValue<int>("Port");
        var certificatePassword = configuration.GetValue<string>("CertificatePassword");
        var smtpHost = configuration.GetValue<string>("SmtpHost");
        var smtpPort = configuration.GetValue<int>("SmtpPort");
        var smtpUsername = configuration.GetValue<string>("SmtpUsername");
        var smtpPassword = configuration.GetValue<string>("SmtpPassword");
        var fromAddress = configuration.GetValue<string>("FromAddress");
        var toAddress = configuration.GetValue<string>("ToAddress");
        var toName = configuration.GetValue<string>("ToName");
        var fromName = configuration.GetValue<string>("FromName");
        var subject = configuration.GetValue<string>("Subject");
        var textTemplate = configuration.GetValue<string>("TextTemplate");


        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"Server listening on port {port}...");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(async () =>
            {
                try
                {
                    await using var sslStream = new SslStream(client.GetStream(), false);
                    var serverCert =
                        X509CertificateLoader.LoadPkcs12FromFile("Certificates/proxy-cert.pfx", certificatePassword);

                    await sslStream.AuthenticateAsServerAsync(serverCert, clientCertificateRequired: false,
                        enabledSslProtocols: System.Security.Authentication.SslProtocols.Tls13,
                        checkCertificateRevocation: true);

                    Console.WriteLine("TLS Handshake complete. Waiting for payload...");

                    var buffer = new byte[1024];
                    var bytesRead = await sslStream.ReadAsync(buffer);
                    var payload = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (payload == "connect")
                    {
                        var message = new MimeMessage();
                        message.From.Add(new MailboxAddress(name: fromName, address: fromAddress));
                        message.To.Add(new MailboxAddress(name: toName, address: toAddress));
                        message.Subject = subject;

                        message.Body = new TextPart("plain")
                        {
                            Text = textTemplate!.Replace("{token}", payload)
                        };

                        using var smtpClient = new SmtpClient();
                        await smtpClient.ConnectAsync(host: smtpHost, port: smtpPort, useSsl: true);
                        await smtpClient.AuthenticateAsync(userName: smtpUsername, password: smtpPassword);
                        await smtpClient.SendAsync(message);
                        await smtpClient.DisconnectAsync(quit: true);
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
            });
        }
    }
}