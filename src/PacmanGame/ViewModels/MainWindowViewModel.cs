using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
using ReactiveUI;

namespace PacmanGame.ViewModels;

/// <summary>
/// ViewModel for the main application window.
/// Manages navigation between different views (menu, game, scoreboard, etc.)
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase _currentViewModel;
    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => this.RaiseAndSetIfChanged(ref _currentViewModel, value);
    }

    private readonly ILogger _logger;
    private readonly IProfileManager _profileManager;
    private readonly NetworkService _networkService;
    private readonly IAudioManager _audioManager;

    public MainWindowViewModel()
    {
        _logger = new Logger();
        _profileManager = new ProfileManager(_logger);
        _networkService = NetworkService.Instance;
        _audioManager = new AudioManager(_logger);

        _networkService.Start();

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

    public void NavigateTo(ViewModelBase viewModel)
    {
        CurrentViewModel = viewModel;
    }

    public void NavigateToRoomLobby(int roomId, string roomName, bool isAdmin)
    {
        var lobbyViewModel = new RoomLobbyViewModel(roomId, roomName, isAdmin, _networkService, this, _audioManager, _logger, _profileManager);
        CurrentViewModel = lobbyViewModel;
    }
}
