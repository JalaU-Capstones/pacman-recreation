using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
using PacmanGame.Shared;
using ReactiveUI;
using System.Reactive;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace PacmanGame.ViewModels;

public class RoomListViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly NetworkService _networkService;
    private readonly IAudioManager _audioManager;
    private readonly ILogger<RoomListViewModel> _logger;
    private readonly IProfileManager _profileManager;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<RoomInfo> Rooms { get; } = new();

    private bool _isPrivateJoinFormVisible;
    public bool IsPrivateJoinFormVisible
    {
        get => _isPrivateJoinFormVisible;
        set => this.RaiseAndSetIfChanged(ref _isPrivateJoinFormVisible, value);
    }

    private string _privateRoomName = string.Empty;
    public string PrivateRoomName
    {
        get => _privateRoomName;
        set => this.RaiseAndSetIfChanged(ref _privateRoomName, value);
    }

    private string _privateRoomPassword = string.Empty;
    public string PrivateRoomPassword
    {
        get => _privateRoomPassword;
        set => this.RaiseAndSetIfChanged(ref _privateRoomPassword, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    private bool _showSpectatorPrompt;
    public bool ShowSpectatorPrompt
    {
        get => _showSpectatorPrompt;
        set => this.RaiseAndSetIfChanged(ref _showSpectatorPrompt, value);
    }

    private bool _canJoinAsSpectatorOption;
    public bool CanJoinAsSpectatorOption
    {
        get => _canJoinAsSpectatorOption;
        set => this.RaiseAndSetIfChanged(ref _canJoinAsSpectatorOption, value);
    }

    private RoomInfo? _pendingJoinRoom;

    public ICommand JoinPublicRoomCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ShowPrivateJoinFormCommand { get; }
    public ICommand CancelPrivateJoinCommand { get; }
    public ICommand JoinPrivateRoomCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand JoinAsSpectatorCommand { get; }
    public ICommand CancelSpectatorJoinCommand { get; }

    public RoomListViewModel(MainWindowViewModel mainWindowViewModel, NetworkService networkService, IAudioManager audioManager, ILogger<RoomListViewModel> logger, IProfileManager profileManager, IServiceProvider serviceProvider)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _networkService = networkService;
        _audioManager = audioManager;
        _logger = logger;
        _profileManager = profileManager;
        _serviceProvider = serviceProvider;

        _networkService.OnJoinedRoom += HandleJoinedRoom;
        _networkService.OnJoinRoomFailed += HandleJoinRoomFailed;
        _networkService.OnRoomListReceived += HandleRoomListReceived;

        JoinPublicRoomCommand = ReactiveCommand.Create<RoomInfo>(JoinPublicRoom);
        RefreshCommand = ReactiveCommand.Create(RefreshRooms);
        ShowPrivateJoinFormCommand = ReactiveCommand.Create(() => IsPrivateJoinFormVisible = true);
        CancelPrivateJoinCommand = ReactiveCommand.Create(() => IsPrivateJoinFormVisible = false);
        JoinPrivateRoomCommand = ReactiveCommand.Create(JoinPrivateRoom);
        BackCommand = ReactiveCommand.Create(Back);
        JoinAsSpectatorCommand = ReactiveCommand.Create(JoinAsSpectator);
        CancelSpectatorJoinCommand = ReactiveCommand.Create(CancelSpectatorJoin);

        RefreshRooms();
    }

    private void JoinPublicRoom(RoomInfo? room)
    {
        if (room == null) return;
        _pendingJoinRoom = room;
        _audioManager.PlaySoundEffect("menu-select");
        var request = new JoinRoomRequest
        {
            RoomName = room.Name,
            PlayerName = _profileManager.GetActiveProfile()?.Name ?? "Player"
        };
        _networkService.SendJoinRoomRequest(request);
    }

    private void JoinPrivateRoom()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(PrivateRoomName) || string.IsNullOrWhiteSpace(PrivateRoomPassword))
        {
            ErrorMessage = "Room name and password cannot be empty.";
            return;
        }

        _audioManager.PlaySoundEffect("menu-select");
        var request = new JoinRoomRequest
        {
            RoomName = PrivateRoomName,
            Password = PrivateRoomPassword,
            PlayerName = _profileManager.GetActiveProfile()?.Name ?? "Player"
        };
        _networkService.SendJoinRoomRequest(request);
    }

    private void JoinAsSpectator()
    {
        if (_pendingJoinRoom == null) return;

        _audioManager.PlaySoundEffect("menu-select");
        var request = new JoinRoomRequest
        {
            RoomName = _pendingJoinRoom.Name,
            PlayerName = _profileManager.GetActiveProfile()?.Name ?? "Player",
            JoinAsSpectator = true
        };
        _networkService.SendJoinRoomRequest(request);
        ShowSpectatorPrompt = false;
        ErrorMessage = string.Empty;
    }

    private void CancelSpectatorJoin()
    {
        ShowSpectatorPrompt = false;
        ErrorMessage = string.Empty;
        _pendingJoinRoom = null;
    }

    private void RefreshRooms()
    {
        _logger.LogInformation("Requesting room list from server...");
        _audioManager.PlaySoundEffect("menu-navigate");
        _networkService.SendGetRoomListRequest();
    }

    private void HandleRoomListReceived(List<RoomInfo> rooms)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Rooms.Clear();
            foreach (var room in rooms)
            {
                Rooms.Add(room);
            }
            _logger.LogInformation($"[DEBUG] RoomList UI updated with {Rooms.Count} items.");
        });
    }

    private void HandleJoinedRoom(int roomId, string roomName, RoomVisibility visibility, List<PlayerState> players, bool isGameStarted)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _logger.LogInformation($"Successfully joined room '{roomName}'. GameStarted: {isGameStarted}");

            if (isGameStarted)
            {
                // Navigate directly to game view
                var myProfile = _profileManager.GetActiveProfile();
                var myState = players.FirstOrDefault(p => p.Name == myProfile?.Name);
                var myRole = myState?.Role ?? PlayerRole.Spectator;
                var myPlayerId = myState?.PlayerId ?? -1;
                var isAdmin = myState?.IsAdmin ?? false;

                var multiplayerGameViewModel = new MultiplayerGameViewModel(
                    _mainWindowViewModel,
                    roomId,
                    myRole,
                    isAdmin,
                    _serviceProvider.GetRequiredService<IGameEngine>(),
                    _audioManager,
                    _serviceProvider.GetRequiredService<ILogger<MultiplayerGameViewModel>>(),
                    _networkService,
                    myPlayerId,
                    _serviceProvider
                );

                _mainWindowViewModel.NavigateTo(multiplayerGameViewModel);
            }
            else
            {
                // Navigate to lobby
                _mainWindowViewModel.NavigateToRoomLobby(roomId, roomName, visibility, players);
            }
        });
    }

    private void HandleJoinRoomFailed(string message, JoinRoomFailureReason reason, bool canJoinAsSpectator)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ErrorMessage = message;
            _logger.LogError($"Failed to join room: {message}");

            if (reason == JoinRoomFailureReason.RoomFull || reason == JoinRoomFailureReason.DuplicateUsername)
            {
                CanJoinAsSpectatorOption = canJoinAsSpectator;
                ShowSpectatorPrompt = true;
            }
        });
    }

    private void Back()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _mainWindowViewModel.NavigateTo<MultiplayerMenuViewModel>();
    }

    ~RoomListViewModel()
    {
        _networkService.OnJoinedRoom -= HandleJoinedRoom;
        _networkService.OnJoinRoomFailed -= HandleJoinRoomFailed;
        _networkService.OnRoomListReceived -= HandleRoomListReceived;
    }
}
