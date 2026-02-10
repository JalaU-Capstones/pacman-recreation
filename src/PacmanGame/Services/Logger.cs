using System;
using System.IO;
using PacmanGame.Services.Interfaces;

namespace PacmanGame.Services;

public class Logger : ILogger
{
    private readonly string _logPath;
    private static readonly object Lock = new();

    public Logger()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logDir = Path.Combine(appData, "PacmanGame");
        _logPath = Path.Combine(logDir, "pacman.log");

        Directory.CreateDirectory(logDir);
    }

    public void Info(string message) => Log("INFO", message);
    public void Warning(string message) => Log("WARNING", message);
    public void Error(string message) => Log("ERROR", message);
    public void Error(string message, Exception ex) => Log("ERROR", $"{message}{Environment.NewLine}{ex}");
    public void Debug(string message) => Log("DEBUG", message);

    private void Log(string level, string message)
    {
        lock (Lock)
        {
            try
            {
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(_logPath, logMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }
    }
}
