using Microsoft.Extensions.Logging;
using PacmanGame.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Windows.Input;
using ReactiveUI;

namespace PacmanGame.ViewModels;

public class ProfileCreationViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IProfileManager _profileManager;
    private readonly IAudioManager _audioManager;
    private readonly ILogger<ProfileCreationViewModel> _logger;

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private string _selectedColor = "#FFFF00"; // Default Pacman Yellow
    public string SelectedColor
    {
        get => _selectedColor;
        set => this.RaiseAndSetIfChanged(ref _selectedColor, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    public List<string> AvailableColors { get; } = new()
    {
        "#FFFF00", // Yellow
        "#FF0000", // Red
        "#FFB8FF", // Pink
        "#00FFFF", // Cyan
        "#FFB852", // Orange
        "#00FF00", // Green
        "#FFFFFF", // White
        "#9999FF"  // Lavender
    };

    public ICommand CreateProfileCommand { get; }
    public ReactiveCommand<string?, Unit> SelectColorCommand { get; }
    public ICommand CancelCommand { get; }

    public ProfileCreationViewModel(MainWindowViewModel mainWindowViewModel, IProfileManager profileManager, IAudioManager audioManager, ILogger<ProfileCreationViewModel> logger)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _profileManager = profileManager;
        _audioManager = audioManager;
        _logger = logger;

        CreateProfileCommand = ReactiveCommand.Create(CreateProfile);
        SelectColorCommand = ReactiveCommand.Create<string?>(color =>
        {
            if (color != null)
            {
                SelectedColor = color;
            }
        });
        CancelCommand = ReactiveCommand.Create(Cancel);

        // Play menu music when profile creation screen is shown
        _audioManager.PlayMusic("menu-theme.wav", loop: true);
    }

    private void CreateProfile()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Name cannot be empty.";
            return;
        }

        if (Name.Length < 3 || Name.Length > 15)
        {
            ErrorMessage = "Name must be between 3 and 15 characters.";
            return;
        }

        foreach (char c in Name)
        {
            if (!char.IsLetterOrDigit(c) && c != ' ')
            {
                ErrorMessage = "Name can only contain letters, numbers, and spaces.";
                return;
            }
        }

        try
        {
            var profile = _profileManager.CreateProfile(Name.Trim(), SelectedColor);
            _profileManager.SetActiveProfile(profile.Id);

            var settings = _profileManager.LoadSettings(profile.Id);
            _audioManager.SetMenuMusicVolume((float)settings.MenuMusicVolume);
            _audioManager.SetGameMusicVolume((float)settings.GameMusicVolume);
            _audioManager.SetSfxVolume((float)settings.SfxVolume);
            _audioManager.SetMuted(settings.IsMuted);

            _mainWindowViewModel.NavigateTo<MainMenuViewModel>();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error creating profile: {ex.Message}";
            _logger.LogError(ex, "Error creating profile");
        }
    }

    private void Cancel()
    {
        var profiles = _profileManager.GetAllProfiles();
        if (profiles.Count > 0)
        {
            _mainWindowViewModel.NavigateTo<ProfileSelectionViewModel>();
        }
        else
        {
            ErrorMessage = "You must create a profile to play.";
        }
    }
}
