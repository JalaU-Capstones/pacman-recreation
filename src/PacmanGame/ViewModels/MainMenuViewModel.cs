using ReactiveUI;
using System;
using System.Reactive;
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

    public ReactiveCommand<Unit, Unit> StartGameCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowScoreBoardCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }

    public MainMenuViewModel(MainWindowViewModel mainWindowViewModel, IProfileManager profileManager, IAudioManager? audioManager = null)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _profileManager = profileManager;

        // If audioManager is not provided, try to get it from a service locator or create a new one
        // For now, we'll assume it's passed in or we can't play music
        _audioManager = audioManager ?? new PacmanGame.Services.AudioManager();
        _audioManager.Initialize();

        // Play menu music
        _audioManager.PlayMusic("menu-theme.wav", loop: true);

        // Initialize commands
        StartGameCommand = ReactiveCommand.Create(StartGame);
        ShowScoreBoardCommand = ReactiveCommand.Create(ShowScoreBoard);
        ShowSettingsCommand = ReactiveCommand.Create(ShowSettings);
        ExitCommand = ReactiveCommand.Create(Exit);
    }

    private void StartGame()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _audioManager.StopMusic();
        _mainWindowViewModel.NavigateTo(new GameViewModel(_mainWindowViewModel, _profileManager, _audioManager));
    }

    private void ShowScoreBoard()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _audioManager.StopMusic();
        _mainWindowViewModel.NavigateTo(new ScoreBoardViewModel(_mainWindowViewModel, _profileManager, _audioManager));
    }

    private void ShowSettings()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _mainWindowViewModel.NavigateTo(new SettingsViewModel(_mainWindowViewModel, _profileManager, _audioManager));
    }

    private void Exit()
    {
        _audioManager.PlaySoundEffect("menu-select");
        Environment.Exit(0);
    }
}
