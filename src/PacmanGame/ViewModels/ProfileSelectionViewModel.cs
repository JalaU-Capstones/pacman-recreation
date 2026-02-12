using System.Collections.ObjectModel;
using System.Reactive;
using Microsoft.Extensions.Logging;
using PacmanGame.Models.Game;
using PacmanGame.Services.Interfaces;
using ReactiveUI;

namespace PacmanGame.ViewModels;

public class ProfileSelectionViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IProfileManager _profileManager;
    private readonly IAudioManager _audioManager;
    private readonly ILogger<ProfileSelectionViewModel> _logger;

    public ObservableCollection<Profile> Profiles { get; } = new();

    public ReactiveCommand<Profile, Unit> SelectProfileCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateNewProfileCommand { get; }
    public ReactiveCommand<Profile, Unit> DeleteProfileCommand { get; }

    public ProfileSelectionViewModel(MainWindowViewModel mainWindowViewModel, IProfileManager profileManager, IAudioManager audioManager, ILogger<ProfileSelectionViewModel> logger)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _profileManager = profileManager;
        _audioManager = audioManager;
        _logger = logger;

        SelectProfileCommand = ReactiveCommand.Create<Profile>(SelectProfile);
        CreateNewProfileCommand = ReactiveCommand.Create(CreateNewProfile);
        DeleteProfileCommand = ReactiveCommand.Create<Profile>(DeleteProfile);

        LoadProfiles();
    }

    private void LoadProfiles()
    {
        Profiles.Clear();
        var profiles = _profileManager.GetAllProfiles();
        foreach (var profile in profiles)
        {
            Profiles.Add(profile);
        }
        _logger.LogInformation("Loaded {Count} profiles.", Profiles.Count);
    }

    private void SelectProfile(Profile profile)
    {
        _profileManager.SetActiveProfile(profile.Id);

        // Load and apply audio settings for this profile
        var settings = _profileManager.LoadSettings(profile.Id);
        _audioManager.SetMenuMusicVolume((float)settings.MenuMusicVolume);
        _audioManager.SetGameMusicVolume((float)settings.GameMusicVolume);
        _audioManager.SetSfxVolume((float)settings.SfxVolume);
        _audioManager.SetMuted(settings.IsMuted);

        _mainWindowViewModel.NavigateTo<MainMenuViewModel>();
    }

    private void CreateNewProfile()
    {
        _mainWindowViewModel.NavigateTo<ProfileCreationViewModel>();
    }

    private void DeleteProfile(Profile profile)
    {
        _logger.LogInformation("Deleting profile: {Name}", profile.Name);
        _profileManager.DeleteProfile(profile.Id);
        LoadProfiles();

        if (Profiles.Count == 0)
        {
            CreateNewProfile();
        }
    }
}
