using System.Windows.Input;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
using ReactiveUI;

namespace PacmanGame.ViewModels;

public class MultiplayerMenuViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly NetworkService _networkService;
    private readonly IAudioManager _audioManager;
    private readonly ILogger _logger;
    private readonly IProfileManager _profileManager;

    public ICommand CreateRoomCommand { get; }
    public ICommand JoinRoomCommand { get; }
    public ICommand BackCommand { get; }

    public MultiplayerMenuViewModel(MainWindowViewModel mainWindowViewModel, NetworkService networkService, IAudioManager audioManager, ILogger logger, IProfileManager profileManager)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _networkService = networkService;
        _audioManager = audioManager;
        _logger = logger;
        _profileManager = profileManager;

        _networkService.Start();

        CreateRoomCommand = ReactiveCommand.Create(CreateRoom);
        JoinRoomCommand = ReactiveCommand.Create(JoinRoom);
        BackCommand = ReactiveCommand.Create(Back);
    }

    private void CreateRoom()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _mainWindowViewModel.NavigateTo(new CreateRoomViewModel(_mainWindowViewModel, _audioManager, _logger, _profileManager));
    }

    private void JoinRoom()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _mainWindowViewModel.NavigateTo(new RoomListViewModel(_mainWindowViewModel, _networkService, _audioManager, _logger, _profileManager));
    }

    private void Back()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _networkService.Stop();
        _mainWindowViewModel.NavigateTo(new MainMenuViewModel(_mainWindowViewModel, _profileManager, _audioManager, _logger));
    }
}
