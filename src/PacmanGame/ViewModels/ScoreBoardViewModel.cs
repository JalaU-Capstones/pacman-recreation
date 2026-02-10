using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using PacmanGame.Models.Game;
using PacmanGame.Services.Interfaces;

namespace PacmanGame.ViewModels;

/// <summary>
/// ViewModel for the score board screen.
/// Displays high scores and player rankings.
/// </summary>
public class ScoreBoardViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IProfileManager _profileManager;
    private readonly IAudioManager _audioManager;
    private readonly ILogger _logger;

    private ObservableCollection<ScoreEntry> _scores;

    /// <summary>
    /// Collection of high scores
    /// </summary>
    public ObservableCollection<ScoreEntry> Scores
    {
        get => _scores;
        set => this.RaiseAndSetIfChanged(ref _scores, value);
    }

    public ReactiveCommand<Unit, Unit> ReturnToMenuCommand { get; }

    public ScoreBoardViewModel(MainWindowViewModel mainWindowViewModel, IProfileManager profileManager, IAudioManager audioManager, ILogger logger)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _profileManager = profileManager;
        _audioManager = audioManager;
        _logger = logger;

        _scores = new ObservableCollection<ScoreEntry>();

        // Initialize commands
        ReturnToMenuCommand = ReactiveCommand.Create(ReturnToMenu);

        // Load scores
        LoadScores();
    }

    /// <summary>
    /// Load high scores from file
    /// </summary>
    private void LoadScores()
    {
        var topScores = _profileManager.GetTopScores(10);
        Scores.Clear();

        int rank = 1;
        foreach (var score in topScores)
        {
            score.Rank = rank++;
            Scores.Add(score);
        }

        _logger.Info($"Loaded {Scores.Count} high scores");
    }

    /// <summary>
    /// Return to main menu
    /// </summary>
    private void ReturnToMenu()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _mainWindowViewModel.NavigateTo(new MainMenuViewModel(_mainWindowViewModel, _profileManager, _audioManager, _logger));
    }
}
