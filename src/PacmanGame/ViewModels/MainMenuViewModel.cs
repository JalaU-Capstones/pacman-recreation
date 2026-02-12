using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Reactive;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;

namespace PacmanGame.ViewModels;

/// <summary>
/// ViewModel for the main menu.
/// Handles menu navigation and game start.
/// </summary>
public class MainMenuViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IAudioManager _audioManager;
    private readonly ILogger<MainMenuViewModel> _logger;

    public ReactiveCommand<Unit, Unit> StartGameCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowScoreBoardCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowMultiplayerCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }

    public MainMenuViewModel(MainWindowViewModel mainWindowViewModel, IAudioManager audioManager, ILogger<MainMenuViewModel> logger)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _audioManager = audioManager;
        _logger = logger;

        // Play menu music
        _audioManager.PlayMusic("menu-theme.wav", loop: true);

        // Initialize commands
        StartGameCommand = ReactiveCommand.Create(StartGame);
        ShowScoreBoardCommand = ReactiveCommand.Create(ShowScoreBoard);
        ShowMultiplayerCommand = ReactiveCommand.Create(ShowMultiplayer);
        ShowSettingsCommand = ReactiveCommand.Create(ShowSettings);
        ExitCommand = ReactiveCommand.Create(Exit);
    }

    private void StartGame()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _audioManager.StopMusic();
        _mainWindowViewModel.NavigateTo<GameViewModel>();
    }

    private void ShowScoreBoard()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _audioManager.StopMusic();
        _mainWindowViewModel.NavigateTo<ScoreBoardViewModel>();
    }

    private void ShowMultiplayer()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _audioManager.StopMusic();
        _mainWindowViewModel.NavigateTo<MultiplayerMenuViewModel>();
    }

    private void ShowSettings()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _mainWindowViewModel.NavigateTo<SettingsViewModel>();
    }

    private void Exit()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _logger.LogInformation("Application exit requested by user.");
        Environment.Exit(0);
    }
}
