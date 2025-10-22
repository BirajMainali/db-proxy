using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using DbProxy.Client.Providers;

namespace DbProxy.Client.Resolvers;

public static class RemoteTcpConnectionResolver
{
    /// <summary>
    /// To simplify token handling, we're using a straightforward approach for now.
    /// When the command is for proxying, the payload will contain the token.
    /// When the command is for connecting, the payload will simply be "connect".
    /// This reduces the need for additional API calls and other complexities.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="payload"></param>
    public static async Task ResolveRemoteConnectionAsync(string[] args, string payload)
    {
        var remoteHost = CommandArgsValueProvider.GetValue(args, "--remote-host");
        var remotePort = CommandArgsValueProvider.GetValue(args, "--remote-port");
        var localPort = CommandArgsValueProvider.GetValue(args, "--local-port", remotePort);

        Console.WriteLine($"Connecting to server... {remoteHost}:{remotePort} to local port {localPort}");

        using var tcp = new TcpClient();
        var localEndpoint = new IPEndPoint(IPAddress.Any, int.Parse(localPort));
        tcp.Client.Bind(localEndpoint);

        await tcp.ConnectAsync(remoteHost, int.Parse(remotePort));

        await using var sslStream = new SslStream(tcp.GetStream(), false, (sender, cert, chain, errors) => true);

        await sslStream.AuthenticateAsClientAsync(remoteHost);
        Console.WriteLine("TLS Handshake complete with server.");

        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        await sslStream.WriteAsync(payloadBytes);
        Console.WriteLine("Payload sent to server.");

        var buffer = new byte[1024];
        var bytesRead = await sslStream.ReadAsync(buffer);
        var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Console.WriteLine($"Server response: {response}");
    }
}