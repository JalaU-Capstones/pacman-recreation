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

        // Ensure we are sending the correct high score
        // The profile.HighScore property is populated by GetAllProfiles() which does a MAX(Score) query
        // But GetCurrentProfileAsync() uses GetProfileById() which might not populate HighScore correctly if the query doesn't join Scores
        // Let's double check GetProfileById in ProfileManager.cs

        // Re-fetch the profile to ensure we have the latest data, specifically the HighScore
        // Actually, Profile object from GetProfileById doesn't seem to populate HighScore based on the SQL in ProfileManager.cs
        // Let's check ProfileManager.cs again.

        // In ProfileManager.cs:
        // GetProfileById query: "SELECT Id, Name, AvatarColor, CreatedAt, LastPlayedAt, HasCompletedAllLevels, GlobalProfileId, LastGlobalScoreSubmission FROM Profiles WHERE Id = $id"
        // It does NOT select HighScore. So profile.HighScore is 0 (default).

        // We need to fetch the high score explicitly.
        var topScores = _profileManager.GetTopScores(1); // This gets global top scores, not specific to user.

        // Better approach: Use the existing GetTopScores but filter or just add a method to get high score for a profile.
        // Or just use the fact that we can get all profiles and find ours.
        var allProfiles = _profileManager.GetAllProfiles();
        var myProfileWithScore = allProfiles.Find(p => p.Id == profile.Id);

        int scoreToSend = myProfileWithScore?.HighScore ?? 0;

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

    private void BackToLocalScores()
    {
        _mainWindowViewModel.NavigateTo<ScoreBoardViewModel>();
    }
}
