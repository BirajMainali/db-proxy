using System.Net;
using System.Net.Sockets;
using DbProxy.Server.Models;

namespace DbProxy.Server.Services;

public class TcpServerService(ClientHandlerService clientHandler, ServerConfiguration configuration)
{
    public async Task StartAsync()
    {
        var listener = new TcpListener(IPAddress.Any, configuration.Port);
        listener.Start();
        Console.WriteLine($"Server listening on port {configuration.Port}...");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(async () => await clientHandler.HandleClientAsync(client));
        }
    }
}