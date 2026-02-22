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
    private const string ConnectionFailedMessage = "Connection failed: Please try again later";

    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly NetworkService _networkService;
    private readonly IAudioManager _audioManager;
    private readonly ILogger<RoomLobbyViewModel> _logger;
    private readonly IProfileManager _profileManager;
    private readonly IServiceProvider _serviceProvider;

    private readonly int _roomId;
    private int _myPlayerId;
    private PlayerRole _myRole;
    private bool _isAdmin;
    private bool _leaveRequested;

    public string RoomName { get; }
    public string RoomVisibility { get; }

    public bool IsAdmin
    {
        get => _isAdmin;
        set => this.RaiseAndSetIfChanged(ref _isAdmin, value);
    }

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
    public ICommand DismissConnectionAlertCommand { get; }

    private bool _isConnectionAlertVisible;
    public bool IsConnectionAlertVisible
    {
        get => _isConnectionAlertVisible;
        set => this.RaiseAndSetIfChanged(ref _isConnectionAlertVisible, value);
    }

    private string _connectionAlertMessage = ConnectionFailedMessage;
    public string ConnectionAlertMessage
    {
        get => _connectionAlertMessage;
        set => this.RaiseAndSetIfChanged(ref _connectionAlertMessage, value);
    }

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
        _logger.LogDebug("Active profile: {ProfileName}", myProfile?.Name ?? "null");
        _logger.LogDebug("Initial players count: {Count}", initialPlayers.Count);

        var myPlayerState = initialPlayers.FirstOrDefault(p => p.Name == myProfile?.Name);
        _myPlayerId = myPlayerState?.PlayerId ?? -1;

        if (_myPlayerId == -1)
        {
            _logger.LogWarning("Could not match player by name '{ProfileName}'.", myProfile?.Name);
        }
        else
        {
             _myRole = myPlayerState!.Role;
             IsAdmin = myPlayerState.IsAdmin;
        }

        _logger.LogInformation("My Player ID: {PlayerId}, IsAdmin: {IsAdmin}", _myPlayerId, IsAdmin);

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
        _networkService.OnConnectionFailed += HandleConnectionFailed;

        LeaveRoomCommand = ReactiveCommand.Create(LeaveRoom);
        StartGameCommand = ReactiveCommand.Create(StartGame, this.WhenAnyValue(x => x.CanStartGame));
        KickPlayerCommand = ReactiveCommand.Create<PlayerViewModel>(KickPlayer);
        DismissConnectionAlertCommand = ReactiveCommand.Create(DismissConnectionAlert);

        _logger.LogInformation("Entered lobby for room '{RoomName}' (ID: {RoomId}). Admin: {IsAdmin}", RoomName, _roomId, IsAdmin);
    }

    private void UpdatePlayers(List<PlayerState> playerStates)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var myProfile = _profileManager.GetActiveProfile();
            var myPlayerState = playerStates.FirstOrDefault(p => p.Name == myProfile?.Name);

            if (myPlayerState != null)
            {
                _myPlayerId = myPlayerState.PlayerId;
                _myRole = myPlayerState.Role;
                IsAdmin = myPlayerState.IsAdmin;
            }

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

                    if (IsAdmin)
                    {
                         newPlayer.WhenAnyValue(x => x.Role)
                            .Skip(1)
                            .Subscribe(newRole =>
                            {
                                AssignRole(newPlayer.PlayerId, newRole);
                            });
                    }

                    Players.Add(newPlayer);
                }
            }

            var duplicateRoles = Players
                .Where(p => p.Role != PlayerRole.None && p.Role != PlayerRole.Spectator)
                .GroupBy(p => p.Role)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var player in Players)
            {
                player.HasRoleConflict = duplicateRoles.Contains(player.Role);
            }

            var playersWithRoles = Players.Count(p => p.Role != PlayerRole.None);
            SpectatorCount = Players.Count - playersWithRoles;

            _logger.LogInformation("Room state updated: {PlayersWithRoles} players with roles, {SpectatorCount} spectators.", playersWithRoles, SpectatorCount);

            bool hasPacman = Players.Any(p => p.Role == PlayerRole.Pacman);
            bool hasRoleConflict = Players.Any(p => p.HasRoleConflict);
            CanStartGame = IsAdmin && hasPacman && !hasRoleConflict;
        });
    }

    private void AssignRole(int playerId, PlayerRole role)
    {
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
        _leaveRequested = true;
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

        var myProfile = _profileManager.GetActiveProfile();
        var myState = gameStartEvent.PlayerStates.FirstOrDefault(p => p.Name == myProfile?.Name);
        if (myState != null)
        {
            _myRole = myState.Role;
            _myPlayerId = myState.PlayerId;
            IsAdmin = myState.IsAdmin;
        }

        var multiplayerGameViewModel = new MultiplayerGameViewModel(
            _mainWindowViewModel,
            _roomId,
            _myRole,
            IsAdmin,
            _serviceProvider.GetRequiredService<IGameEngine>(),
            _audioManager,
            _serviceProvider.GetRequiredService<ILogger<MultiplayerGameViewModel>>(),
            _networkService,
            _myPlayerId,
            _serviceProvider
        );

        _mainWindowViewModel.NavigateTo(multiplayerGameViewModel);
    }

    private void HandleLeftRoom()
    {
        _logger.LogInformation("Left room confirmation received or disconnected.");
        if (_leaveRequested)
        {
            NavigateToMultiplayerMenu();
            return;
        }

        // Unexpected disconnect while waiting in lobby.
        ShowConnectionAlert();
    }

    private void HandleConnectionFailed(string technicalReason)
    {
        if (_leaveRequested) return;
        _logger.LogError("Connection issue detected in lobby: {Reason}", technicalReason);
        ShowConnectionAlert();
    }

    private void ShowConnectionAlert()
    {
        ConnectionAlertMessage = ConnectionFailedMessage;
        IsConnectionAlertVisible = true;
    }

    private void DismissConnectionAlert()
    {
        IsConnectionAlertVisible = false;
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
        _networkService.OnConnectionFailed -= HandleConnectionFailed;
    }
}
