using System;
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

    public event Action<int, string>? OnRoomCreated;
    public event Action<string>? OnRoomCreationFailed;
    public event Action<JoinRoomResponse>? OnJoinRoomResponse;
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

        Task.Run(() =>
        {
            while (true)
            {
                _netManager.PollEvents();
                Thread.Sleep(15);
            }
        });
    }

    public bool IsConnected
    {
        get
        {
            lock (_connectionLock)
            {
                return _isConnected;
            }
        }
    }

    public void WaitForConnection(int timeoutMs = 5000)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (!IsConnected && stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            Thread.Sleep(50);
        }

        if (!IsConnected)
        {
            _logger.Warning($"Connection timeout after {timeoutMs}ms");
        }
    }

    public void Stop()
    {
        if (!_isStarted) return;
        _netManager.Stop();
        _isStarted = false;
    }

    private void SendMessage(NetworkMessageBase message)
    {
        if (_server == null)
        {
            _logger.Error("Cannot send message: _server is null");
            return;
        }

        if (_server.ConnectionState != ConnectionState.Connected)
        {
            _logger.Warning($"Server connection state is {_server.ConnectionState}, message may not be delivered");
        }

        var bytes = MessagePackSerializer.Serialize<NetworkMessageBase>(message, MessagePackSerializerOptions.Standard);
        _logger.Debug($"Sending message type: {message.Type}");
        _server.Send(bytes, DeliveryMethod.ReliableOrdered);
    }

    public void SendCreateRoomRequest(CreateRoomRequest request)
    {
        _logger.Info($"Sending CreateRoomRequest for room '{request.RoomName}' (Visibility: {request.Visibility})...");
        SendMessage(request);
    }

    public void SendJoinRoomRequest(JoinRoomRequest request)
    {
        SendMessage(request);
    }

    public void SendPlayerInput(PlayerInputMessage input)
    {
        SendMessage(input);
    }

    public void OnPeerConnected(NetPeer peer)
    {
        _logger.Info($"Connected to server: {peer.Address}");
        lock (_connectionLock)
        {
            _isConnected = true;
        }
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _logger.Warning($"Disconnected from server: {disconnectInfo.Reason}");
        lock (_connectionLock)
        {
            _isConnected = false;
        }
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        _logger.Error($"Network error: {socketError}");
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        var bytes = reader.GetRemainingBytes();
        var baseMessage = MessagePackSerializer.Deserialize<NetworkMessageBase>(bytes, MessagePackSerializerOptions.Standard);
        _logger.Debug($"Received message type: {baseMessage.Type}");

        Dispatcher.UIThread.Post(() =>
        {
            switch (baseMessage.Type)
            {
                case MessageType.CreateRoomResponse:
                    _logger.Debug("Processing CreateRoomResponse...");
                    var createRoomResponse = (CreateRoomResponse)baseMessage;
                    _logger.Info($"CreateRoomResponse: Success={createRoomResponse.Success}, Message='{createRoomResponse.Message}'");
                    if (createRoomResponse.Success)
                    {
                        OnRoomCreated?.Invoke(createRoomResponse.RoomId, createRoomResponse.RoomName ?? string.Empty);
                    }
                    else
                    {
                        OnRoomCreationFailed?.Invoke(createRoomResponse.Message ?? "Unknown error");
                    }
                    break;
                case MessageType.JoinRoomResponse:
                    _logger.Debug("Processing JoinRoomResponse...");
                    var joinRoomResponse = (JoinRoomResponse)baseMessage;
                    _logger.Info($"JoinRoomResponse: Success={joinRoomResponse.Success}, Message='{joinRoomResponse.Message}'");
                    OnJoinRoomResponse?.Invoke(joinRoomResponse);
                    break;
                case MessageType.GameState:
                    _logger.Debug("Processing GameState...");
                    var gameStateMessage = (GameStateMessage)baseMessage;
                    OnGameStateUpdate?.Invoke(gameStateMessage);
                    break;
            }
        });
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        // Handle unconnected messages
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        // Handle latency updates
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        // Not used on client
    }
}
