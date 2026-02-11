using System.Collections.ObjectModel;
using System.Windows.Input;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
using PacmanGame.Shared;
using ReactiveUI;

namespace PacmanGame.ViewModels;

public class RoomListViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly NetworkService _networkService;
    private readonly IAudioManager _audioManager;
    private readonly ILogger _logger;
    private readonly IProfileManager _profileManager;

    public ObservableCollection<RoomViewModel> Rooms { get; } = new();
    public ICommand JoinRoomCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand BackCommand { get; }

    public RoomListViewModel(MainWindowViewModel mainWindowViewModel, NetworkService networkService, IAudioManager audioManager, ILogger logger, IProfileManager profileManager)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _networkService = networkService;
        _audioManager = audioManager;
        _logger = logger;
        _profileManager = profileManager;
        _networkService.OnJoinRoomResponse += OnJoinRoomResponse;

        JoinRoomCommand = ReactiveCommand.Create<RoomViewModel>(JoinRoom);
        RefreshCommand = ReactiveCommand.Create(RefreshRooms);
        BackCommand = ReactiveCommand.Create(Back);

        RefreshRooms();
    }

    private void JoinRoom(RoomViewModel? room)
    {
        if (room == null) return;
        _audioManager.PlaySoundEffect("menu-select");
        var request = new JoinRoomRequest
        {
            RoomName = room.Name
        };
        _networkService.SendJoinRoomRequest(request);
    }

    private void RefreshRooms()
    {
        _audioManager.PlaySoundEffect("menu-navigate");
        Rooms.Clear();
    }

    private void OnJoinRoomResponse(JoinRoomResponse response)
    {
        if (response.Success)
        {
            _mainWindowViewModel.NavigateToRoomLobby(response.RoomId, response.RoomName ?? string.Empty, false);
        }
        else
        {
            _logger.Error("Failed to join room.");
        }
    }

    private void Back()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _mainWindowViewModel.NavigateTo(new MultiplayerMenuViewModel(_mainWindowViewModel, _networkService, _audioManager, _logger, _profileManager));
    }
}

public class RoomViewModel : ViewModelBase
{
    public string Name { get; set; } = string.Empty;
    public string PlayerCount { get; set; } = string.Empty;
    public string SpectatorCount { get; set; } = string.Empty;
}
