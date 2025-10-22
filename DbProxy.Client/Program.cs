using DbProxy.Client.Providers;
using DbProxy.Client.Resolvers;

namespace DbProxy.Client;

class Program
{
    /// <summary>
    /// DbProxy connect --remote-host localhost --remote-port 5432 --local-port 5433 --token XYZ
    /// </summary>
    /// <param name="args"></param>
    public static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: DbProxy <command> [options]");
            return;
        }

        var command = args[0].ToLower();
        var token = CommandArgsValueProvider.GetValue(args, "--token");

        var commandTask = command switch
        {
            "connect" => RemoteTcpConnectionResolver.ResolveRemoteConnectionAsync(args, command),
            "proxy" => RemoteTcpConnectionResolver.ResolveRemoteConnectionAsync(args, token),
            _ => throw new ArgumentException($"Unknown command: {command}")
        };

        await commandTask;
    }
}