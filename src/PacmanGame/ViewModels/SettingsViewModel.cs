using Microsoft.Extensions.Logging;
using PacmanGame.Models.Game;
using PacmanGame.Services.Interfaces;
using System.Windows.Input;
using ReactiveUI;
using System;

namespace PacmanGame.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IProfileManager _profileManager;
    private readonly IAudioManager _audioManager;
    private readonly ILogger<SettingsViewModel> _logger;

    private Profile? _activeProfile;
    public Profile? ActiveProfile
    {
        get => _activeProfile;
        set => this.RaiseAndSetIfChanged(ref _activeProfile, value);
    }

    private bool _isDeleteConfirmationVisible;
    public bool IsDeleteConfirmationVisible
    {
        get => _isDeleteConfirmationVisible;
        set => this.RaiseAndSetIfChanged(ref _isDeleteConfirmationVisible, value);
    }

    private bool _isMusicEnabled;
    public bool IsMusicEnabled
    {
        get => _isMusicEnabled;
        set => this.RaiseAndSetIfChanged(ref _isMusicEnabled, value);
    }

    private int _menuMusicVolume;
    public int MenuMusicVolume
    {
        get => _menuMusicVolume;
        set => this.RaiseAndSetIfChanged(ref _menuMusicVolume, value);
    }

    private int _gameMusicVolume;
    public int GameMusicVolume
    {
        get => _gameMusicVolume;
        set => this.RaiseAndSetIfChanged(ref _gameMusicVolume, value);
    }

    private int _sfxVolume;
    public int SfxVolume
    {
        get => _sfxVolume;
        set => this.RaiseAndSetIfChanged(ref _sfxVolume, value);
    }

    public ICommand SwitchProfileCommand { get; }
    public ICommand ShowDeleteConfirmationCommand { get; }
    public ICommand CancelDeleteCommand { get; }
    public ICommand ConfirmDeleteCommand { get; }
    public ICommand ReturnToMenuCommand { get; }

    public SettingsViewModel(MainWindowViewModel mainWindowViewModel, IProfileManager profileManager, IAudioManager audioManager, ILogger<SettingsViewModel> logger)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _profileManager = profileManager;
        _audioManager = audioManager;
        _logger = logger;

        _activeProfile = _profileManager.GetActiveProfile();
        _isMusicEnabled = !_audioManager.IsMuted;
        _menuMusicVolume = (int)(_audioManager.MenuMusicVolume * 100);
        _gameMusicVolume = (int)(_audioManager.GameMusicVolume * 100);
        _sfxVolume = (int)(_audioManager.SfxVolume * 100);

        SwitchProfileCommand = ReactiveCommand.Create(SwitchProfile);
        ShowDeleteConfirmationCommand = ReactiveCommand.Create(() => IsDeleteConfirmationVisible = true);
        CancelDeleteCommand = ReactiveCommand.Create(() => IsDeleteConfirmationVisible = false);
        ConfirmDeleteCommand = ReactiveCommand.Create(ConfirmDelete);
        ReturnToMenuCommand = ReactiveCommand.Create(ReturnToMenu);

        this.WhenAnyValue(x => x.IsMusicEnabled).Subscribe(value =>
        {
            _audioManager.SetMuted(!value);
            SaveSettings();
        });

        this.WhenAnyValue(x => x.MenuMusicVolume).Subscribe(value =>
        {
            _audioManager.SetMenuMusicVolume(value / 100f);
            SaveSettings();
        });

        this.WhenAnyValue(x => x.GameMusicVolume).Subscribe(value =>
        {
            _audioManager.SetGameMusicVolume(value / 100f);
            SaveSettings();
        });

        this.WhenAnyValue(x => x.SfxVolume).Subscribe(value =>
        {
            _audioManager.SetSfxVolume(value / 100f);
            SaveSettings();
        });
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
        _mainWindowViewModel.NavigateTo<ProfileSelectionViewModel>();
    }

    private void ConfirmDelete()
    {
        if (ActiveProfile != null)
        {
            _audioManager.PlaySoundEffect("menu-select");
            _profileManager.DeleteProfile(ActiveProfile.Id);

            var profiles = _profileManager.GetAllProfiles();
            if (profiles.Count > 0)
            {
                _mainWindowViewModel.NavigateTo<ProfileSelectionViewModel>();
            }
            else
            {
                _mainWindowViewModel.NavigateTo<ProfileCreationViewModel>();
            }
        }
    }

    private void ReturnToMenu()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _mainWindowViewModel.NavigateTo<MainMenuViewModel>();
    }
}
