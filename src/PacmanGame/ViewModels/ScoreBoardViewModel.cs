using PacmanGame.Models.Game;
using PacmanGame.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ReactiveUI;

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
    public ObservableCollection<ScoreEntry> Scores
    {
        get => _scores;
        set => this.RaiseAndSetIfChanged(ref _scores, value);
    }

    public ICommand ReturnToMenuCommand { get; }

    public ScoreBoardViewModel(MainWindowViewModel mainWindowViewModel, IProfileManager profileManager, IAudioManager audioManager, ILogger logger)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _profileManager = profileManager;
        _audioManager = audioManager;
        _logger = logger;

        _scores = new ObservableCollection<ScoreEntry>();

        ReturnToMenuCommand = ReactiveCommand.Create(ReturnToMenu);

        LoadScores();
    }

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

    private void ReturnToMenu()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _mainWindowViewModel.NavigateTo(new MainMenuViewModel(_mainWindowViewModel, _profileManager, _audioManager, _logger));
    }
}
