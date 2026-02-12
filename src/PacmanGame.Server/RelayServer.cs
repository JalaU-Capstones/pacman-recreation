using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using PacmanGame.Server.Models;
using PacmanGame.Shared;
using MessagePack;
using System.Collections.Concurrent;
using System.Threading;
using System;
using System.Threading.Tasks;
using PacmanGame.Server.Services;
using System.Linq;
using System.Collections.Generic;

namespace PacmanGame.Server;

public class RelayServer : INetEventListener
{
    private readonly NetManager _netManager;
    private readonly RoomManager _roomManager;
    private readonly ConcurrentDictionary<NetPeer, Player> _connectedPlayers = new();
    private readonly ILogger _logger;
    private readonly MessagePackSerializerOptions _serializerOptions;
    private readonly IMapLoader _mapLoader;
    private readonly ICollisionDetector _collisionDetector;

    public RelayServer()
    {
        _netManager = new NetManager(this) { DisconnectTimeout = 10000 };
        _roomManager = new RoomManager();
        _logger = new ConsoleLogger();
        _serializerOptions = MessagePack.MessagePackSerializer.DefaultOptions.WithResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        _mapLoader = new MapLoader(_logger);
        _collisionDetector = new CollisionDetector();
    }

    public void Start()
    {
        if (_netManager.Start(IPAddress.Any, IPAddress.IPv6Any, 9050))
        {
            _logger.LogInfo($"Server listening on all interfaces, port 9050");
        }
        else
        {
            _logger.LogError("Failed to start server. Is the port already in use?");
            return;
        }

        Task.Run(() =>
        {
            while (true)
            {
                _netManager.PollEvents();
                foreach (var room in _roomManager.GetPublicRooms().Where(r => r.State == RoomState.Playing))
                {
                    room.Game?.Update(1f / 20f); // 20 FPS
                    if (room.Game != null)
                    {
                        BroadcastToRoom(room, room.Game.GetState());
                    }
                }
                Thread.Sleep(1000 / 20); // 20 FPS
            }
        });
    }

    public void Stop()
    {
        _netManager.Stop();
        _logger.LogInfo("Server stopped.");
    }

    public void OnPeerConnected(NetPeer peer)
    {
        _logger.LogInfo($"Peer connected: {peer.Address}");
        var player = new Player(peer);
        _connectedPlayers.TryAdd(peer, player);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _logger.LogInfo($"Peer disconnected: {peer.Address}, reason: {disconnectInfo.Reason}");
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
            _logger.LogInfo($"Server received message type: {baseMessage.Type} from player {player.Id} ({peer.Address})");
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
            default: _logger.LogWarning($"Unknown message type: {message.Type} from {player.Peer.Address}"); break;
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

                _logger.LogInfo($"✓ Player {player.Id} ({player.Name}) created room '{room.Name}' as Pacman.");
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
            response.Message = "Player name is already taken in this room.";
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

            _logger.LogInfo($"Player {player.Id} ({player.Name}) joined room '{room.Name}'");
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
            _logger.LogInfo($"Player {player.Id} ({player.Name}) has been fully disconnected from room '{room.Name}'.");

            SendMessageToPlayer(player, new LeaveRoomConfirmation());

            if (room.Players.Count == 0)
            {
                _roomManager.RemoveRoom(room.Name);
                _logger.LogInfo($"Room '{room.Name}' is empty and has been removed.");
            }
            else
            {
                if (!room.Players.Any(p => p.IsAdmin))
                {
                    var newAdmin = room.Players.FirstOrDefault();
                    if (newAdmin != null)
                    {
                        newAdmin.IsAdmin = true;
                        _logger.LogInfo($"Admin left. New admin is Player {newAdmin.Id} ({newAdmin.Name}).");
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
                _logger.LogInfo($"Admin {player.Id} assigned role {request.Role} to player {request.PlayerId}");
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
                _logger.LogInfo($"Admin {player.Id} kicked player {playerToKick.Id}");
                SendMessageToPlayer(playerToKick, new KickedEvent { Reason = "Kicked by admin." });
                BroadcastRoomState(room);
            }
        }
    }

    private void HandleStartGameRequest(Player player)
    {
        var room = player.CurrentRoom;
        if (room != null && player.IsAdmin && room.Players.Any(p => p.Role != PlayerRole.None))
        {
            _logger.LogInfo($"Admin {player.Id} is starting game in room '{room.Name}'");
            room.State = RoomState.Playing;
            room.Game = new GameSimulation(_mapLoader, _collisionDetector, _logger);
            room.Game.Initialize(room.Id, room.Players.Select(p => p.Role).ToList());

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
        _logger.LogInfo($"Received GetRoomListRequest from Player {player.Id} ({player.Peer.Address}).");
        var publicRooms = _roomManager.GetPublicRooms().Select(r => new RoomInfo
        {
            RoomId = r.Id,
            Name = r.Name,
            PlayerCount = r.Players.Count(p => p.Role != PlayerRole.None),
            MaxPlayers = 5
        }).ToList();

        var response = new GetRoomListResponse { Rooms = publicRooms };
        SendMessageToPlayer(player, response);
        _logger.LogInfo($"Sent GetRoomListResponse to Player {player.Id} with {publicRooms.Count} rooms.");
    }

    private void BroadcastRoomState(Room room)
    {
        var state = new RoomStateUpdateMessage { Players = room.GetPlayerStates() };
        BroadcastToRoom(room, state);
    }

    private void BroadcastToRoom(Room room, NetworkMessageBase message)
    {
        var bytes = MessagePackSerializer.Serialize(message, _serializerOptions);
        foreach (var p in room.Players)
        {
            p.Peer.Send(bytes, DeliveryMethod.ReliableOrdered);
        }
    }

    private void SendMessageToPlayer(Player player, NetworkMessageBase message)
    {
        var bytes = MessagePackSerializer.Serialize(message, _serializerOptions);
        player.Peer.Send(bytes, DeliveryMethod.ReliableOrdered);
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    public void OnConnectionRequest(ConnectionRequest request) => request.AcceptIfKey("PacmanGame");
}

public interface ILogger
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogDebug(string message);
}

public class ConsoleLogger : ILogger
{
    public void LogInfo(string message) => Console.WriteLine($"[INFO] {message}");
    public void LogWarning(string message) => Console.WriteLine($"[WARN] {message}");
    public void LogError(string message) => Console.Error.WriteLine($"[ERROR] {message}");
    public void LogDebug(string message) => Console.WriteLine($"[DEBUG] {message}");
}
