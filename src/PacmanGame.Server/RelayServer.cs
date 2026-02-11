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
        _netManager = new NetManager(this)
        {
            DisconnectTimeout = 10000
        };
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
                _logger.LogInfo($"Player {player.Id} removed from room {room.Name}");
            }
        }
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        _logger.LogError($"Network error: {socketError} from {endPoint}");
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        if (!_connectedPlayers.TryGetValue(peer, out var player))
        {
            _logger.LogWarning($"Received message from unknown peer: {peer.Address}");
            return;
        }

        var bytes = reader.GetRemainingBytes();
        var baseMessage = MessagePackSerializer.Deserialize<NetworkMessageBase>(bytes, _serializerOptions);
        _logger.LogInfo($"Server received message type: {baseMessage.Type} from player {player.Id} ({peer.Address})");

        switch (baseMessage.Type)
        {
            case MessageType.CreateRoomRequest:
                _logger.LogInfo("Processing CreateRoomRequest...");
                HandleCreateRoomRequest(player, (CreateRoomRequest)baseMessage);
                break;
            case MessageType.JoinRoomRequest:
                _logger.LogInfo("Processing JoinRoomRequest...");
                HandleJoinRoomRequest(player, (JoinRoomRequest)baseMessage);
                break;
            case MessageType.PlayerInput:
                _logger.LogDebug("Processing PlayerInput...");
                HandlePlayerInput(player, (PlayerInputMessage)baseMessage);
                break;
            default:
                _logger.LogWarning($"Unknown message type: {baseMessage.Type} from {peer.Address}");
                break;
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        _logger.LogInfo($"Unconnected message from {remoteEndPoint}, type: {messageType}");
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        request.AcceptIfKey("PacmanGame");
    }

    private void HandleCreateRoomRequest(Player player, CreateRoomRequest request)
    {
        _logger.LogInfo($"HandleCreateRoomRequest: Player {player.Id} requesting to create room '{request.RoomName}'");
        var response = new CreateRoomResponse();

        if (player.CurrentRoom != null)
        {
            response.Success = false;
            response.Message = "Already in a room.";
            _logger.LogWarning($"Player {player.Id} already in a room. Denying request.");
        }
        else
        {
            _logger.LogInfo($"Attempting to create room '{request.RoomName}' with password: {(string.IsNullOrEmpty(request.Password) ? "none" : "***")}");
            var room = _roomManager.CreateRoom(request.RoomName, request.Password);
            if (room != null)
            {
                room.AddPlayer(player);
                player.CurrentRoom = room;
                player.IsAdmin = true;
                response.Success = true;
                response.Message = $"Room '{room.Name}' created successfully.";
                response.RoomId = room.Id;
                response.RoomName = room.Name;
                _logger.LogInfo($"✓ Player {player.Id} created room '{room.Name}'. Sending success response.");
            }
            else
            {
                response.Success = false;
                response.Message = $"Room '{request.RoomName}' already exists.";
                _logger.LogWarning($"Room '{request.RoomName}' already exists. Denying request.");
            }
        }

        _logger.LogInfo($"Sending CreateRoomResponse to player {player.Id}: Success={response.Success}, Message={response.Message}");
        player.Peer.Send(MessagePackSerializer.Serialize<NetworkMessageBase>(response, _serializerOptions), DeliveryMethod.ReliableOrdered);
    }

    private void HandleJoinRoomRequest(Player player, JoinRoomRequest request)
    {
        var response = new JoinRoomResponse();

        if (player.CurrentRoom != null)
        {
            response.Success = false;
            response.Message = "Already in a room.";
        }
        else
        {
            var room = _roomManager.GetRoom(request.RoomName);
            if (room != null)
            {
                if (room.IsPublic || room.Password == request.Password)
                {
                    if (room.AddPlayer(player))
                    {
                        player.CurrentRoom = room;
                        response.Success = true;
                        response.Message = $"Joined room '{room.Name}' successfully.";
                        response.RoomId = room.Id;
                        response.RoomName = room.Name;
                        _logger.LogInfo($"Player {player.Id} joined room '{room.Name}'");
                    }
                    else
                    {
                        response.Success = false;
                        response.Message = "Room is full.";
                    }
                }
                else
                {
                    response.Success = false;
                    response.Message = "Invalid password.";
                }
            }
            else
            {
                response.Success = false;
                response.Message = $"Room '{request.RoomName}' not found.";
            }
        }

        player.Peer.Send(MessagePackSerializer.Serialize<NetworkMessageBase>(response, _serializerOptions), DeliveryMethod.ReliableOrdered);
    }

    private void HandlePlayerInput(Player player, PlayerInputMessage input)
    {
        _logger.LogDebug($"Player {player.Id} input: {input.Direction}");
    }
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
