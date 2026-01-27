using ReactiveUI;
using System.Reactive;

namespace PacmanGame.ViewModels;

/// <summary>
/// ViewModel for the main application window.
/// Manages navigation between different views (menu, game, scoreboard, etc.)
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase _currentViewModel;

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
        // Start with the main menu
        _currentViewModel = new MainMenuViewModel(this);
    }

    /// <summary>
    /// Navigate to a specific view
    /// </summary>
    public void NavigateTo(ViewModelBase viewModel)
    {
        CurrentViewModel = viewModel;
    }
}
