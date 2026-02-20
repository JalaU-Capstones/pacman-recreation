using System;

namespace PacmanGame.Models.Console;

public enum ConsoleMessageType
{
    System,
    Info,
    Success,
    Error,
    Input
}

public sealed class ConsoleMessage
{
    public string Text { get; }
    public ConsoleMessageType Type { get; }
    public DateTime Timestamp { get; }

    public ConsoleMessage(string text, ConsoleMessageType type)
    {
        Text = text;
        Type = type;
        Timestamp = DateTime.UtcNow;
    }
}
