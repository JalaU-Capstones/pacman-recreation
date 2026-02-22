using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using PacmanGame.Server.Models;
using PacmanGame.Shared;
using MessagePack;
using System.Collections.Concurrent;
using PacmanGame.Server.Services;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;
using System;

namespace PacmanGame.Server;

public class RelayServer : INetEventListener
{
    private readonly NetManager _netManager;
    private readonly RoomManager _roomManager;
    private readonly ConcurrentDictionary<NetPeer, Player> _connectedPlayers = new();
    private readonly ILogger<RelayServer> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly MessagePackSerializerOptions _serializerOptions;
    private readonly IMapLoader _mapLoader;
    private readonly LeaderboardService _leaderboardService;
    private CancellationTokenSource? _cancellationTokenSource;

    // Mapping to track player sessions and roles
    private class PlayerSession
    {
        public int PlayerId { get; set; }
        public int RoomId { get; set; }
        public PlayerRole Role { get; set; }
        public NetPeer Peer { get; set; } = null!;
        public string PlayerName { get; set; } = string.Empty;
    }

    private ConcurrentDictionary<int, PlayerSession> _playerSessions = new();

    private static readonly PlayerRole[] PlayableRolesInOrder =
    {
        PlayerRole.Pacman,
        PlayerRole.Blinky,
        PlayerRole.Pinky,
        PlayerRole.Inky,
        PlayerRole.Clyde
    };

    private static readonly HashSet<PlayerRole> PlayableRoleSet = new()
    {
        PlayerRole.Pacman,
        PlayerRole.Blinky,
        PlayerRole.Pinky,
        PlayerRole.Inky,
        PlayerRole.Clyde
    };

    public RelayServer(ILogger<RelayServer> logger, ILoggerFactory loggerFactory)
    {
        _netManager = new NetManager(this) { DisconnectTimeout = 30000 }; // Increased timeout
        _roomManager = new RoomManager();
        _logger = logger;
        _loggerFactory = loggerFactory;
        // Use StandardResolver to support [MessagePackObject] and [Union] attributes
        _serializerOptions = MessagePack.MessagePackSerializer.DefaultOptions.WithResolver(MessagePack.Resolvers.StandardResolver.Instance);
        _mapLoader = new MapLoader(_loggerFactory.CreateLogger<MapLoader>());
        _leaderboardService = new LeaderboardService(_loggerFactory.CreateLogger<LeaderboardService>());
    }

    public Task StartAsync()
    {
        if (_netManager.Start(IPAddress.Any, IPAddress.IPv6Any, 9050))
        {
            _logger.LogInformation($"Server listening on all interfaces, port 9050");
        }
        else
        {
            _logger.LogError("Failed to start server. Is the port already in use?");
            return Task.CompletedTask;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        return Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                _netManager.PollEvents();

                // Update game simulations for ALL rooms (including private ones)
                foreach (var room in _roomManager.GetAllRooms().Where(r => r.State == RoomState.Playing))
                {
                    if (room.Game != null && !room.IsPaused && !room.IsFrozen)
                    {
                        room.Game.Update(1f / 30f); // 30 FPS
                        var state = room.Game.GetState();
                        BroadcastToRoom(room, state);
                    }
                    else if (room.Game != null && !room.IsPaused && room.IsFrozen)
                    {
                        // During READY freeze: keep broadcasting the authoritative state so all clients converge
                        // even if they missed the reset packet.
                        var state = room.Game.GetState();
                        BroadcastToRoom(room, state);
                    }
                }
                Thread.Sleep(1000 / 30); // 30 FPS
            }
        }, token);
    }

    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
        _netManager.Stop();
        _logger.LogInformation("Server stopped.");
    }

    public void OnPeerConnected(NetPeer peer)
    {
        _logger.LogInformation($"Peer connected: {peer.Address}");
        var player = new Player(peer);
        _connectedPlayers.TryAdd(peer, player);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _logger.LogInformation($"Peer disconnected: {peer.Address}, reason: {disconnectInfo.Reason}");
        if (_connectedPlayers.TryRemove(peer, out var player))
        {
            HandleLeaveRoomRequest(player);
            // Remove session
            _playerSessions.TryRemove(player.Id, out _);
        }
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) => _logger.LogError($"Network error: {socketError} from {endPoint}");

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        if (!_connectedPlayers.TryGetValue(peer, out var player))
        {
            _logger.LogWarning($"Received message from unknown peer: {peer.Address}");
            return;
        }

        var bytes = reader.GetRemainingBytes();
        try
        {
            var baseMessage = MessagePackSerializer.Deserialize<NetworkMessageBase>(bytes, _serializerOptions);
            HandleMessage(player, baseMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deserializing message from {peer.Address}: {ex.Message}");
        }
    }

    private void HandleMessage(Player player, NetworkMessageBase message)
    {
        switch (message)
        {
            case CreateRoomRequest req: HandleCreateRoomRequest(player, req); break;
            case JoinRoomRequest req: HandleJoinRoomRequest(player, req); break;
            case LeaveRoomRequest _: HandleLeaveRoomRequest(player); break;
            case AssignRoleRequest req: HandleAssignRoleRequest(player, req); break;
            case KickPlayerRequest req: HandleKickPlayerRequest(player, req); break;
            case StartGameRequest _: HandleStartGameRequest(player); break;
            case RestartGameRequest _: HandleRestartGameRequest(player); break;
            case GetRoomListRequest _: HandleGetRoomListRequest(player); break;
            case PlayerInputMessage input: HandlePlayerInput(player, input); break;
            case PauseGameRequest req: HandlePauseGameRequest(player, req); break;
            case LeaderboardGetTop10Request req: HandleLeaderboardGetTop10Request(player, req); break;
            case LeaderboardSubmitScoreRequest req: HandleLeaderboardSubmitScoreRequest(player, req); break;
            default: _logger.LogWarning($"Unknown message type: {message.Type} from {player.Peer.Address}"); break;
        }
    }

    private async void HandleLeaderboardGetTop10Request(Player player, LeaderboardGetTop10Request request)
    {
        var top10 = await _leaderboardService.GetTop10Async();
        var response = new LeaderboardGetTop10Response
        {
            Top10 = top10,
            ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        SendMessageToPlayer(player, response);
    }

    private async void HandleLeaderboardSubmitScoreRequest(Player player, LeaderboardSubmitScoreRequest request)
    {
        var result = await _leaderboardService.SubmitScoreAsync(
            request.ProfileId,
            request.ProfileName,
            request.HighScore,
            request.ClientTimestamp);

        var response = new LeaderboardSubmitScoreResponse
        {
            Success = result.Success,
            Message = result.Message,
            NewRank = result.NewRank,
            ReplacedEntry = result.ReplacedEntry
        };
        SendMessageToPlayer(player, response);
    }

    private void HandlePauseGameRequest(Player player, PauseGameRequest request)
    {
        var room = player.CurrentRoom;
        if (room != null && player.IsAdmin)
        {
            room.IsPaused = !room.IsPaused;
            BroadcastToRoom(room, new GamePausedEvent { IsPaused = room.IsPaused });
        }
    }

    private void HandlePlayerInput(Player player, PlayerInputMessage input)
    {
        // Get player session to find their role
        if (!_playerSessions.TryGetValue(input.PlayerId, out var session))
        {
            _logger.LogWarning($"[SERVER] Received input from unknown player {input.PlayerId}");
            return;
        }

        if (session.Role == PlayerRole.None || session.Role == PlayerRole.Spectator)
        {
            _logger.LogWarning($"[SERVER] Player {input.PlayerId} has no controllable role");
            return;
        }

        // Get the room
        var room = player.CurrentRoom;
        if (room == null || room.Id != session.RoomId)
        {
             _logger.LogWarning($"[SERVER] Player {input.PlayerId} room mismatch");
             return;
        }

        if (room.IsFrozen)
        {
            // Ignore inputs during the short "READY!" resync window.
            return;
        }

        // Get the room's game simulation
        if (room.Game == null)
        {
            _logger.LogWarning($"[SERVER] No simulation found for room {session.RoomId}");
            return;
        }

        // Forward input to simulation with the player's ROLE
        room.Game.SetPlayerInput(session.Role, input.Direction);

        _logger.LogDebug($"[SERVER] Player {input.PlayerId} ({session.Role}) input: {input.Direction}");
    }

    private void HandleCreateRoomRequest(Player player, CreateRoomRequest request)
    {
        var response = new CreateRoomResponse();
        if (player.CurrentRoom != null)
        {
            response.Success = false;
            response.Message = "Already in a room.";
        }
        else
        {
            player.Name = request.PlayerName;
            player.IsAdmin = true;
            player.Role = PlayerRole.Pacman;
            var room = _roomManager.CreateRoom(request.RoomName, request.Password, request.Visibility);
            if (room != null)
            {
                room.AddPlayer(player);
                player.CurrentRoom = room;

                // Create session
                _playerSessions[player.Id] = new PlayerSession
                {
                    PlayerId = player.Id,
                    RoomId = room.Id,
                    Role = player.Role,
                    Peer = player.Peer,
                    PlayerName = player.Name
                };

                response.Success = true;
                response.Message = $"Room '{room.Name}' created successfully.";
                response.RoomId = room.Id;
                response.RoomName = room.Name;
                response.Visibility = room.Visibility;
                response.Players = room.GetPlayerStates();

                _logger.LogInformation($"✓ Player {player.Id} ({player.Name}) created room '{room.Name}' as Pacman.");
            }
            else
            {
                response.Success = false;
                response.Message = $"Room '{request.RoomName}' already exists.";
            }
        }
        SendMessageToPlayer(player, response);
    }

    private void HandleJoinRoomRequest(Player player, JoinRoomRequest request)
    {
        var response = new JoinRoomResponse();
        var room = _roomManager.GetRoom(request.RoomName);

        if (player.CurrentRoom != null)
        {
            response.Success = false;
            response.Message = "Already in a room.";
            response.FailureReason = JoinRoomFailureReason.AlreadyInRoom;
        }
        else if (room == null)
        {
            response.Success = false;
            response.Message = "Room not found.";
            response.FailureReason = JoinRoomFailureReason.RoomNotFound;
        }
        else if (room.Visibility == RoomVisibility.Private && room.Password != request.Password)
        {
            response.Success = false;
            response.Message = "Incorrect password.";
            response.FailureReason = JoinRoomFailureReason.IncorrectPassword;
        }
        else if (room.Players.Any(p => p.Role != PlayerRole.Spectator && p.Name.Equals(request.PlayerName, StringComparison.OrdinalIgnoreCase)))
        {
            response.Success = false;
            response.Message = "Username already in use by a player.";
            response.FailureReason = JoinRoomFailureReason.DuplicateUsername;
            response.CanJoinAsSpectator = room.Players.Count(p => p.Role == PlayerRole.Spectator) < 5;
        }
        else
        {
            int playerCount = room.Players.Count(p => p.Role != PlayerRole.Spectator && p.Role != PlayerRole.None);
            int spectatorCount = room.Players.Count(p => p.Role == PlayerRole.Spectator);
            bool playablesFull = playerCount >= 5;
            bool spectatorsFull = spectatorCount >= 5;

            if (playablesFull && !request.JoinAsSpectator)
            {
                if (!spectatorsFull)
                {
                    response.Success = false;
                    response.Message = "All player slots are full. Would you like to join as a spectator?";
                    response.CanJoinAsSpectator = true;
                    response.FailureReason = JoinRoomFailureReason.RoomFull;
                }
                else
                {
                    response.Success = false;
                    response.Message = "Room is completely full (Players & Spectators).";
                    response.FailureReason = JoinRoomFailureReason.RoomFull;
                }
            }
            else if (!room.AddPlayer(player))
            {
                response.Success = false;
                response.Message = "Room is full.";
                response.FailureReason = JoinRoomFailureReason.RoomFull;
            }
            else
            {
                player.Name = request.PlayerName;
                player.CurrentRoom = room;
                player.Role = PlayerRole.None;

                if (request.JoinAsSpectator || playablesFull)
                {
                    player.Role = PlayerRole.Spectator;
                    _logger.LogInformation($"[INFO] Player {player.Name} joined as Spectator.");
                }
                else if (room.State == RoomState.Playing)
                {
                    var assignedRoles = room.Players.Select(p => p.Role).ToList();
                    var availableRoles = new List<PlayerRole> { PlayerRole.Pacman, PlayerRole.Blinky, PlayerRole.Pinky, PlayerRole.Inky, PlayerRole.Clyde }
                        .Except(assignedRoles)
                        .ToList();

                    if (availableRoles.Any())
                    {
                        player.Role = availableRoles.First();
                        _logger.LogInformation($"[INFO] Player {player.Name} joined mid-game. Assigning role {player.Role}.");
                        room.Game?.AddPlayerRole(player.Role);
                        var newPlayerEvent = new NewPlayerJoinedEvent { PlayerId = player.Id, PlayerName = player.Name, Role = player.Role };
                        BroadcastToRoom(room, newPlayerEvent);
                    }
                    else
                    {
                        player.Role = PlayerRole.Spectator;
                        _logger.LogInformation($"[INFO] Player {player.Name} joined mid-game as Spectator (No roles free).");
                    }
                }

                _playerSessions[player.Id] = new PlayerSession { PlayerId = player.Id, RoomId = room.Id, Role = player.Role, Peer = player.Peer, PlayerName = player.Name };
                _logger.LogInformation($"[SERVER] Player {player.Id} session created for room {room.Id}");

                response.Success = true;
                response.Message = $"Joined room '{room.Name}' successfully.";
                response.RoomId = room.Id;
                response.RoomName = room.Name;
                response.Visibility = room.Visibility;
                response.Players = room.GetPlayerStates();
                response.IsGameStarted = room.State == RoomState.Playing;

                _logger.LogInformation($"Player {player.Id} ({player.Name}) joined room '{room.Name}'");
                BroadcastRoomState(room);

                if (room.State == RoomState.Playing)
                {
                    var gameStartEvent = new GameStartEvent { PlayerStates = room.GetPlayerStates(), MapName = "level1.txt" };
                    SendMessageToPlayer(player, gameStartEvent);
                }
            }
        }
        SendMessageToPlayer(player, response);
    }

    private void HandleLeaveRoomRequest(Player player)
    {
        var room = player.CurrentRoom;
        if (room != null)
        {
            var role = player.Role;
            var playerName = player.Name;
            var wasAdmin = player.IsAdmin;

            room.RemovePlayer(player);
            _logger.LogWarning($"[WARNING] Player {player.Name} left or lost connection. Removing entity {role}.");

            player.CurrentRoom = null;
            player.IsAdmin = false;

            _playerSessions.TryRemove(player.Id, out _);

            SendMessageToPlayer(player, new LeaveRoomConfirmation());

            if (room.Players.Count == 0)
            {
                _roomManager.RemoveRoom(room.Name);
                _logger.LogInformation($"Room '{room.Name}' is empty and has been removed.");
            }
            else
            {
                if (wasAdmin)
                {
                    var newAdmin = room.Players.FirstOrDefault();
                    if (newAdmin != null)
                    {
                        newAdmin.IsAdmin = true;
                        _logger.LogInformation($"Admin left. New admin is Player {newAdmin.Id} ({newAdmin.Name}).");

                        // If the admin leaves, do not implicitly change roles here.
                    }
                }

                // Ensure Pac-Man always exists (critical for a playable match).
                EnsurePacmanAssigned(room, playerName);

                // If the departing role was a ghost role and is now missing, fill it from spectators.
                if (role != PlayerRole.Spectator && role != PlayerRole.None && role != PlayerRole.Pacman &&
                    !room.Players.Any(p => p.Role == role))
                {
                    PromoteNextSpectator(room, role, playerName);
                }

                if (room.State == RoomState.Playing && room.Game != null)
                {
                    room.Game.UpdateAssignedRoles(room.Players.Select(p => p.Role).ToList());
                }

                BroadcastRoomState(room);

                // If the match is running, broadcast an authoritative snapshot so clients can remove entities
                // immediately (especially important if the game is paused/frozen and regular state ticks stop).
                if (room.State == RoomState.Playing && room.Game != null)
                {
                    BroadcastToRoom(room, room.Game.GetState());
                }
            }
        }
    }

    private void EnsurePacmanAssigned(Room room, string previousPlayerName)
    {
        var ordered = room.Players.OrderBy(p => p.Id).ToList();
        if (ordered.Count == 0)
        {
            return;
        }

        var pacmanPlayers = ordered.Where(p => p.Role == PlayerRole.Pacman).ToList();
        if (pacmanPlayers.Count > 1)
        {
            // Keep the earliest, demote others.
            foreach (var extra in pacmanPlayers.Skip(1))
            {
                extra.Role = PlayerRole.Spectator;
                if (_playerSessions.TryGetValue(extra.Id, out var session))
                {
                    session.Role = extra.Role;
                }
            }
        }

        if (ordered.Any(p => p.Role == PlayerRole.Pacman))
        {
            return;
        }

        // Promote a spectator first, otherwise reassign the first remaining player.
        var candidate = ordered.FirstOrDefault(p => p.Role == PlayerRole.Spectator) ?? ordered[0];
        var oldRole = candidate.Role;
        candidate.Role = PlayerRole.Pacman;
        if (_playerSessions.TryGetValue(candidate.Id, out var candidateSession))
        {
            candidateSession.Role = PlayerRole.Pacman;
        }

        _logger.LogWarning("Pac-Man role was missing; reassigned Pac-Man to {PlayerName} (Room {RoomId}).", candidate.Name, room.Id);

        // If we promoted a spectator, notify them (optional countdown).
        if (oldRole == PlayerRole.Spectator)
        {
            SendMessageToPlayer(candidate, new SpectatorPromotionEvent
            {
                PreviousPlayerName = previousPlayerName,
                NewRole = PlayerRole.Pacman,
                PreparationTimeSeconds = 3
            });
        }
        else if (oldRole != PlayerRole.Pacman && oldRole != PlayerRole.None && oldRole != PlayerRole.Spectator)
        {
            // Candidate previously held a ghost role; that role must not remain "ownerless".
            // Promote a spectator to fill it, otherwise remove the entity from the simulation.
            PromoteNextSpectator(room, oldRole, candidate.Name);
        }
    }

    private void NormalizeRolesForStart(Room room)
    {
        var players = room.Players.OrderBy(p => p.Id).ToList();
        if (players.Count == 0) return;

        // Ensure a single Pac-Man.
        var pacman = players.FirstOrDefault(p => p.Role == PlayerRole.Pacman);
        if (pacman == null)
        {
            pacman = players.FirstOrDefault(p => p.IsAdmin) ?? players[0];
            pacman.Role = PlayerRole.Pacman;
        }

        foreach (var extraPacman in players.Where(p => p.Id != pacman.Id && p.Role == PlayerRole.Pacman))
        {
            extraPacman.Role = PlayerRole.None;
        }

        // Ensure each ghost role is unique; duplicates become unassigned.
        foreach (var ghostRole in new[] { PlayerRole.Blinky, PlayerRole.Pinky, PlayerRole.Inky, PlayerRole.Clyde })
        {
            var holders = players.Where(p => p.Role == ghostRole).OrderBy(p => p.Id).ToList();
            foreach (var extra in holders.Skip(1))
            {
                extra.Role = PlayerRole.None;
            }
        }

        var taken = players.Where(p => PlayableRoleSet.Contains(p.Role)).Select(p => p.Role).ToHashSet();
        var availableGhosts = new Queue<PlayerRole>(new[] { PlayerRole.Blinky, PlayerRole.Pinky, PlayerRole.Inky, PlayerRole.Clyde }
            .Where(r => !taken.Contains(r)));

        foreach (var p in players.Where(p => p.Role == PlayerRole.None).OrderBy(p => p.Id))
        {
            if (availableGhosts.Count > 0)
            {
                p.Role = availableGhosts.Dequeue();
            }
            else
            {
                p.Role = PlayerRole.Spectator;
            }
        }

        // Update sessions for all players.
        foreach (var p in players)
        {
            if (_playerSessions.TryGetValue(p.Id, out var session))
            {
                session.Role = p.Role;
            }
        }
    }

    private void AssignRolesForRestart(Room room)
    {
        var players = room.Players.OrderBy(p => p.Id).ToList();
        if (players.Count == 0) return;

        room.RoleRotationOffset = (room.RoleRotationOffset + 1) % players.Count;
        var startIndex = room.RoleRotationOffset;

        for (var i = 0; i < players.Count; i++)
        {
            var p = players[(startIndex + i) % players.Count];
            p.Role = i < PlayableRolesInOrder.Length ? PlayableRolesInOrder[i] : PlayerRole.Spectator;

            if (_playerSessions.TryGetValue(p.Id, out var session))
            {
                session.Role = p.Role;
            }
        }
    }

    private void PromoteNextSpectator(Room room, PlayerRole roleToFill, string previousPlayerName)
    {
        var spectators = room.Players.Where(p => p.Role == PlayerRole.Spectator).ToList();
        foreach (var spectator in spectators)
        {
            if (room.Players.Any(p => p.Role != PlayerRole.Spectator && p.Name.Equals(spectator.Name, StringComparison.OrdinalIgnoreCase)))
            {
                SendMessageToPlayer(spectator, new SpectatorPromotionFailedEvent { Reason = "You cannot take this player spot because your username matches an existing player's. Skipping to next spectator." });
                continue;
            }

            spectator.Role = roleToFill;
            if (_playerSessions.TryGetValue(spectator.Id, out var session))
            {
                session.Role = roleToFill;
            }
            _logger.LogInformation($"Promoted spectator {spectator.Name} to {roleToFill}.");
            var promotionEvent = new SpectatorPromotionEvent { PreviousPlayerName = previousPlayerName, NewRole = roleToFill, PreparationTimeSeconds = 5 };
            SendMessageToPlayer(spectator, promotionEvent);
            return;
        }

        if (room.State == RoomState.Playing && room.Game != null)
        {
            room.Game.RemovePlayerRole(roleToFill);
        }
    }

    private void HandleAssignRoleRequest(Player player, AssignRoleRequest request)
    {
        var room = player.CurrentRoom;
        if (room != null && player.IsAdmin)
        {
            var targetPlayer = room.Players.FirstOrDefault(p => p.Id == request.PlayerId);
            if (targetPlayer != null)
            {
                if (request.Role != PlayerRole.Spectator && request.Role != PlayerRole.None && room.Players.Any(p => p.Id != targetPlayer.Id && p.Role == request.Role))
                {
                    _logger.LogWarning($"Admin {player.Id} failed to assign role {request.Role} to player {request.PlayerId}: role already taken.");
                    // Here you could send a message back to the admin to inform them of the failure.
                    return;
                }

                targetPlayer.Role = request.Role;

                if (_playerSessions.TryGetValue(targetPlayer.Id, out var session))
                {
                    session.Role = request.Role;
                    _logger.LogInformation($"[SERVER] Player {targetPlayer.Id} assigned role: {request.Role}");
                }

                _logger.LogInformation($"Admin {player.Id} assigned role {request.Role} to player {request.PlayerId}");
                BroadcastRoomState(room);
            }
        }
    }

    private void HandleKickPlayerRequest(Player player, KickPlayerRequest request)
    {
        var room = player.CurrentRoom;
        if (room != null && player.IsAdmin)
        {
            var playerToKick = room.Players.FirstOrDefault(p => p.Id == request.PlayerIdToKick);
            if (playerToKick != null)
            {
                room.RemovePlayer(playerToKick);
                playerToKick.CurrentRoom = null;

                _playerSessions.TryRemove(playerToKick.Id, out _);

                _logger.LogInformation($"Admin {player.Id} kicked player {playerToKick.Id}");
                SendMessageToPlayer(playerToKick, new KickedEvent { Reason = "Kicked by admin." });
                BroadcastRoomState(room);
            }
        }
    }

    private void HandleStartGameRequest(Player player)
    {
        var room = player.CurrentRoom;
        if (room != null && player.IsAdmin)
        {
            _logger.LogInformation($"Admin {player.Id} is starting game in room '{room.Name}'");
            room.State = RoomState.Playing;
            room.Game = new GameSimulation(_mapLoader, _loggerFactory.CreateLogger<GameSimulation>());
            room.IsPaused = false;
            room.FreezeUntilUtcTicks = 0;

            // Ensure roles are valid and Pac-Man exists before starting.
            NormalizeRolesForStart(room);

            room.Game.OnGameEvent += (evt) =>
            {
                BroadcastToRoom(room, evt);
                if (evt.EventType == GameEventType.GameOver || evt.EventType == GameEventType.Victory)
                {
                    room.IsPaused = true;
                }
            };

            room.Game.OnRoundReset += (reset) =>
            {
                _logger.LogInformation("[SERVER] Global reset for room {RoomId}", room.Id);
                room.FreezeUntilUtcTicks = DateTime.UtcNow.AddSeconds(reset.ReadySeconds).Ticks;
                reset.RoomId = room.Id;
                BroadcastToRoom(room, reset);
                // Immediately broadcast authoritative positions/lives after reset.
                BroadcastToRoom(room, room.Game.GetState());
            };

            var assignedRoles = room.Players.Select(p => p.Role).Where(r => r != PlayerRole.None && r != PlayerRole.Spectator).ToList();
            room.Game.Initialize(room.Id, assignedRoles);

            var gameStartEvent = new GameStartEvent { PlayerStates = room.GetPlayerStates(), MapName = "level1.txt" };
            BroadcastToRoom(room, gameStartEvent);
        }
    }

    private void HandleRestartGameRequest(Player player)
    {
        var room = player.CurrentRoom;
        if (room != null && player.IsAdmin)
        {
            _logger.LogInformation($"Admin {player.Id} is restarting game in room '{room.Name}'");
            room.IsPaused = false;
            room.FreezeUntilUtcTicks = 0;

            // Deterministic role rotation: ensures exactly one Pac-Man after restart.
            AssignRolesForRestart(room);

            room.Game = new GameSimulation(_mapLoader, _loggerFactory.CreateLogger<GameSimulation>());
            room.Game.OnGameEvent += (evt) =>
            {
                BroadcastToRoom(room, evt);
                if (evt.EventType == GameEventType.GameOver || evt.EventType == GameEventType.Victory)
                {
                    room.IsPaused = true;
                }
            };

            room.Game.OnRoundReset += (reset) =>
            {
                _logger.LogInformation("[SERVER] Global reset for room {RoomId}", room.Id);
                room.FreezeUntilUtcTicks = DateTime.UtcNow.AddSeconds(reset.ReadySeconds).Ticks;
                reset.RoomId = room.Id;
                BroadcastToRoom(room, reset);
                BroadcastToRoom(room, room.Game.GetState());
            };

            var assignedRoles = room.Players.Select(p => p.Role).Where(r => r != PlayerRole.None && r != PlayerRole.Spectator).ToList();
            room.Game.Initialize(room.Id, assignedRoles);

            BroadcastRoomState(room);
            var gameStartEvent = new GameStartEvent { PlayerStates = room.GetPlayerStates(), MapName = "level1.txt" };
            BroadcastToRoom(room, gameStartEvent);
        }
    }

    private void HandleGetRoomListRequest(Player player)
    {
        _logger.LogInformation($"Received GetRoomListRequest from Player {player.Id} ({player.Peer.Address}).");
        var publicRooms = _roomManager.GetPublicRooms().Select(r => new RoomInfo
        {
            RoomId = r.Id,
            Name = r.Name,
            PlayerCount = r.Players.Count(p => p.Role != PlayerRole.None),
            MaxPlayers = 5
        }).ToList();

        var response = new GetRoomListResponse { Rooms = publicRooms };
        SendMessageToPlayer(player, response);
        _logger.LogInformation($"Sent GetRoomListResponse to Player {player.Id} with {publicRooms.Count} rooms.");
    }

    private void BroadcastRoomState(Room room)
    {
        var state = new RoomStateUpdateMessage { Players = room.GetPlayerStates() };
        BroadcastToRoom(room, state);
    }

    private void BroadcastToRoom(Room room, NetworkMessageBase message)
    {
        try
        {
            var bytes = MessagePackSerializer.Serialize(message, _serializerOptions);
            foreach (var p in room.Players)
            {
                p.Peer.Send(bytes, DeliveryMethod.ReliableOrdered);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error broadcasting message: {ex.Message}");
        }
    }

    private void SendMessageToPlayer(Player player, NetworkMessageBase message)
    {
        try
        {
            var bytes = MessagePackSerializer.Serialize(message, _serializerOptions);
            player.Peer.Send(bytes, DeliveryMethod.ReliableOrdered);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending message to player {player.Id}: {ex.Message}");
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    public void OnConnectionRequest(ConnectionRequest request) => request.AcceptIfKey("PacmanGame");
}
