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
using MessagePack.Resolvers;
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

    public RelayServer()
    {
        _netManager = new NetManager(this) { DisconnectTimeout = 10000 };
        _roomManager = new RoomManager();
        _logger = new ConsoleLogger();
        _serializerOptions = MessagePackSerializerOptions.Standard;
    }

    public void Start()
    {
        _netManager.Start(9050);
        _logger.LogInfo("Server listening on port 9050");
        Task.Run(() =>
        {
            while (true)
            {
                _netManager.PollEvents();
                Thread.Sleep(15);
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
            var room = _roomManager.GetRoomForPlayer(player);
            if (room != null)
            {
                room.RemovePlayer(player);
                BroadcastRoomState(room);
                _logger.LogInfo($"Player {player.Id} removed from room {room.Name}");
            }
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
            default: _logger.LogWarning($"Unknown message type: {message.Type} from {player.Peer.Address}"); break;
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

                _logger.LogInfo($"✓ Player {player.Id} ({player.Name}) created room '{room.Name}'.");
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
            _logger.LogInfo($"Player {player.Id} left room '{room.Name}'");
            BroadcastRoomState(room);
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
            BroadcastToRoom(room, new GameStartEvent());
        }
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
