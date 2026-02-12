using System.Windows.Input;
using Microsoft.Extensions.Logging;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
using ReactiveUI;

namespace PacmanGame.ViewModels;

public class MultiplayerMenuViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly NetworkService _networkService;
    private readonly IAudioManager _audioManager;
    private readonly ILogger<MultiplayerMenuViewModel> _logger;

    public ICommand CreateRoomCommand { get; }
    public ICommand JoinRoomCommand { get; }
    public ICommand BackCommand { get; }

    public MultiplayerMenuViewModel(MainWindowViewModel mainWindowViewModel, NetworkService networkService, IAudioManager audioManager, ILogger<MultiplayerMenuViewModel> logger)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _networkService = networkService;
        _audioManager = audioManager;
        _logger = logger;

        _networkService.Start();

        CreateRoomCommand = ReactiveCommand.Create(CreateRoom);
        JoinRoomCommand = ReactiveCommand.Create(JoinRoom);
        BackCommand = ReactiveCommand.Create(Back);
    }

    private void CreateRoom()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _mainWindowViewModel.NavigateTo<CreateRoomViewModel>();
    }

    private void JoinRoom()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _mainWindowViewModel.NavigateTo<RoomListViewModel>();
    }



    private void Back()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _networkService.Stop();
        _mainWindowViewModel.NavigateTo<MainMenuViewModel>();
    }
}
