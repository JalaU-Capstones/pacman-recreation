using ReactiveUI;
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
    private readonly ILogger _logger;
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
        _logger = new Logger();
        _profileManager = new ProfileManager(_logger);

        // Check if any profiles exist
        var profiles = _profileManager.GetAllProfiles();
        if (profiles.Count == 0)
        {
            _currentViewModel = new ProfileCreationViewModel(this, _profileManager, _logger);
        }
        else
        {
            _currentViewModel = new ProfileSelectionViewModel(this, _profileManager, _logger);
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
