using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using PacmanGame.Models.Game;
using PacmanGame.Services.Interfaces;
using ReactiveUI;

namespace PacmanGame.ViewModels;

public class ProfileSelectionViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IProfileManager _profileManager;
    private readonly IAudioManager _audioManager;

    public ObservableCollection<Profile> Profiles { get; } = new();

    public ReactiveCommand<Profile, Unit> SelectProfileCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateNewProfileCommand { get; }
    public ReactiveCommand<Profile, Unit> DeleteProfileCommand { get; }

    public ProfileSelectionViewModel(MainWindowViewModel mainWindowViewModel, IProfileManager profileManager, IAudioManager? audioManager = null)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _profileManager = profileManager;
        _audioManager = audioManager ?? new PacmanGame.Services.AudioManager();
        if (audioManager == null)
        {
            _audioManager.Initialize();
        }

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

        _mainWindowViewModel.NavigateTo(new MainMenuViewModel(_mainWindowViewModel, _profileManager, _audioManager));
    }

    private void CreateNewProfile()
    {
        _mainWindowViewModel.NavigateTo(new ProfileCreationViewModel(_mainWindowViewModel, _profileManager, _audioManager));
    }

    private void DeleteProfile(Profile profile)
    {
        _profileManager.DeleteProfile(profile.Id);
        LoadProfiles();

        if (Profiles.Count == 0)
        {
            CreateNewProfile();
        }
    }
}
