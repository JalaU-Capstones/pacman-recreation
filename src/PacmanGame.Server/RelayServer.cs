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
    private readonly ICollisionDetector _collisionDetector;
    private CancellationTokenSource? _cancellationTokenSource;

    public RelayServer(ILogger<RelayServer> logger, ILoggerFactory loggerFactory)
    {
        _netManager = new NetManager(this) { DisconnectTimeout = 10000 };
        _roomManager = new RoomManager();
        _logger = logger;
        _loggerFactory = loggerFactory;
        _serializerOptions = MessagePack.MessagePackSerializer.DefaultOptions.WithResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        _mapLoader = new MapLoader(_loggerFactory.CreateLogger<MapLoader>());
        _collisionDetector = new CollisionDetector();
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
                foreach (var room in _roomManager.GetPublicRooms().Where(r => r.State == RoomState.Playing))
                {
                    if (room.Game != null && !room.IsPaused)
                    {
                        room.Game.Update(1f / 20f); // 20 FPS
                        var state = room.Game.GetState();
                        BroadcastToRoom(room, state);
                    }
                }
                Thread.Sleep(1000 / 20); // 20 FPS
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
        var room = player.CurrentRoom;
        if (room?.Game != null)
        {
            room.Game.SetPlayerInput(player.Role, input.Direction);
        }
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
            response.Message = "Name already taken. Join as Spectator or return to list?";
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
            response.Success = true;
            response.Message = $"Joined room '{room.Name}' successfully.";
            response.RoomId = room.Id;
            response.RoomName = room.Name;
            response.Visibility = room.Visibility;
            response.Players = room.GetPlayerStates();

            _logger.LogInformation($"Player {player.Id} ({player.Name}) joined room '{room.Name}'");
            BroadcastRoomState(room);
        }
        SendMessageToPlayer(player, response);
    }

    private void HandleLeaveRoomRequest(Player player)
    {
        var room = player.CurrentRoom;
        if (room != null)
        {
            room.RemovePlayer(player);
            player.CurrentRoom = null;
            player.IsAdmin = false;
            _logger.LogInformation($"Player {player.Id} ({player.Name}) has been fully disconnected from room '{room.Name}'.");

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
            room.Game = new GameSimulation(_mapLoader, _collisionDetector, _loggerFactory.CreateLogger<GameSimulation>());

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
