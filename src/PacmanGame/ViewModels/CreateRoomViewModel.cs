using System;
using System.Windows.Input;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
using PacmanGame.Shared;
using ReactiveUI;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace PacmanGame.ViewModels;

public class CreateRoomViewModel : ViewModelBase
{
    private const string ConnectionFailedMessage = "Connection failed: Please try again later";

    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly NetworkService _networkService;
    private readonly IAudioManager _audioManager;
    private readonly ILogger<CreateRoomViewModel> _logger;
    private readonly IProfileManager _profileManager;
    private System.Threading.CancellationTokenSource? _requestTimeoutCts;

    private string _roomName = string.Empty;
    public string RoomName
    {
        get => _roomName;
        set => this.RaiseAndSetIfChanged(ref _roomName, value);
    }

    private bool _isPublic = true;
    public bool IsPublic
    {
        get => _isPublic;
        set => this.RaiseAndSetIfChanged(ref _isPublic, value);
    }

    private bool _isPrivate;
    public bool IsPrivate
    {
        get => _isPrivate;
        set => this.RaiseAndSetIfChanged(ref _isPrivate, value);
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    private bool _isConnectionAlertVisible;
    public bool IsConnectionAlertVisible
    {
        get => _isConnectionAlertVisible;
        set => this.RaiseAndSetIfChanged(ref _isConnectionAlertVisible, value);
    }

    private string _connectionAlertMessage = ConnectionFailedMessage;
    public string ConnectionAlertMessage
    {
        get => _connectionAlertMessage;
        set => this.RaiseAndSetIfChanged(ref _connectionAlertMessage, value);
    }

    public ICommand CreateCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand DismissConnectionAlertCommand { get; }

    public CreateRoomViewModel(MainWindowViewModel mainWindowViewModel, NetworkService networkService, IAudioManager audioManager, ILogger<CreateRoomViewModel> logger, IProfileManager profileManager)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _networkService = networkService;
        _audioManager = audioManager;
        _logger = logger;
        _profileManager = profileManager;

        _networkService.OnJoinedRoom += HandleJoinedRoom;
        _networkService.OnJoinRoomFailed += HandleJoinRoomFailed;
        _networkService.OnConnectionFailed += HandleConnectionFailed;

        CreateCommand = ReactiveCommand.Create(CreateRoom);
        CancelCommand = ReactiveCommand.Create(Cancel);
        DismissConnectionAlertCommand = ReactiveCommand.Create(DismissConnectionAlert);
    }

    private void CreateRoom()
    {
        ErrorMessage = string.Empty;
        IsConnectionAlertVisible = false;
        _logger.LogInformation($"[CreateRoomViewModel] Attempting to create room with name: '{RoomName}'");
        _audioManager.PlaySoundEffect("menu-select");

        if (string.IsNullOrWhiteSpace(RoomName))
        {
            ErrorMessage = "Room name cannot be empty.";
            return;
        }

        var request = new CreateRoomRequest
        {
            RoomName = RoomName,
            Visibility = IsPublic ? RoomVisibility.Public : RoomVisibility.Private,
            Password = IsPrivate ? Password : null,
            PlayerName = _profileManager.GetActiveProfile()?.Name ?? "Player"
        };

        _networkService.SendCreateRoomRequest(request);
        StartRequestTimeout("CreateRoom");
    }

    private void HandleJoinedRoom(int roomId, string roomName, RoomVisibility visibility, List<PlayerState> players, bool isGameStarted)
    {
        CancelRequestTimeout();
        _logger.LogInformation($"[CreateRoomViewModel] Joined room '{roomName}' successfully. Navigating to lobby.");
        _mainWindowViewModel.NavigateToRoomLobby(roomId, roomName, visibility, players);
    }

    private void HandleJoinRoomFailed(string message, JoinRoomFailureReason reason, bool canJoinAsSpectator)
    {
        CancelRequestTimeout();
        var errorMessage = $"Failed to create room: {message}";
        _logger.LogError($"[CreateRoomViewModel] {errorMessage}");
        ErrorMessage = errorMessage;
    }

    private void HandleConnectionFailed(string technicalReason)
    {
        CancelRequestTimeout();
        _logger.LogError("Create room request failed due to connection issue: {Reason}", technicalReason);
        ShowConnectionAlert();
    }

    private void StartRequestTimeout(string operation)
    {
        CancelRequestTimeout();
        _requestTimeoutCts = new System.Threading.CancellationTokenSource();
        var token = _requestTimeoutCts.Token;
        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5), token);
                if (token.IsCancellationRequested) return;
                _logger.LogWarning("{Operation} timed out waiting for server response.", operation);
                Avalonia.Threading.Dispatcher.UIThread.Post(ShowConnectionAlert);
            }
            catch (System.Threading.Tasks.TaskCanceledException)
            {
                // ignore
            }
        }, token);
    }

    private void CancelRequestTimeout()
    {
        try { _requestTimeoutCts?.Cancel(); } catch { }
        _requestTimeoutCts?.Dispose();
        _requestTimeoutCts = null;
    }

    private void ShowConnectionAlert()
    {
        ConnectionAlertMessage = ConnectionFailedMessage;
        IsConnectionAlertVisible = true;
    }

    private void DismissConnectionAlert()
    {
        IsConnectionAlertVisible = false;
        _mainWindowViewModel.NavigateTo<MultiplayerMenuViewModel>();
    }

    private void Cancel()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _mainWindowViewModel.NavigateTo<MultiplayerMenuViewModel>();
    }

    ~CreateRoomViewModel()
    {
        _networkService.OnJoinedRoom -= HandleJoinedRoom;
        _networkService.OnJoinRoomFailed -= HandleJoinRoomFailed;
        _networkService.OnConnectionFailed -= HandleConnectionFailed;
        CancelRequestTimeout();
    }
}
