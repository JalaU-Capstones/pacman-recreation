using System;
using System.Globalization;
using System.IO;
using Microsoft.Extensions.Logging;

namespace PacmanGame.Services.Logging;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly object _lock = new();
    private readonly StreamWriter? _writer;
    private readonly LogLevel _minLevel;

    public string LogFilePath { get; }

    public FileLoggerProvider(string logFilePath, LogLevel minLevel)
    {
        LogFilePath = logFilePath;
        _minLevel = minLevel;

        try
        {
            var dir = Path.GetDirectoryName(LogFilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var fs = new FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            _writer = new StreamWriter(fs) { AutoFlush = true };
        }
        catch
        {
            // Best-effort: if file logging fails (permissions, readonly FS, etc.),
            // keep the app running with console/debug logging only.
            _writer = null;
        }
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    internal void WriteLine(string line)
    {
        if (_writer == null) return;
        lock (_lock)
        {
            _writer.WriteLine(line);
        }
    }

    internal bool IsEnabled(LogLevel level) => level >= _minLevel;

    public void Dispose()
    {
        try
        {
            _writer?.Dispose();
        }
        catch
        {
            // ignore
        }
    }

    private sealed class FileLogger : ILogger
    {
        private readonly FileLoggerProvider _provider;
        private readonly string _category;

        public FileLogger(FileLoggerProvider provider, string category)
        {
            _provider = provider;
            _category = category;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => _provider.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception == null) return;

            var ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            var level = logLevel.ToString().ToUpperInvariant();

            var line = $"[{ts}] [{level}] [{_category}] {message}";
            _provider.WriteLine(line);

            if (exception != null)
            {
                _provider.WriteLine(exception.ToString());
            }
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}

