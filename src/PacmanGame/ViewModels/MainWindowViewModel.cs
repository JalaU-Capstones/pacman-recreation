using ReactiveUI;
using System.Reactive;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;

namespace PacmanGame.ViewModels;

/// <summary>
/// ViewModel for the main application window.
/// Manages navigation between different views (menu, game, scoreboard, etc.)
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase _currentViewModel;
    private readonly IProfileManager _profileManager;

    /// <summary>
    /// The currently displayed ViewModel (for view navigation)
    /// </summary>
    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => this.RaiseAndSetIfChanged(ref _currentViewModel, value);
    }

    public MainWindowViewModel()
    {
        // Initialize services
        _profileManager = new ProfileManager();

        // Check if any profiles exist
        var profiles = _profileManager.GetAllProfiles();
        if (profiles.Count == 0)
        {
            _currentViewModel = new ProfileCreationViewModel(this, _profileManager);
        }
        else
        {
            _currentViewModel = new ProfileSelectionViewModel(this, _profileManager);
        }
    }

    /// <summary>
    /// Navigate to a specific view
    /// </summary>
    public void NavigateTo(ViewModelBase viewModel)
    {
        CurrentViewModel = viewModel;
    }
}
