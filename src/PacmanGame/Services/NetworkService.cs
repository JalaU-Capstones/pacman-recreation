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
using Microsoft.Extensions.Logging;

namespace PacmanGame.Services;

public class NetworkService : INetEventListener
{
    private readonly NetManager _netManager;
    private readonly ILogger<NetworkService> _logger;
    private NetPeer? _server;
    private bool _isConnected;
    private bool _isStarted;

    // Room events
    public event Action<int, string, RoomVisibility, List<PlayerState>>? OnJoinedRoom;
    public event Action<string>? OnJoinRoomFailed;
    public event Action? OnLeftRoom;
    public event Action<List<PlayerState>>? OnRoomStateUpdate;
    public event Action<string>? OnKicked;
    public event Action<List<RoomInfo>>? OnRoomListReceived;

    // Game events
    public event Action<GameStartEvent>? OnGameStart;
    public event Action<GameStateMessage>? OnGameStateUpdate;
    public event Action<GameEventMessage>? OnGameEvent;
    public event Action<bool>? OnGamePaused;

    public NetworkService(ILogger<NetworkService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _netManager = new NetManager(this);
    }

    public void Start()
    {
        if (_isStarted) return;
        _isStarted = true;
        Console.WriteLine("DEBUG: NetworkService.Start() called");
        _netManager.Start();
        _logger.LogInformation("Starting connection to server...");
        Console.WriteLine($"DEBUG: Connecting to server at {Constants.MultiplayerServerIP}:{Constants.MultiplayerServerPort}");
        _server = _netManager.Connect(Constants.MultiplayerServerIP, Constants.MultiplayerServerPort, "PacmanGame");
        Console.WriteLine("DEBUG: Server connection initiated, starting PollEventsLoop");
        Task.Run(PollEventsLoop);
        Console.WriteLine("DEBUG: NetworkService.Start() completed");
    }

    private void PollEventsLoop()
    {
        Console.WriteLine("DEBUG: PollEventsLoop started");
        try
        {
            while (_isStarted)
            {
                _netManager.PollEvents();
                Thread.Sleep(15);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DEBUG: PollEventsLoop exception: {ex}");
            _logger.LogError(ex, "Error in PollEventsLoop");
        }
        finally
        {
            Console.WriteLine("DEBUG: PollEventsLoop ended");
        }
    }

    public bool IsConnected => _isConnected;

    public void Stop()
    {
        if (!_isStarted) return;
        _isStarted = false;
        _netManager.Stop();
        _logger.LogInformation("Network service stopped.");
    }

    private void SendMessage(NetworkMessageBase message)
    {
        if (_server == null || _server.ConnectionState != ConnectionState.Connected)
        {
            _logger.LogError("Cannot send message: not connected to server.");
            return;
        }

        try
        {
            var bytes = MessagePackSerializer.Serialize(message, MessagePackSerializerOptions.Standard);
            _logger.LogDebug($"Sending message type: {message.Type}");
            _server.Send(bytes, DeliveryMethod.ReliableOrdered);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error serializing message: {Exception}", ex);
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
    public void SendPauseGameRequest() => SendMessage(new PauseGameRequest());

    public void OnPeerConnected(NetPeer peer)
    {
        _logger.LogInformation($"Connected to server: {peer.Address}");
        _isConnected = true;
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _logger.LogWarning("Disconnected from server: {Reason}", disconnectInfo.Reason);
        _isConnected = false;
        Dispatcher.UIThread.Post(() => OnLeftRoom?.Invoke());
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) => _logger.LogError("Network error: {SocketError}", socketError);

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        var bytes = reader.GetRemainingBytes();
        _logger.LogDebug("Raw data received. Length: {Length}", bytes.Length);
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
                    _logger.LogError("Error handling message {MessageType}: {Exception}", baseMessage.Type, ex);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("[FATAL] Failed to deserialize message. Length: {Length}. Exception: {Exception}", bytes.Length, ex);
        }
    }

    private void HandleMessage(NetworkMessageBase message)
    {
        _logger.LogDebug("Received message type: {MessageType}", message.Type);
        switch (message)
        {
            case CreateRoomResponse createResponse:
                if (createResponse.Success)
                {
                    _logger.LogInformation("Successfully created and joined room '{RoomName}'", createResponse.RoomName);
                    OnJoinedRoom?.Invoke(createResponse.RoomId, createResponse.RoomName!, createResponse.Visibility, createResponse.Players);
                }
                else
                {
                    _logger.LogError("Failed to create room: {Message}", createResponse.Message);
                    OnJoinRoomFailed?.Invoke(createResponse.Message ?? "Unknown error");
                }
                break;
            case JoinRoomResponse joinResponse:
                if (joinResponse.Success)
                {
                    _logger.LogInformation("Successfully joined room '{RoomName}'", joinResponse.RoomName);
                    OnJoinedRoom?.Invoke(joinResponse.RoomId, joinResponse.RoomName!, joinResponse.Visibility, joinResponse.Players);
                }
                else
                {
                    _logger.LogError("Failed to join room: {Message}", joinResponse.Message);
                    OnJoinRoomFailed?.Invoke(joinResponse.Message ?? "Unknown error");
                }
                break;
            case GetRoomListResponse roomListResponse:
                _logger.LogInformation("Received room list with {RoomCount} rooms.", roomListResponse.Rooms.Count);
                OnRoomListReceived?.Invoke(roomListResponse.Rooms);
                break;
            case RoomStateUpdateMessage roomUpdate:
                _logger.LogInformation("Received room state update.");
                OnRoomStateUpdate?.Invoke(roomUpdate.Players);
                break;
            case LeaveRoomConfirmation _:
                _logger.LogInformation("Leave room confirmation received. Session state cleared.");
                OnLeftRoom?.Invoke();
                break;
            case KickedEvent kickedEvent:
                _logger.LogWarning("Kicked from room: {Reason}", kickedEvent.Reason);
                OnKicked?.Invoke(kickedEvent.Reason);
                break;
            case GameStartEvent gameStartEvent:
                _logger.LogInformation("Game is starting!");
                OnGameStart?.Invoke(gameStartEvent);
                break;
            case GameStateMessage gameState:
                OnGameStateUpdate?.Invoke(gameState);
                break;
            case GameEventMessage gameEvent:
                OnGameEvent?.Invoke(gameEvent);
                break;
            case GamePausedEvent gamePausedEvent:
                OnGamePaused?.Invoke(gamePausedEvent.IsPaused);
                break;
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    public void OnConnectionRequest(ConnectionRequest request) { }
}
