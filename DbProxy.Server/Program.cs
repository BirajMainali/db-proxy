using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DbProxy.Server.Models;
using DbProxy.Server.Services;

namespace DbProxy.Server;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting server...");

        var configuration = BuildConfiguration();
        var services = ConfigureServices(configuration);

        await using var serviceProvider = services.BuildServiceProvider();
        var tcpServer = serviceProvider.GetRequiredService<TcpServerService>();

        await tcpServer.StartAsync();
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    private static IServiceCollection ConfigureServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();

        var serverConfig = new ServerConfiguration();
        configuration.Bind(serverConfig);
        services.AddSingleton(serverConfig);

        services.AddSingleton<EmailService>();
        services.AddSingleton<ClientHandlerService>();
        services.AddSingleton<TcpServerService>();

        return services;
    }
}