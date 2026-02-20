using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PacmanGame.Models.Console;
using PacmanGame.Services.Interfaces;

namespace PacmanGame.Services.ConsoleCommands;

public class ActiveCommand : ConsoleCommandBase
{
    private readonly ILogger<ActiveCommand> _logger;

    public override string Name => "active";
    public override string Syntax => "/active <service>";
    public override string Description => "Activate an auxiliary service.";

    public override IReadOnlyCollection<string> Aliases => new[] { "activate" };

    public ActiveCommand(ILogger<ActiveCommand> logger)
    {
        _logger = logger;
    }

    public override Task<ConsoleCommandResult> ExecuteAsync(string[] args, ConsoleContext context)
    {
        if (args.Length == 0)
        {
            return Task.FromResult(new ConsoleCommandResult
            {
                Message = "Specify a service to activate. Example: /active creative",
                MessageType = ConsoleMessageType.Info
            });
        }

        var service = args[0].ToLowerInvariant();
        _logger.LogInformation("Activating console service {Service}", service);

        if (service == "creative")
        {
            if (context.ActiveProfile == null)
            {
                return Task.FromResult(new ConsoleCommandResult
                {
                    Message = "Create or select a profile before activating creative mode.",
                    MessageType = ConsoleMessageType.Error
                });
            }

            if (!context.ActiveProfile.HasCompletedAllLevels)
            {
                return Task.FromResult(new ConsoleCommandResult
                {
                    Message = "Error: You must complete all 3 levels to unlock Creative Mode. Current progress: incomplete.",
                    MessageType = ConsoleMessageType.Error
                });
            }

            context.MainWindowViewModel?.NavigateToCreativeMode();

            return Task.FromResult(new ConsoleCommandResult
            {
                Message = "Creative Mode activated. Happy creating!",
                MessageType = ConsoleMessageType.Success,
                CloseConsole = true
            });
        }

        return Task.FromResult(new ConsoleCommandResult
        {
            Message = $"Unknown service '{service}'.",
            MessageType = ConsoleMessageType.Error
        });
    }
}
