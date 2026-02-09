using System.Reactive;
using PacmanGame.Models.Game;
using PacmanGame.Services.Interfaces;
using ReactiveUI;
using System;

namespace PacmanGame.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IProfileManager _profileManager;
    private readonly IAudioManager _audioManager;

    private Profile? _activeProfile;
    private bool _isDeleteConfirmationVisible;
    private bool _isMusicEnabled;
    private int _menuMusicVolume;
    private int _gameMusicVolume;
    private int _sfxVolume;

    public Profile? ActiveProfile
    {
        get => _activeProfile;
        set => this.RaiseAndSetIfChanged(ref _activeProfile, value);
    }

    public bool IsDeleteConfirmationVisible
    {
        get => _isDeleteConfirmationVisible;
        set => this.RaiseAndSetIfChanged(ref _isDeleteConfirmationVisible, value);
    }

    public bool IsMusicEnabled
    {
        get => _isMusicEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref _isMusicEnabled, value);
            _audioManager.SetMuted(!value);
            SaveSettings();
        }
    }

    public int MenuMusicVolume
    {
        get => _menuMusicVolume;
        set
        {
            this.RaiseAndSetIfChanged(ref _menuMusicVolume, value);
            _audioManager.SetMenuMusicVolume(value / 100f);
            SaveSettings();
        }
    }

    public int GameMusicVolume
    {
        get => _gameMusicVolume;
        set
        {
            this.RaiseAndSetIfChanged(ref _gameMusicVolume, value);
            _audioManager.SetGameMusicVolume(value / 100f);
            SaveSettings();
        }
    }

    public int SfxVolume
    {
        get => _sfxVolume;
        set
        {
            this.RaiseAndSetIfChanged(ref _sfxVolume, value);
            _audioManager.SetSfxVolume(value / 100f);
            SaveSettings();
        }
    }

    public ReactiveCommand<Unit, Unit> SwitchProfileCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowDeleteConfirmationCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelDeleteCommand { get; }
    public ReactiveCommand<Unit, Unit> ConfirmDeleteCommand { get; }
    public ReactiveCommand<Unit, Unit> ReturnToMenuCommand { get; }

    public SettingsViewModel(MainWindowViewModel mainWindowViewModel, IProfileManager profileManager, IAudioManager audioManager)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _profileManager = profileManager;
        _audioManager = audioManager;

        ActiveProfile = _profileManager.GetActiveProfile();
        // Avoid invoking the IsMusicEnabled property (which calls SaveSettings) during construction
        // because the volume fields haven't been initialized yet and would be saved as zeros.
        _isMusicEnabled = !_audioManager.IsMuted;

        // Initialize volumes from AudioManager
        _menuMusicVolume = (int)(_audioManager.MenuMusicVolume * 100);
        _gameMusicVolume = (int)(_audioManager.GameMusicVolume * 100);
        _sfxVolume = (int)(_audioManager.SfxVolume * 100);

        SwitchProfileCommand = ReactiveCommand.Create(SwitchProfile);

        ShowDeleteConfirmationCommand = ReactiveCommand.Create(() => {
            IsDeleteConfirmationVisible = true;
        });

        CancelDeleteCommand = ReactiveCommand.Create(() => {
            IsDeleteConfirmationVisible = false;
        });

        ConfirmDeleteCommand = ReactiveCommand.Create(ConfirmDelete);
        ReturnToMenuCommand = ReactiveCommand.Create(ReturnToMenu);
    }

    private void SaveSettings()
    {
        if (ActiveProfile == null) return;

        var settings = new Settings
        {
            ProfileId = ActiveProfile.Id,
            MenuMusicVolume = MenuMusicVolume / 100.0,
            GameMusicVolume = GameMusicVolume / 100.0,
            SfxVolume = SfxVolume / 100.0,
            IsMuted = !IsMusicEnabled
        };
        _profileManager.SaveSettings(ActiveProfile.Id, settings);
    }

    private void SwitchProfile()
    {
        _audioManager.PlaySoundEffect("menu-select");
        // Pass the existing audio manager to preserve state/resources
        _mainWindowViewModel.NavigateTo(new ProfileSelectionViewModel(_mainWindowViewModel, _profileManager, _audioManager));
    }

    private void ConfirmDelete()
    {
        if (ActiveProfile != null)
        {
            _audioManager.PlaySoundEffect("menu-select");
            _profileManager.DeleteProfile(ActiveProfile.Id);

            // Navigate back to profile selection (or creation if no profiles left)
            var profiles = _profileManager.GetAllProfiles();
            if (profiles.Count > 0)
            {
                _mainWindowViewModel.NavigateTo(new ProfileSelectionViewModel(_mainWindowViewModel, _profileManager, _audioManager));
            }
            else
            {
                _mainWindowViewModel.NavigateTo(new ProfileCreationViewModel(_mainWindowViewModel, _profileManager, _audioManager));
            }
        }
    }

    private void ReturnToMenu()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _mainWindowViewModel.NavigateTo(new MainMenuViewModel(_mainWindowViewModel, _profileManager, _audioManager));
    }
}
