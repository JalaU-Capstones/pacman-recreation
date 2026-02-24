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
            // Ensure SQLite provider is initialized early (SQLCipher bundle when available).
            SQLitePCL.Batteries_V2.Init();

            if (OperatingSystem.IsWindows())
            {
                CreateWindowsShortcuts(logger);
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

    private static void CreateWindowsShortcuts(ILogger logger)
    {
        if (!OperatingSystem.IsWindows()) return;

        var flagPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PacmanRecreation", "shortcuts_created.flag"
        );

        if (File.Exists(flagPath))
        {
            logger.LogDebug("Windows shortcuts already created (flag present at {FlagPath}).", flagPath);
            return;
        }

        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                logger.LogWarning("WScript.Shell is unavailable; skipping shortcut creation.");
                return;
            }

            var shell = Activator.CreateInstance(shellType);
            if (shell == null)
            {
                logger.LogWarning("Failed to create WScript.Shell instance; skipping shortcut creation.");
                return;
            }

            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            // Explicit per requirement:
            // %APPDATA%\Microsoft\Windows\Start Menu\Programs\Pacman Recreation\
            var startMenuProgramsRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft", "Windows", "Start Menu", "Programs"
            );
            var startMenuPath = Path.Combine(startMenuProgramsRoot, "Pacman Recreation");

            var flagDir = Path.GetDirectoryName(flagPath);
            if (!string.IsNullOrEmpty(flagDir))
            {
                Directory.CreateDirectory(flagDir);
            }
            Directory.CreateDirectory(startMenuPath);

                CreateShortcut(shell, desktopPath, "Pacman Recreation.lnk", logger);
                CreateShortcut(shell, startMenuPath, "Pacman Recreation.lnk", logger);

            File.WriteAllText(flagPath, DateTime.UtcNow.ToString());
            logger.LogInformation("Windows shortcuts created (Desktop + Start Menu).");
        }
        catch (Exception ex)
        {
            // Best-effort only; log details for troubleshooting.
            logger.LogWarning(ex, "Failed to create Windows shortcuts (permissions or shell unavailable).");
        }
    }

    private static void CreateShortcut(object shell, string folder, string name, ILogger logger)
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
            new object[] { AppDomain.CurrentDomain.BaseDirectory });

        // Prefer a stable absolute path to an .ico on disk for persistent branding.
        // This also works when the app isn't installed as an MSI/MSIX and has no embedded exe icon metadata.
        var iconPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "icon.ico"));
        if (File.Exists(iconPath))
        {
            shortcut.GetType().InvokeMember("IconLocation",
                System.Reflection.BindingFlags.SetProperty, null, shortcut,
                new object[] { $"{iconPath},0" });
        }
        else
        {
            logger.LogWarning("icon.ico was not found at {IconPath}; shortcut will use the default icon.", iconPath);
        }

        shortcut.GetType().InvokeMember("Save",
            System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);

        logger.LogInformation("Shortcut created successfully at {Path}", shortcutPath);
    }
}
