using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using PacmanGame;
using PacmanGame.Models.Console;
using PacmanGame.Services.Interfaces;
using ReactiveUI;
using System.Reactive;

namespace PacmanGame.ViewModels;

public class ConsoleViewModel : ViewModelBase
{
    private readonly IConsoleService _consoleService;
    private readonly IProfileManager _profileManager;
    private readonly ILogger<ConsoleViewModel> _logger;

    private string _currentCommand = string.Empty;
    public string CurrentCommand
    {
        get => _currentCommand;
        set => this.RaiseAndSetIfChanged(ref _currentCommand, value);
    }

    private bool _isVisible;
    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    public ObservableCollection<ConsoleMessage> CommandHistory { get; } = new();

    public ReactiveCommand<Unit, Unit> SubmitCommand { get; }

    private bool _introDisplayed;

    public ConsoleViewModel(
        IConsoleService consoleService,
        IProfileManager profileManager,
        ILogger<ConsoleViewModel> logger)
    {
        _consoleService = consoleService;
        _profileManager = profileManager;
        _logger = logger;

        SubmitCommand = ReactiveCommand.CreateFromTask(ExecuteCurrentCommandAsync);
    }

    public void Open()
    {
        if (IsVisible) return;
        IsVisible = true;
        if (!_introDisplayed)
        {
            AppendMessage(new ConsoleMessage("Pac-Man Recreation Console v1.0.0", ConsoleMessageType.System));
            AppendMessage(new ConsoleMessage("Type '*/' for a list of available commands.", ConsoleMessageType.System));
            _introDisplayed = true;
        }
    }

    public void Close()
    {
        IsVisible = false;
        CurrentCommand = string.Empty;
    }

    private async Task ExecuteCurrentCommandAsync()
    {
        var input = CurrentCommand?.Trim();
        if (string.IsNullOrEmpty(input))
        {
            return;
        }

        AppendMessage(new ConsoleMessage($"pacman-recreation/{_profileManager.GetActiveProfile()?.Name ?? "guest"}/$ {input}", ConsoleMessageType.Input));
        CurrentCommand = string.Empty;

        var context = new ConsoleContext(
            App.GetService<MainWindowViewModel>() ?? throw new InvalidOperationException("MainWindowViewModel not available"),
            _profileManager.GetActiveProfile(),
            _profileManager,
            () => _consoleService.Commands);

        var result = await _consoleService.ExecuteCommandAsync(input, context);

        if (result.ClearHistory)
        {
            CommandHistory.Clear();
        }

        if (!string.IsNullOrEmpty(result.Message))
        {
            AppendMessage(new ConsoleMessage(result.Message, result.MessageType));
        }

        if (result.CloseConsole)
        {
            Close();
        }
    }

    private void AppendMessage(ConsoleMessage message)
    {
        CommandHistory.Add(message);
    }
}
