using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using PacmanGame.Models.Game;

namespace PacmanGame.ViewModels;

/// <summary>
/// ViewModel for the score board screen.
/// Displays high scores and player rankings.
/// </summary>
public class ScoreBoardViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;

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

    public ScoreBoardViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
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
        // TODO: Load from ScoreManager service
        // For now, add some dummy data
        Scores.Add(new ScoreEntry { Rank = 1, PlayerName = "AAA", Score = 10000, Date = DateTime.Now.AddDays(-7) });
        Scores.Add(new ScoreEntry { Rank = 2, PlayerName = "BBB", Score = 8500, Date = DateTime.Now.AddDays(-5) });
        Scores.Add(new ScoreEntry { Rank = 3, PlayerName = "CCC", Score = 7200, Date = DateTime.Now.AddDays(-3) });
        Scores.Add(new ScoreEntry { Rank = 4, PlayerName = "DDD", Score = 6800, Date = DateTime.Now.AddDays(-2) });
        Scores.Add(new ScoreEntry { Rank = 5, PlayerName = "EEE", Score = 5500, Date = DateTime.Now.AddDays(-1) });

        Console.WriteLine($"Loaded {Scores.Count} high scores");
    }

    /// <summary>
    /// Return to main menu
    /// </summary>
    private void ReturnToMenu()
    {
        // TODO: Play menu select sound
        _mainWindowViewModel.NavigateTo(new MainMenuViewModel(_mainWindowViewModel));
    }
}
