using System;
using System.IO;
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
            if (OperatingSystem.IsWindows())
            {
                CreateWindowsShortcuts();
            }

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

    private static void CreateWindowsShortcuts()
    {
        if (!OperatingSystem.IsWindows()) return;

        var flagPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PacmanRecreation", "shortcuts_created.flag"
        );

        if (File.Exists(flagPath)) return;

        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;

            var shell = Activator.CreateInstance(shellType);
            if (shell == null) return;

            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var startMenuPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                "Pacman Recreation"
            );

            var flagDir = Path.GetDirectoryName(flagPath);
            if (!string.IsNullOrEmpty(flagDir))
            {
                Directory.CreateDirectory(flagDir);
            }
            Directory.CreateDirectory(startMenuPath);

            CreateShortcut(shell, desktopPath, "Pacman Recreation.lnk");
            CreateShortcut(shell, startMenuPath, "Pacman Recreation.lnk");

            File.WriteAllText(flagPath, DateTime.UtcNow.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not create shortcuts: {ex.Message}");
        }
    }

    private static void CreateShortcut(object shell, string folder, string name)
    {
        var shortcutPath = Path.Combine(folder, name);
        var shortcut = shell.GetType().InvokeMember("CreateShortcut",
            System.Reflection.BindingFlags.InvokeMethod, null, shell,
            new object[] { shortcutPath });

        if (shortcut == null) return;

        shortcut.GetType().InvokeMember("TargetPath",
            System.Reflection.BindingFlags.SetProperty, null, shortcut,
            new object[] { Environment.ProcessPath ?? string.Empty });

        shortcut.GetType().InvokeMember("WorkingDirectory",
            System.Reflection.BindingFlags.SetProperty, null, shortcut,
            new object[] { AppContext.BaseDirectory });

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
        if (File.Exists(iconPath))
        {
            shortcut.GetType().InvokeMember("IconLocation",
                System.Reflection.BindingFlags.SetProperty, null, shortcut,
                new object[] { iconPath });
        }

        shortcut.GetType().InvokeMember("Save",
            System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);
    }
}
