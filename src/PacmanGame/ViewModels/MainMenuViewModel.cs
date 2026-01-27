using ReactiveUI;
using System;
using System.Reactive;

namespace PacmanGame.ViewModels;

/// <summary>
/// ViewModel for the main menu.
/// Handles menu navigation and game start.
/// </summary>
public class MainMenuViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    public ReactiveCommand<Unit, Unit> StartGameCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowScoreBoardCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }

    public MainMenuViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;

        // Initialize commands
        StartGameCommand = ReactiveCommand.Create(StartGame);
        ShowScoreBoardCommand = ReactiveCommand.Create(ShowScoreBoard);
        ShowSettingsCommand = ReactiveCommand.Create(ShowSettings);
        ExitCommand = ReactiveCommand.Create(Exit);
    }

    private void StartGame()
    {
        // TODO: Play menu select sound
        _mainWindowViewModel.NavigateTo(new GameViewModel(_mainWindowViewModel));
    }

    private void ShowScoreBoard()
    {
        // TODO: Play menu select sound
        _mainWindowViewModel.NavigateTo(new ScoreBoardViewModel(_mainWindowViewModel));
    }

    private void ShowSettings()
    {
        // TODO: Play menu select sound
        // TODO: Implement settings view
        Console.WriteLine("Settings - Not implemented yet");
    }

    private void Exit()
    {
        // TODO: Play menu select sound
        Environment.Exit(0);
    }
}
