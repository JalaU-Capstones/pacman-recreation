using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
using PacmanGame.Shared;
using ReactiveUI;
using System.Reactive.Linq;
using System.Collections.Generic;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace PacmanGame.ViewModels;

public class RoomLobbyViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly NetworkService _networkService;
    private readonly IAudioManager _audioManager;
    private readonly ILogger<RoomLobbyViewModel> _logger;
    private readonly IProfileManager _profileManager;
    private readonly IServiceProvider _serviceProvider;

    private readonly int _roomId;
    private readonly int _myPlayerId;
    private PlayerRole _myRole;

    public string RoomName { get; }
    public string RoomVisibility { get; }
    public bool IsAdmin { get; }

    private bool _canStartGame;
    public bool CanStartGame { get => _canStartGame; set => this.RaiseAndSetIfChanged(ref _canStartGame, value); }

    private string _statusText = "Waiting for admin to start...";
    public string StatusText { get => _statusText; set => this.RaiseAndSetIfChanged(ref _statusText, value); }

    private int _spectatorCount;
    public int SpectatorCount { get => _spectatorCount; set => this.RaiseAndSetIfChanged(ref _spectatorCount, value); }

    public ObservableCollection<PlayerViewModel> Players { get; } = new();
    public ReadOnlyObservableCollection<PlayerRole> Roles { get; }

    public ICommand StartGameCommand { get; }
    public ICommand LeaveRoomCommand { get; }
    public ICommand KickPlayerCommand { get; }

    public RoomLobbyViewModel(
        int roomId,
        string roomName,
        RoomVisibility visibility,
        List<PlayerState> initialPlayers,
        MainWindowViewModel mainWindowViewModel,
        NetworkService networkService,
        IAudioManager audioManager,
        ILogger<RoomLobbyViewModel> logger,
        IProfileManager profileManager,
        IServiceProvider serviceProvider)
    {
        _roomId = roomId;
        _mainWindowViewModel = mainWindowViewModel;
        _networkService = networkService;
        _audioManager = audioManager;
        _logger = logger;
        _profileManager = profileManager;
        _serviceProvider = serviceProvider;

        RoomName = roomName;
        RoomVisibility = visibility.ToString().ToUpper();

        var myProfile = _profileManager.GetActiveProfile();
        _myPlayerId = initialPlayers.FirstOrDefault(p => p.Name == myProfile?.Name)?.PlayerId ?? -1;
        IsAdmin = initialPlayers.FirstOrDefault(p => p.PlayerId == _myPlayerId)?.IsAdmin ?? false;

        var roles = new ObservableCollection<PlayerRole>
        {
            PlayerRole.None, PlayerRole.Pacman, PlayerRole.Blinky,
            PlayerRole.Pinky, PlayerRole.Inky, PlayerRole.Clyde
        };
        Roles = new ReadOnlyObservableCollection<PlayerRole>(roles);

        UpdatePlayers(initialPlayers);

        _networkService.OnRoomStateUpdate += UpdatePlayers;
        _networkService.OnKicked += HandleKicked;
        _networkService.OnGameStart += HandleGameStart;
        _networkService.OnLeftRoom += HandleLeftRoom;

        LeaveRoomCommand = ReactiveCommand.Create(LeaveRoom);
        StartGameCommand = ReactiveCommand.Create(StartGame, this.WhenAnyValue(x => x.CanStartGame));
        KickPlayerCommand = ReactiveCommand.Create<PlayerViewModel>(KickPlayer);

        _logger.LogInformation("Entered lobby for room '{RoomName}' (ID: {RoomId}). Admin: {IsAdmin}", RoomName, _roomId, IsAdmin);
    }

    private void UpdatePlayers(List<PlayerState> playerStates)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var myPlayerState = playerStates.FirstOrDefault(p => p.PlayerId == _myPlayerId);
            if (myPlayerState != null) _myRole = myPlayerState.Role;

            var playersToRemove = Players.Where(p => !playerStates.Any(s => s.PlayerId == p.PlayerId)).ToList();
            foreach (var player in playersToRemove) Players.Remove(player);

            foreach (var state in playerStates)
            {
                var existingPlayer = Players.FirstOrDefault(p => p.PlayerId == state.PlayerId);
                if (existingPlayer != null)
                {
                    existingPlayer.Role = state.Role;
                    existingPlayer.IsAdmin = state.IsAdmin;
                }
                else
                {
                    var newPlayer = new PlayerViewModel
                    {
                        PlayerId = state.PlayerId,
                        Name = state.Name,
                        Role = state.Role,
                        IsAdmin = state.IsAdmin,
                        IsYou = state.PlayerId == _myPlayerId
                    };

                    if (IsAdmin && !newPlayer.IsYou)
                    {
                        newPlayer.WhenAnyValue(x => x.Role).Skip(1).Subscribe(newRole => AssignRole(newPlayer.PlayerId, newRole));
                    }
                    Players.Add(newPlayer);
                }
            }

            var playersWithRoles = Players.Count(p => p.Role != PlayerRole.None);
            SpectatorCount = Players.Count - playersWithRoles;

            _logger.LogInformation("Room state updated: {PlayersWithRoles} players with roles, {SpectatorCount} spectators.", playersWithRoles, SpectatorCount);

            CanStartGame = IsAdmin && playersWithRoles > 0;
        });
    }

    private void AssignRole(int playerId, PlayerRole role)
    {
        if (role != PlayerRole.None && Players.Any(p => p.PlayerId != playerId && p.Role == role))
        {
            _logger.LogWarning("Role '{Role}' is already taken. Cannot assign to player {PlayerId}.", role, playerId);
            var player = Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player != null) player.Role = PlayerRole.None;
            return;
        }

        _logger.LogInformation("Admin assigning role '{Role}' to player {PlayerId}.", role, playerId);
        _networkService.SendAssignRoleRequest(playerId, role);
    }

    private void StartGame()
    {
        _logger.LogInformation("Admin is starting the game...");
        _networkService.SendStartGameRequest();
    }

    private void LeaveRoom()
    {
        _logger.LogInformation("Leaving room...");
        _networkService.SendLeaveRoomRequest();
    }

    private void KickPlayer(PlayerViewModel player)
    {
        if (!IsAdmin || player == null) return;
        _logger.LogInformation("Admin kicking player {Name} (ID: {PlayerId})", player.Name, player.PlayerId);
        _networkService.SendKickPlayerRequest(player.PlayerId);
    }

    private void HandleKicked(string reason)
    {
        _logger.LogWarning("Kicked from room. Reason: {Reason}", reason);
        NavigateToMultiplayerMenu();
    }

    private void HandleGameStart(GameStartEvent gameStartEvent)
    {
        _logger.LogInformation("Game start signal received. Preparing assets and navigating to game view...");

        var multiplayerGameViewModel = new MultiplayerGameViewModel(
            _mainWindowViewModel,
            _roomId,
            _myRole,
            _serviceProvider.GetRequiredService<IGameEngine>(),
            _audioManager,
            _serviceProvider.GetRequiredService<ILogger<MultiplayerGameViewModel>>(),
            _networkService
        );

        _mainWindowViewModel.NavigateTo(multiplayerGameViewModel);
    }



    private void HandleLeftRoom()
    {
        _logger.LogInformation("Left room confirmation received or disconnected.");
        NavigateToMultiplayerMenu();
    }

    private void NavigateToMultiplayerMenu()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _mainWindowViewModel.NavigateTo<MultiplayerMenuViewModel>();
        });
    }

    ~RoomLobbyViewModel()
    {
        _logger.LogDebug("Disposing RoomLobbyViewModel and unsubscribing from network events.");
        _networkService.OnRoomStateUpdate -= UpdatePlayers;
        _networkService.OnKicked -= HandleKicked;
        _networkService.OnGameStart -= HandleGameStart;
        _networkService.OnLeftRoom -= HandleLeftRoom;
    }
}
