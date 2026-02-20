using System.Threading.Tasks;
using PacmanGame.Models.Console;

namespace PacmanGame.Services.ConsoleCommands;

public class ExitCommand : ConsoleCommandBase
{
    public override string Name => "exit";
    public override string Syntax => "/exit";
    public override string Description => "Close the console.";

    public override Task<ConsoleCommandResult> ExecuteAsync(string[] args, ConsoleContext context)
    {
        return Task.FromResult(new ConsoleCommandResult
        {
            Message = string.Empty,
            MessageType = ConsoleMessageType.Info,
            CloseConsole = true
        });
    }
}
