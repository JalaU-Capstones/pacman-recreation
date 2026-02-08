using System;
using System.Collections.Generic;
using System.Reactive;
using PacmanGame.Services.Interfaces;
using ReactiveUI;

namespace PacmanGame.ViewModels;

public class ProfileCreationViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IProfileManager _profileManager;
    private string _name = string.Empty;
    private string _selectedColor = "#FFFF00"; // Default Pacman Yellow
    private string _errorMessage = string.Empty;

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public string SelectedColor
    {
        get => _selectedColor;
        set => this.RaiseAndSetIfChanged(ref _selectedColor, value);
    }

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

    public ReactiveCommand<Unit, Unit> CreateProfileCommand { get; }
    public ReactiveCommand<string, Unit> SelectColorCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public ProfileCreationViewModel(MainWindowViewModel mainWindowViewModel, IProfileManager profileManager)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _profileManager = profileManager;

        CreateProfileCommand = ReactiveCommand.Create(CreateProfile);
        SelectColorCommand = ReactiveCommand.Create<string>(color => SelectedColor = color);
        CancelCommand = ReactiveCommand.Create(Cancel);
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

        // Simple alphanumeric check
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
            _mainWindowViewModel.NavigateTo(new MainMenuViewModel(_mainWindowViewModel, _profileManager));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error creating profile: {ex.Message}";
        }
    }

    private void Cancel()
    {
        var profiles = _profileManager.GetAllProfiles();
        if (profiles.Count > 0)
        {
            _mainWindowViewModel.NavigateTo(new ProfileSelectionViewModel(_mainWindowViewModel, _profileManager));
        }
        else
        {
            // If no profiles exist, we can't cancel, must create one.
            ErrorMessage = "You must create a profile to play.";
        }
    }
}
