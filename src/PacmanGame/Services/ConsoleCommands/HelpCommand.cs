using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PacmanGame.Models.Console;

namespace PacmanGame.Services.ConsoleCommands;

public class HelpCommand : ConsoleCommandBase
{
    public override string Name => "help";
    public override string Syntax => "/help [command]";
    public override string Description => "Show available commands or detailed help.";
    public override IReadOnlyCollection<string> Aliases => new[] { "*/" };

    public override Task<ConsoleCommandResult> ExecuteAsync(string[] args, ConsoleContext context)
    {
        var builder = new StringBuilder();
        var commands = context.GetAvailableCommands();

        if (args.Length == 0)
        {
            builder.AppendLine("Available commands:");
            foreach (var command in commands)
            {
                builder.AppendLine(FormatCommandEntry(command));
            }
        }
        else
        {
            var target = args[0].TrimStart('/');
            var command = commands.FirstOrDefault(c => string.Equals(c.Name, target, System.StringComparison.OrdinalIgnoreCase)
                || c.Aliases.Any(alias => string.Equals(alias, target, System.StringComparison.OrdinalIgnoreCase)));

            if (command == null)
            {
                builder.AppendLine($"Unknown command '{target}'.");
            }
            else
            {
                builder.AppendLine($"/{command.Name} - {command.Description}");
                builder.AppendLine($"Syntax: {command.Syntax}");
            }
        }

        return Task.FromResult(new ConsoleCommandResult
        {
            Message = builder.ToString().TrimEnd(),
            MessageType = ConsoleMessageType.Info
        });
    }
}
