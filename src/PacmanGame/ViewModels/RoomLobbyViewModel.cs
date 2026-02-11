using System.Collections.ObjectModel;
using System.Windows.Input;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
using PacmanGame.Shared;
using ReactiveUI;

namespace PacmanGame.ViewModels;

public class RoomLobbyViewModel : ViewModelBase
{
    public string RoomName { get; }
    public bool IsAdmin { get; }
    public ObservableCollection<PlayerViewModel> Players { get; } = new();
    public ObservableCollection<PlayerRole> Roles { get; } = new();

    public ICommand StartGameCommand { get; }
    public ICommand LeaveRoomCommand { get; }
    public ICommand KickPlayerCommand { get; }

    public RoomLobbyViewModel(int roomId, string roomName, bool isAdmin, NetworkService networkService, MainWindowViewModel mainWindowViewModel, IAudioManager audioManager, ILogger logger, IProfileManager profileManager)
    {
        RoomName = roomName;
        IsAdmin = isAdmin;

        StartGameCommand = ReactiveCommand.Create(() => { });
        LeaveRoomCommand = ReactiveCommand.Create(() => { });
        KickPlayerCommand = ReactiveCommand.Create<PlayerViewModel>(player => { });
    }
}
