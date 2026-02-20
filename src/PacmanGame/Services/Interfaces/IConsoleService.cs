using System.Collections.Generic;
using System.Threading.Tasks;
using PacmanGame.Models.Console;

namespace PacmanGame.Services.Interfaces;

public interface IConsoleService
{
    IReadOnlyCollection<IConsoleCommand> Commands { get; }
    Task<ConsoleCommandResult> ExecuteCommandAsync(string input, ConsoleContext context);
}
