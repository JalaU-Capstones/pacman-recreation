namespace PacmanGame.Models.Console;

public sealed class ConsoleCommandResult
{
    public string Message { get; init; } = string.Empty;
    public ConsoleMessageType MessageType { get; init; } = ConsoleMessageType.Info;
    public bool CloseConsole { get; init; }
    public bool ClearHistory { get; init; }
}
