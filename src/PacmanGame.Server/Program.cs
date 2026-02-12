using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PacmanGame.Server;
using System;
using System.Threading.Tasks;

var serviceCollection = new ServiceCollection();
serviceCollection.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
serviceCollection.AddSingleton<RelayServer>();

var serviceProvider = serviceCollection.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Server starting up...");

try
{
    var server = serviceProvider.GetRequiredService<RelayServer>();

    var serverTask = server.StartAsync();

    logger.LogInformation("Pac-Man Relay Server started. Press any key to exit.");
    Console.ReadKey();

    logger.LogInformation("Shutting down server...");
    server.Stop();

    await serverTask;
    logger.LogInformation("Server shut down gracefully.");
}
catch (Exception ex)
{
    logger.LogCritical(ex, "An unhandled exception occurred during server execution.");
    logger.LogInformation("Press any key to exit.");
    Console.ReadKey();
}
