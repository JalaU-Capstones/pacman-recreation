using System;
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.Logging;

namespace PacmanGame;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Create a temporary logger for startup
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        var logger = loggerFactory.CreateLogger<Program>();

        logger.LogInformation("DEBUG: Client App Entry Point reached");
        try
        {
            logger.LogDebug("DEBUG: Building Avalonia app...");
            var app = BuildAvaloniaApp();
            logger.LogDebug("DEBUG: Avalonia app built, starting...");
            app.StartWithClassicDesktopLifetime(args);
            logger.LogInformation("DEBUG: Avalonia app shut down gracefully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "FATAL ERROR: Application terminated unexpectedly.");
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}
