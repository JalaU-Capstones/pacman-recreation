using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
using ReactiveUI;
using PacmanGame.Shared;

namespace PacmanGame.ViewModels;

public class LeaderboardEntryViewModel
{
    public int Rank { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public int HighScore { get; set; }
}

public class GlobalLeaderboardViewModel : ViewModelBase
{
    private readonly GlobalLeaderboardCache _cache;
    private readonly IProfileManager _profileManager;
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly ILogger<GlobalLeaderboardViewModel> _logger;

    public ObservableCollection<LeaderboardEntryViewModel> Top10Entries { get; } = new();

    private bool _canSubmitScore;
    public bool CanSubmitScore
    {
        get => _canSubmitScore;
        set => this.RaiseAndSetIfChanged(ref _canSubmitScore, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public ICommand SubmitScoreCommand { get; }
    public ICommand BackToLocalScoresCommand { get; }

    /// <summary>
    /// Parameterless constructor for the Avalonia XAML designer.
    /// The runtime uses DI and the main constructor below.
    /// </summary>
    public GlobalLeaderboardViewModel()
    {
        _cache = null!;
        _profileManager = null!;
        _mainWindowViewModel = null!;
        _logger = NullLogger<GlobalLeaderboardViewModel>.Instance;

        CanSubmitScore = false;
        IsLoading = false;

        SubmitScoreCommand = ReactiveCommand.Create(() => { });
        BackToLocalScoresCommand = ReactiveCommand.Create(() => { });

        Top10Entries.Add(new LeaderboardEntryViewModel { Rank = 1, ProfileName = "PlayerOne", HighScore = 12345 });
        Top10Entries.Add(new LeaderboardEntryViewModel { Rank = 2, ProfileName = "PlayerTwo", HighScore = 9000 });
        Top10Entries.Add(new LeaderboardEntryViewModel { Rank = 3, ProfileName = "PlayerThree", HighScore = 7000 });
    }

    public GlobalLeaderboardViewModel(
        GlobalLeaderboardCache cache,
        IProfileManager profileManager,
        MainWindowViewModel mainWindowViewModel,
        ILogger<GlobalLeaderboardViewModel> logger)
    {
        _cache = cache;
        _profileManager = profileManager;
        _mainWindowViewModel = mainWindowViewModel;
        _logger = logger;

        SubmitScoreCommand = ReactiveCommand.CreateFromTask(SubmitScoreAsync);
        BackToLocalScoresCommand = ReactiveCommand.Create(BackToLocalScores);

        Initialize();
    }

    private async void Initialize()
    {
        try
        {
            var profile = await _profileManager.GetCurrentProfileAsync();
            CanSubmitScore = profile != null && profile.HasCompletedAllLevels;

            await LoadLeaderboardAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize GlobalLeaderboardViewModel");
        }
    }

    public async Task LoadLeaderboardAsync()
    {
        IsLoading = true;
        try
        {
            var entries = await _cache.GetTop10Async();
            Top10Entries.Clear();

            int rank = 1;
            foreach (var entry in entries)
            {
                Top10Entries.Add(new LeaderboardEntryViewModel
                {
                    Rank = rank++,
                    ProfileName = entry.ProfileName,
                    HighScore = entry.HighScore
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load global leaderboard");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SubmitScoreAsync()
    {
        try
        {
            var profile = await _profileManager.GetCurrentProfileAsync();

            if (profile == null || !profile.HasCompletedAllLevels)
            {
                return;
            }

            if (string.IsNullOrEmpty(profile.GlobalProfileId))
            {
                profile.GlobalProfileId = Guid.NewGuid().ToString();
                await _profileManager.UpdateProfileAsync(profile);
            }

            var scoreToSend = profile.HighScore;
            if (scoreToSend > 0)
            {
                await _cache.SubmitScoreAsync(
                    profile.GlobalProfileId,
                    profile.Name,
                    scoreToSend);

                // Refresh view
                await LoadLeaderboardAsync();
            }
        }
        catch (Exception ex)
        {
            // Prevent ReactiveCommand pipelines from faulting and terminating the app.
            _logger.LogError(ex, "Failed to submit global score");
        }
    }

    private void BackToLocalScores()
    {
        _mainWindowViewModel.NavigateTo<ScoreBoardViewModel>();
    }
}
