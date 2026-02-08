using System.Reactive;
using PacmanGame.Models.Game;
using PacmanGame.Services.Interfaces;
using ReactiveUI;

namespace PacmanGame.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IProfileManager _profileManager;
    private readonly IAudioManager _audioManager;

    private Profile? _activeProfile;
    private bool _isDeleteConfirmationVisible;
    private bool _isMusicEnabled;
    // Removed unused field _isSfxEnabled

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
        IsMusicEnabled = !_audioManager.IsMuted;

        SwitchProfileCommand = ReactiveCommand.Create(SwitchProfile);

        // Fix: Explicitly return Unit.Default for commands that just set a property
        ShowDeleteConfirmationCommand = ReactiveCommand.Create(() => {
            IsDeleteConfirmationVisible = true;
        });

        CancelDeleteCommand = ReactiveCommand.Create(() => {
            IsDeleteConfirmationVisible = false;
        });

        ConfirmDeleteCommand = ReactiveCommand.Create(ConfirmDelete);
        ReturnToMenuCommand = ReactiveCommand.Create(ReturnToMenu);
    }

    private void SwitchProfile()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _mainWindowViewModel.NavigateTo(new ProfileSelectionViewModel(_mainWindowViewModel, _profileManager));
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
                _mainWindowViewModel.NavigateTo(new ProfileSelectionViewModel(_mainWindowViewModel, _profileManager));
            }
            else
            {
                _mainWindowViewModel.NavigateTo(new ProfileCreationViewModel(_mainWindowViewModel, _profileManager));
            }
        }
    }

    private void ReturnToMenu()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _mainWindowViewModel.NavigateTo(new MainMenuViewModel(_mainWindowViewModel, _profileManager, _audioManager));
    }
}
