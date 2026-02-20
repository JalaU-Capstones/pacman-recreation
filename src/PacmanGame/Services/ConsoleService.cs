using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PacmanGame.Models.Console;
using PacmanGame.Services.Interfaces;

namespace PacmanGame.Services;

public class ConsoleService : IConsoleService
{
    private readonly IReadOnlyCollection<IConsoleCommand> _commands;
    private readonly ILogger<ConsoleService> _logger;

    public IReadOnlyCollection<IConsoleCommand> Commands => _commands;

    public ConsoleService(IEnumerable<IConsoleCommand> commands, ILogger<ConsoleService> logger)
    {
        _commands = commands.OrderBy(c => c.Name).ToList().AsReadOnly();
        _logger = logger;
    }

    public async Task<ConsoleCommandResult> ExecuteCommandAsync(string input, ConsoleContext context)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new ConsoleCommandResult { Message = string.Empty, MessageType = ConsoleMessageType.Info };
        }

        var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var commandName = parts[0].StartsWith('/') ? parts[0][1..] : parts[0];
        var args = parts.Skip(1).ToArray();

        var command = _commands.FirstOrDefault(c => string.Equals(c.Name, commandName, StringComparison.OrdinalIgnoreCase)
            || c.Aliases.Contains(commandName, StringComparer.OrdinalIgnoreCase));

        if (command == null)
        {
            _logger.LogWarning("Console command not found: {Command}", commandName);
            return new ConsoleCommandResult
            {
                Message = $"Command not found: {commandName}",
                MessageType = ConsoleMessageType.Error
            };
        }

        _logger.LogInformation("Executing console command {Name} with args {Args}", command.Name, args);
        try
        {
            return await command.ExecuteAsync(args, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Console command execution failed: {Name}", command.Name);
            return new ConsoleCommandResult
            {
                Message = "An unexpected error occurred while running the command.",
                MessageType = ConsoleMessageType.Error
            };
        }
    }
}
