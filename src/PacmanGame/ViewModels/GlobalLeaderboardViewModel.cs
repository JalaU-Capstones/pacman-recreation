using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
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

    public GlobalLeaderboardViewModel(
        GlobalLeaderboardCache cache,
        IProfileManager profileManager,
        MainWindowViewModel mainWindowViewModel)
    {
        _cache = cache;
        _profileManager = profileManager;
        _mainWindowViewModel = mainWindowViewModel;

        SubmitScoreCommand = ReactiveCommand.CreateFromTask(SubmitScoreAsync);
        BackToLocalScoresCommand = ReactiveCommand.Create(BackToLocalScores);

        Initialize();
    }

    private async void Initialize()
    {
        var profile = await _profileManager.GetCurrentProfileAsync();
        CanSubmitScore = profile != null && profile.HasCompletedAllLevels;

        await LoadLeaderboardAsync();
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
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SubmitScoreAsync()
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

        await _cache.SubmitScoreAsync(
            profile.GlobalProfileId,
            profile.Name,
            profile.HighScore);

        // Refresh view
        await LoadLeaderboardAsync();
    }

    private void BackToLocalScores()
    {
        _mainWindowViewModel.NavigateTo<ScoreBoardViewModel>();
    }
}
