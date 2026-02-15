using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using PacmanGame.Server.Models;
using PacmanGame.Shared;
using MessagePack;
using System.Collections.Concurrent;
using PacmanGame.Server.Services;
using Microsoft.Extensions.Logging;

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

    public RelayServer(ILogger<RelayServer> logger, ILoggerFactory loggerFactory)
    {
        _netManager = new NetManager(this) { DisconnectTimeout = 30000 }; // Increased timeout
        _roomManager = new RoomManager();
        _logger = logger;
        _loggerFactory = loggerFactory;
        // Use StandardResolver to support [MessagePackObject] and [Union] attributes
        _serializerOptions = MessagePack.MessagePackSerializer.DefaultOptions.WithResolver(MessagePack.Resolvers.StandardResolver.Instance);
        _mapLoader = new MapLoader(_loggerFactory.CreateLogger<MapLoader>());
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
                    if (room.Game != null && !room.IsPaused)
                    {
                        room.Game.Update(1f / 30f); // 30 FPS
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
            default: _logger.LogWarning($"Unknown message type: {message.Type} from {player.Peer.Address}"); break;
        }
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
        }
        else if (room == null)
        {
            response.Success = false;
            response.Message = "Room not found.";
        }
        else if (room.Visibility == RoomVisibility.Private && room.Password != request.Password)
        {
            response.Success = false;
            response.Message = "Incorrect password.";
        }
        else if (room.Players.Any(p => p.Name.Equals(request.PlayerName, StringComparison.OrdinalIgnoreCase)))
        {
            response.Success = false;
            response.Message = "Name already taken.";
        }
        else
        {
            // Check capacity
            int playerCount = room.Players.Count(p => p.Role != PlayerRole.Spectator && p.Role != PlayerRole.None);
            int spectatorCount = room.Players.Count(p => p.Role == PlayerRole.Spectator);
            bool roomFull = playerCount >= 5;
            bool spectatorsFull = spectatorCount >= 5;

            if (roomFull && !request.JoinAsSpectator)
            {
                if (!spectatorsFull)
                {
                    // Prompt user to join as spectator
                    response.Success = false;
                    response.Message = "Room is full. Join as Spectator?";
                    response.CanJoinAsSpectator = true;
                }
                else
                {
                    response.Success = false;
                    response.Message = "Room is completely full (Players & Spectators).";
                }
            }
            else if (!room.AddPlayer(player))
            {
                response.Success = false;
                response.Message = "Room is full.";
            }
            else
            {
                player.Name = request.PlayerName;
                player.CurrentRoom = room;
                player.Role = PlayerRole.None; // Default

                // Handle mid-game join or explicit spectator request
                if (request.JoinAsSpectator)
                {
                    player.Role = PlayerRole.Spectator;
                    _logger.LogInformation($"[INFO] Player {player.Name} joined as Spectator.");
                }
                else if (room.State == RoomState.Playing)
                {
                    // Try to assign an available role
                    var assignedRoles = room.Players.Select(p => p.Role).ToList();
                    var availableRoles = new List<PlayerRole> { PlayerRole.Pacman, PlayerRole.Blinky, PlayerRole.Pinky, PlayerRole.Inky, PlayerRole.Clyde }
                        .Except(assignedRoles)
                        .ToList();

                    if (availableRoles.Any())
                    {
                        player.Role = availableRoles.First();
                        _logger.LogInformation($"[INFO] Player {player.Name} re-joined mid-game. Assigning role {player.Role}.");

                        // Update simulation with new role
                        room.Game?.UpdateAssignedRoles(room.Players.Select(p => p.Role).ToList());
                    }
                    else
                    {
                        player.Role = PlayerRole.Spectator;
                        _logger.LogInformation($"[INFO] Player {player.Name} joined mid-game as Spectator (Room full).");
                    }
                }

                // Create session
                _playerSessions[player.Id] = new PlayerSession
                {
                    PlayerId = player.Id,
                    RoomId = room.Id,
                    Role = player.Role,
                    Peer = player.Peer,
                    PlayerName = player.Name
                };
                _logger.LogInformation($"[SERVER] Player {player.Id} session created for room {room.Id}");

                response.Success = true;
                response.Message = $"Joined room '{room.Name}' successfully.";
                response.RoomId = room.Id;
                response.RoomName = room.Name;
                response.Visibility = room.Visibility;
                response.Players = room.GetPlayerStates();

                _logger.LogInformation($"Player {player.Id} ({player.Name}) joined room '{room.Name}'");
                BroadcastRoomState(room);

                // If game is running, send game start event to the new player so they load the map
                if (room.State == RoomState.Playing)
                {
                    var gameStartEvent = new GameStartEvent
                    {
                        PlayerStates = room.GetPlayerStates(),
                        MapName = "level1.txt"
                    };
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
            room.RemovePlayer(player);
            _logger.LogWarning($"[WARNING] Player {player.Name} left or lost connection. Removing entity {role}.");

            player.CurrentRoom = null;
            player.IsAdmin = false;

            // Remove session
            _playerSessions.TryRemove(player.Id, out _);

            SendMessageToPlayer(player, new LeaveRoomConfirmation());

            if (room.Players.Count == 0)
            {
                _roomManager.RemoveRoom(room.Name);
                _logger.LogInformation($"Room '{room.Name}' is empty and has been removed.");
            }
            else
            {
                if (!room.Players.Any(p => p.IsAdmin))
                {
                    var newAdmin = room.Players.FirstOrDefault();
                    if (newAdmin != null)
                    {
                        newAdmin.IsAdmin = true;
                        _logger.LogInformation($"Admin left. New admin is Player {newAdmin.Id} ({newAdmin.Name}).");
                    }
                }

                // Promote spectator if available
                if (role != PlayerRole.Spectator && role != PlayerRole.None)
                {
                    var spectator = room.Players.FirstOrDefault(p => p.Role == PlayerRole.Spectator);
                    if (spectator != null)
                    {
                        spectator.Role = role;
                        if (_playerSessions.TryGetValue(spectator.Id, out var session))
                        {
                            session.Role = role;
                        }
                        _logger.LogInformation($"Promoted spectator {spectator.Name} to {role}.");

                        // Notify spectator they have a new role
                        var promotionEvent = new SpectatorPromotionEvent
                        {
                            PreviousPlayerName = playerName,
                            NewRole = role,
                            PreparationTimeSeconds = 5
                        };
                        SendMessageToPlayer(spectator, promotionEvent);
                    }
                    else if (room.State == RoomState.Playing && room.Game != null)
                    {
                        // No spectator to take over, remove entity
                        room.Game.RemovePlayerRole(role);
                    }
                }

                if (room.State == RoomState.Playing && room.Game != null)
                {
                    // Update roles in simulation
                    room.Game.UpdateAssignedRoles(room.Players.Select(p => p.Role).ToList());
                }

                BroadcastRoomState(room);
            }
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
                targetPlayer.Role = request.Role;

                // Update session
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

                // Remove session
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

            // Get all assigned roles
            var assignedRoles = room.Players.Select(p => p.Role).Where(r => r != PlayerRole.None && r != PlayerRole.Spectator).ToList();
            room.Game.Initialize(room.Id, assignedRoles);

            var gameStartEvent = new GameStartEvent
            {
                PlayerStates = room.GetPlayerStates(),
                MapName = "level1.txt"
            };
            BroadcastToRoom(room, gameStartEvent);
        }
    }

    private void HandleRestartGameRequest(Player player)
    {
        var room = player.CurrentRoom;
        if (room != null && player.IsAdmin)
        {
            _logger.LogInformation($"Admin {player.Id} is restarting game in room '{room.Name}'");

            // Shuffle roles among current players (excluding spectators)
            var players = room.Players.Where(p => p.Role != PlayerRole.Spectator && p.Role != PlayerRole.None).ToList();
            var roles = players.Select(p => p.Role).ToList();

            // Simple shuffle
            var rng = new Random();
            int n = roles.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (roles[k], roles[n]) = (roles[n], roles[k]);
            }

            for (int i = 0; i < players.Count; i++)
            {
                players[i].Role = roles[i];
                if (_playerSessions.TryGetValue(players[i].Id, out var session))
                {
                    session.Role = roles[i];
                }
            }

            // Re-initialize game
            room.Game = new GameSimulation(_mapLoader, _loggerFactory.CreateLogger<GameSimulation>());
            var assignedRoles = room.Players.Select(p => p.Role).Where(r => r != PlayerRole.None && r != PlayerRole.Spectator).ToList();
            room.Game.Initialize(room.Id, assignedRoles);

            // Broadcast new state and start event
            BroadcastRoomState(room);

            var gameStartEvent = new GameStartEvent
            {
                PlayerStates = room.GetPlayerStates(),
                MapName = "level1.txt"
            };
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
