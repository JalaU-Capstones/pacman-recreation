using System.Windows.Input;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
using PacmanGame.Shared;
using ReactiveUI;

namespace PacmanGame.ViewModels;

public class CreateRoomViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly NetworkService _networkService;
    private readonly IAudioManager _audioManager;
    private readonly ILogger _logger;
    private readonly IProfileManager _profileManager;

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

    public ICommand CreateCommand { get; }
    public ICommand CancelCommand { get; }

    public CreateRoomViewModel(MainWindowViewModel mainWindowViewModel, IAudioManager audioManager, ILogger logger, IProfileManager profileManager)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _networkService = NetworkService.Instance;
        _audioManager = audioManager;
        _logger = logger;
        _profileManager = profileManager;
        _networkService.OnRoomCreated += HandleRoomCreated;
        _networkService.OnRoomCreationFailed += HandleRoomCreationFailed;

        CreateCommand = ReactiveCommand.Create(CreateRoom);
        CancelCommand = ReactiveCommand.Create(Cancel);
    }

    private void CreateRoom()
    {
        ErrorMessage = string.Empty;
        _logger.Info($"[CreateRoomViewModel] Attempting to create room with name: '{RoomName}', Public: {IsPublic}, Private: {IsPrivate}");
        _audioManager.PlaySoundEffect("menu-select");

        if (string.IsNullOrWhiteSpace(RoomName))
        {
            ErrorMessage = "Room name cannot be empty.";
            _logger.Warning("[CreateRoomViewModel] Room name validation failed: empty name.");
            return;
        }

        if (!_networkService.IsConnected)
        {
            _logger.Info("[CreateRoomViewModel] Not connected to server, attempting to reconnect...");
            ErrorMessage = "Connecting to server...";
            _networkService.WaitForConnection(timeoutMs: 5000);

            if (!_networkService.IsConnected)
            {
                ErrorMessage = "Failed to connect to server. Please check if the server is running.";
                _logger.Error("[CreateRoomViewModel] Failed to connect to server after timeout.");
                return;
            }
        }

        var request = new CreateRoomRequest
        {
            RoomName = RoomName,
            Visibility = IsPublic ? RoomVisibility.Public : RoomVisibility.Private,
            Password = IsPrivate ? Password : null
        };

        _logger.Info($"[CreateRoomViewModel] Sending CreateRoomRequest: RoomName='{request.RoomName}', Visibility='{request.Visibility}'");
        _networkService.SendCreateRoomRequest(request);
    }

    private void HandleRoomCreated(int roomId, string roomName)
    {
        _logger.Info($"[CreateRoomViewModel] Room '{roomName}' (ID: {roomId}) created successfully. Navigating to lobby.");
        _mainWindowViewModel.NavigateToRoomLobby(roomId, roomName, true);
    }

    private void HandleRoomCreationFailed(string message)
    {
        var errorMessage = $"Failed to create room: {message}";
        _logger.Error($"[CreateRoomViewModel] {errorMessage}");
        ErrorMessage = errorMessage;
    }

    private void Cancel()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _mainWindowViewModel.NavigateTo(new MultiplayerMenuViewModel(_mainWindowViewModel, _networkService, _audioManager, _logger, _profileManager));
    }

    ~CreateRoomViewModel()
    {
        _networkService.OnRoomCreated -= HandleRoomCreated;
        _networkService.OnRoomCreationFailed -= HandleRoomCreationFailed;
    }
}
