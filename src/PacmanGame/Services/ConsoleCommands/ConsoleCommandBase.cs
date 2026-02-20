using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PacmanGame.Models.Console;
using PacmanGame.Services.Interfaces;

namespace PacmanGame.Services.ConsoleCommands;

public abstract class ConsoleCommandBase : IConsoleCommand
{
    public abstract string Name { get; }
    public abstract string Syntax { get; }
    public abstract string Description { get; }
    public virtual IReadOnlyCollection<string> Aliases => new List<string>();

    public abstract Task<ConsoleCommandResult> ExecuteAsync(string[] args, ConsoleContext context);

    protected static string FormatCommandEntry(IConsoleCommand command)
    {
        return $"/{command.Name}\t{command.Syntax}\t{command.Description}";
    }
}
