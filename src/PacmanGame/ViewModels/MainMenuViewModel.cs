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
    private readonly IProfileManager _profileManager;
    private readonly IAudioManager _audioManager;
    private readonly ILogger _logger;

    public ReactiveCommand<Unit, Unit> StartGameCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowScoreBoardCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowMultiplayerCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }

    public MainMenuViewModel(MainWindowViewModel mainWindowViewModel, IProfileManager profileManager, IAudioManager audioManager, ILogger logger)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _profileManager = profileManager;
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
        _mainWindowViewModel.NavigateTo(new GameViewModel(_mainWindowViewModel, _profileManager, _audioManager, _logger));
    }

    private void ShowScoreBoard()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _audioManager.StopMusic();
        _mainWindowViewModel.NavigateTo(new ScoreBoardViewModel(_mainWindowViewModel, _profileManager, _audioManager, _logger));
    }

    private void ShowMultiplayer()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _audioManager.StopMusic();
        _mainWindowViewModel.NavigateTo(new MultiplayerMenuViewModel(_mainWindowViewModel, NetworkService.Instance, _audioManager, _logger, _profileManager));
    }

    private void ShowSettings()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _mainWindowViewModel.NavigateTo(new SettingsViewModel(_mainWindowViewModel, _profileManager, _audioManager, _logger));
    }

    private void Exit()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _logger.Info("Application exit requested by user.");
        Environment.Exit(0);
    }
}
