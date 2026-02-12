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

namespace PacmanGame.ViewModels;

public class RoomLobbyViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly NetworkService _networkService;
    private readonly IAudioManager _audioManager;
    private readonly ILogger _logger;
    private readonly IProfileManager _profileManager;

    private readonly int _roomId;
    private readonly int _myPlayerId;

    public string RoomName { get; }
    public string RoomVisibility { get; }
    public bool IsAdmin { get; }

    private bool _canStartGame;
    public bool CanStartGame
    {
        get => _canStartGame;
        set => this.RaiseAndSetIfChanged(ref _canStartGame, value);
    }

    private string _statusText = "Waiting for admin to start...";
    public string StatusText
    {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    private int _spectatorCount;
    public int SpectatorCount
    {
        get => _spectatorCount;
        set => this.RaiseAndSetIfChanged(ref _spectatorCount, value);
    }

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
        IAudioManager audioManager,
        ILogger logger,
        IProfileManager profileManager)
    {
        _roomId = roomId;
        _mainWindowViewModel = mainWindowViewModel;
        _networkService = NetworkService.Instance;
        _audioManager = audioManager;
        _logger = logger;
        _profileManager = profileManager;

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

        // Subscribe to network events
        _networkService.OnRoomStateUpdate += UpdatePlayers;
        _networkService.OnKicked += HandleKicked;
        _networkService.OnGameStart += HandleGameStart;
        _networkService.OnLeftRoom += HandleLeftRoom;

        // Command implementations
        LeaveRoomCommand = ReactiveCommand.Create(LeaveRoom);
        StartGameCommand = ReactiveCommand.Create(StartGame, this.WhenAnyValue(x => x.CanStartGame));
        KickPlayerCommand = ReactiveCommand.Create<PlayerViewModel>(KickPlayer);

        _logger.Info($"Entered lobby for room '{RoomName}' (ID: {_roomId}). Admin: {IsAdmin}");
    }

    private void UpdatePlayers(List<PlayerState> playerStates)
    {
        Players.Clear();
        foreach (var state in playerStates)
        {
            var playerVM = new PlayerViewModel
            {
                PlayerId = state.PlayerId,
                Name = state.Name,
                Role = state.Role,
                IsAdmin = state.IsAdmin,
                IsYou = state.PlayerId == _myPlayerId
            };

            // Subscribe to role changes if the current user is an admin
            if (IsAdmin && !playerVM.IsYou)
            {
                playerVM.WhenAnyValue(x => x.Role)
                    .Skip(1) // Skip the initial value set
                    .Subscribe(newRole => AssignRole(playerVM.PlayerId, newRole));
            }

            Players.Add(playerVM);
        }

        // Update spectator count and CanStartGame logic
        SpectatorCount = Players.Count(p => p.Role == PlayerRole.None);
        var assignedRoles = Players.Count(p => p.Role != PlayerRole.None);

        _logger.Info($"Room state updated: {Players.Count} players, {SpectatorCount} spectators.");

        CanStartGame = IsAdmin && assignedRoles > 0;
    }

    private void AssignRole(int playerId, PlayerRole role)
    {
        // Prevent assigning a role that is already taken by another player
        if (role != PlayerRole.None && Players.Any(p => p.PlayerId != playerId && p.Role == role))
        {
            _logger.Warning($"Role '{role}' is already taken. Cannot assign to player {playerId}.");
            // Revert UI change (optional, server will have final say)
            var player = Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player != null) player.Role = PlayerRole.None; // Revert
            return;
        }

        _logger.Info($"Admin assigning role '{role}' to player {playerId}.");
        _networkService.SendAssignRoleRequest(playerId, role);
    }

    private void StartGame()
    {
        _logger.Info("Admin is starting the game...");
        _networkService.SendStartGameRequest();
    }

    private void LeaveRoom()
    {
        _logger.Info("Leaving room...");
        _networkService.SendLeaveRoomRequest();
        NavigateToMultiplayerMenu();
    }

    private void KickPlayer(PlayerViewModel player)
    {
        if (!IsAdmin || player == null) return;
        _logger.Info($"Admin kicking player {player.Name} (ID: {player.PlayerId})");
        _networkService.SendKickPlayerRequest(player.PlayerId);
    }

    private void HandleKicked(string reason)
    {
        _logger.Warning($"Kicked from room. Reason: {reason}");
        // Here you would show a dialog to the user.
        NavigateToMultiplayerMenu();
    }

    private void HandleGameStart()
    {
        _logger.Info("Game start signal received. Loading assets and navigating to game view...");
        _audioManager.StopMusic();
        // Here you would load all necessary assets
        // e.g., _mapLoader.LoadAllMaps(); _spriteManager.LoadAllSprites();
        _mainWindowViewModel.NavigateTo(new MultiplayerGameViewModel(_networkService));
    }

    private void HandleLeftRoom()
    {
        _logger.Info("Left room confirmation received or disconnected.");
        NavigateToMultiplayerMenu();
    }

    private void NavigateToMultiplayerMenu()
    {
        _mainWindowViewModel.NavigateTo(new MultiplayerMenuViewModel(_mainWindowViewModel, _networkService, _audioManager, _logger, _profileManager));
    }

    // Destructor to unsubscribe from events
    ~RoomLobbyViewModel()
    {
        _logger.Debug("Disposing RoomLobbyViewModel and unsubscribing from network events.");
        _networkService.OnRoomStateUpdate -= UpdatePlayers;
        _networkService.OnKicked -= HandleKicked;
        _networkService.OnGameStart -= HandleGameStart;
        _networkService.OnLeftRoom -= HandleLeftRoom;
    }
}
