using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using LiteNetLib;
using MessagePack;
using PacmanGame.Helpers;
using PacmanGame.Shared;
using PacmanGame.Services.Interfaces;

namespace PacmanGame.Services;

public class NetworkService : INetEventListener
{
    private static readonly Lazy<NetworkService> _lazyInstance = new(() => new NetworkService());
    public static NetworkService Instance => _lazyInstance.Value;

    private readonly NetManager _netManager;
    private readonly ILogger _logger;
    private NetPeer? _server;
    private bool _isConnected = false;
    private readonly object _connectionLock = new();
    private bool _isStarted = false;
    private int? _currentRoomId;

    // Room events
    public event Action<int, string, RoomVisibility, List<PlayerState>>? OnJoinedRoom;
    public event Action<string>? OnJoinRoomFailed;
    public event Action? OnLeftRoom;
    public event Action<List<PlayerState>>? OnRoomStateUpdate;
    public event Action<string>? OnKicked;
    public event Action<List<RoomInfo>>? OnRoomListReceived;

    // Game events
    public event Action? OnGameStart;
    public event Action<GameStateMessage>? OnGameStateUpdate;

    private NetworkService()
    {
        _logger = new Logger();
        _netManager = new NetManager(this);
    }

    public void Start()
    {
        if (_isStarted) return;
        _isStarted = true;
        _netManager.Start();
        _logger.Info("Starting connection to server...");
        _server = _netManager.Connect(Constants.MultiplayerServerIP, Constants.MultiplayerServerPort, "PacmanGame");
        Task.Run(PollEventsLoop);
    }

    private void PollEventsLoop()
    {
        while (_isStarted)
        {
            _netManager.PollEvents();
            Thread.Sleep(15);
        }
    }

    public bool IsConnected => _isConnected;

    public void Stop()
    {
        if (!_isStarted) return;
        _isStarted = false;
        _netManager.Stop();
        _logger.Info("Network service stopped.");
    }

    private void SendMessage(NetworkMessageBase message)
    {
        if (_server == null || _server.ConnectionState != ConnectionState.Connected)
        {
            _logger.Error("Cannot send message: not connected to server.");
            return;
        }

        try
        {
            var bytes = MessagePackSerializer.Serialize<NetworkMessageBase>(message, MessagePackSerializerOptions.Standard);
            _logger.Debug($"Sending message type: {message.Type}");
            _server.Send(bytes, DeliveryMethod.ReliableOrdered);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error serializing message: {ex.Message}");
        }
    }

    public void SendCreateRoomRequest(CreateRoomRequest request) => SendMessage(request);
    public void SendJoinRoomRequest(JoinRoomRequest request) => SendMessage(request);
    public void SendLeaveRoomRequest() => SendMessage(new LeaveRoomRequest());
    public void SendAssignRoleRequest(int playerId, PlayerRole role) => SendMessage(new AssignRoleRequest { PlayerId = playerId, Role = role });
    public void SendKickPlayerRequest(int playerId) => SendMessage(new KickPlayerRequest { PlayerIdToKick = playerId });
    public void SendStartGameRequest() => SendMessage(new StartGameRequest());
    public void SendPlayerInput(PlayerInputMessage input) => SendMessage(input);
    public void SendGetRoomListRequest() => SendMessage(new GetRoomListRequest());

    public void OnPeerConnected(NetPeer peer)
    {
        _logger.Info($"Connected to server: {peer.Address}");
        _isConnected = true;
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _logger.Warning($"Disconnected from server: {disconnectInfo.Reason}");
        _isConnected = false;
        _currentRoomId = null;
        Dispatcher.UIThread.Post(() => OnLeftRoom?.Invoke());
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) => _logger.Error($"Network error: {socketError}");

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        var bytes = reader.GetRemainingBytes();
        _logger.Debug($"Raw data received. Length: {bytes.Length}");
        try
        {
            var baseMessage = MessagePackSerializer.Deserialize<NetworkMessageBase>(bytes, MessagePackSerializerOptions.Standard);
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    HandleMessage(baseMessage);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error handling message {baseMessage?.Type}: {ex}");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"[FATAL] Failed to deserialize message. Length: {bytes.Length}. Exception: {ex}");
        }
    }

    private void HandleMessage(NetworkMessageBase message)
    {
        if (message == null)
        {
            _logger.Warning("Received null message in HandleMessage");
            return;
        }

        _logger.Debug($"Received message type: {message.Type}");
        switch (message)
        {
            case CreateRoomResponse createResponse:
                if (createResponse.Success)
                {
                    _logger.Info($"Successfully created and joined room '{createResponse.RoomName}'");
                    _currentRoomId = createResponse.RoomId;
                    OnJoinedRoom?.Invoke(createResponse.RoomId, createResponse.RoomName!, createResponse.Visibility, createResponse.Players);
                }
                else
                {
                    _logger.Error($"Failed to create room: {createResponse.Message}");
                    OnJoinRoomFailed?.Invoke(createResponse.Message ?? "Unknown error");
                }
                break;
            case JoinRoomResponse joinResponse:
                if (joinResponse.Success)
                {
                    _logger.Info($"Successfully joined room '{joinResponse.RoomName}'");
                    _currentRoomId = joinResponse.RoomId;
                    OnJoinedRoom?.Invoke(joinResponse.RoomId, joinResponse.RoomName!, joinResponse.Visibility, joinResponse.Players);
                }
                else
                {
                    _logger.Error($"Failed to join room: {joinResponse.Message}");
                    OnJoinRoomFailed?.Invoke(joinResponse.Message ?? "Unknown error");
                }
                break;
            case GetRoomListResponse roomListResponse:
                _logger.Info($"Received room list with {roomListResponse.Rooms.Count} rooms.");
                OnRoomListReceived?.Invoke(roomListResponse.Rooms);
                break;
            case RoomStateUpdateMessage roomUpdate:
                _logger.Info("Received room state update.");
                OnRoomStateUpdate?.Invoke(roomUpdate.Players);
                break;
            case LeaveRoomConfirmation _:
                _logger.Info("Leave room confirmation received. Session state cleared.");
                _currentRoomId = null;
                OnLeftRoom?.Invoke();
                break;
            case KickedEvent kickedEvent:
                _logger.Warning($"Kicked from room: {kickedEvent.Reason}");
                _currentRoomId = null;
                OnKicked?.Invoke(kickedEvent.Reason);
                break;
            case GameStartEvent _:
                _logger.Info("Game is starting!");
                OnGameStart?.Invoke();
                break;
            case GameStateMessage gameState:
                OnGameStateUpdate?.Invoke(gameState);
                break;
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    public void OnConnectionRequest(ConnectionRequest request) { }
}
