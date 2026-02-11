using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
using PacmanGame.Shared;
using ReactiveUI;

namespace PacmanGame.ViewModels;

public class RoomLobbyViewModel : ViewModelBase
{
    public string RoomName { get; }
    public string RoomVisibility { get; }
    public bool IsAdmin { get; }

    private bool _canStartGame;
    public bool CanStartGame
    {
        get => _canStartGame;
        set => this.RaiseAndSetIfChanged(ref _canStartGame, value);
    }

    private string _statusText = "Waiting for admin to start...";
    public string StatusText
    {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    private int _spectatorCount;
    public int SpectatorCount
    {
        get => _spectatorCount;
        set => this.RaiseAndSetIfChanged(ref _spectatorCount, value);
    }

    public ObservableCollection<PlayerViewModel> Players { get; } = new();
    public ObservableCollection<PlayerRole> Roles { get; } = new();

    public ICommand StartGameCommand { get; }
    public ICommand LeaveRoomCommand { get; }
    public ICommand KickPlayerCommand { get; }

    public RoomLobbyViewModel(int roomId, string roomName, bool isAdmin, NetworkService networkService, MainWindowViewModel mainWindowViewModel, IAudioManager audioManager, ILogger logger, IProfileManager profileManager)
    {
        RoomName = roomName;
        IsAdmin = isAdmin;
        RoomVisibility = isAdmin ? "PRIVATE" : "PUBLIC"; // This is a placeholder

        // Populate roles
        Roles.Add(PlayerRole.None);
        Roles.Add(PlayerRole.Pacman);
        Roles.Add(PlayerRole.Blinky);
        Roles.Add(PlayerRole.Pinky);
        Roles.Add(PlayerRole.Inky);
        Roles.Add(PlayerRole.Clyde);

        // Dummy data for testing
        Players.Add(new PlayerViewModel { Name = "Player1", IsYou = true, IsAdmin = true, Role = PlayerRole.Pacman });
        Players.Add(new PlayerViewModel { Name = "Player2", Role = PlayerRole.Blinky });
        Players.Add(new PlayerViewModel { Name = "Player3", Role = PlayerRole.None });

        StartGameCommand = ReactiveCommand.Create(() => { }, this.WhenAnyValue(x => x.CanStartGame));
        LeaveRoomCommand = ReactiveCommand.Create(() => { });
        KickPlayerCommand = ReactiveCommand.Create<PlayerViewModel>(player => { });

        this.WhenAnyValue(x => x.Players.Count)
            .Subscribe(_ => UpdateCanStartGame());
    }

    private void UpdateCanStartGame()
    {
        CanStartGame = Players.Any(p => p.Role != PlayerRole.None);
    }
}
