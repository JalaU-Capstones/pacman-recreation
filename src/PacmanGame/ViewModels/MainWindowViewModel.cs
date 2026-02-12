using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
using PacmanGame.Shared;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PacmanGame.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MainWindowViewModel> _logger;
    private ViewModelBase _currentViewModel;
    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => this.RaiseAndSetIfChanged(ref _currentViewModel, value);
    }

    public MainWindowViewModel(IServiceProvider serviceProvider, ILogger<MainWindowViewModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _currentViewModel = new ViewModelBase(); // Placeholder
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("MainWindowViewModel.InitializeAsync started");

            var profileManager = _serviceProvider.GetRequiredService<IProfileManager>();
            await profileManager.InitializeAsync();

            var profiles = await Task.Run(() => profileManager.GetAllProfiles());
            _logger.LogInformation("Loaded {Count} profiles", profiles.Count);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (profiles.Count == 0)
                {
                    CurrentViewModel = _serviceProvider.GetRequiredService<ProfileCreationViewModel>();
                }
                else
                {
                    CurrentViewModel = _serviceProvider.GetRequiredService<ProfileSelectionViewModel>();
                }
                _logger.LogInformation("MainWindow UI updated with initial view.");
            });

            var networkService = _serviceProvider.GetRequiredService<NetworkService>();
            await Task.Run(() =>
            {
                _logger.LogInformation("Starting NetworkService...");
                networkService.Start();
                _logger.LogInformation("NetworkService started.");
            });
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "A fatal error occurred in MainWindowViewModel.InitializeAsync");
        }
    }

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        CurrentViewModel = _serviceProvider.GetRequiredService<TViewModel>();
    }

    public void NavigateTo(ViewModelBase viewModel)
    {
        CurrentViewModel = viewModel;
    }

    public void NavigateToRoomLobby(int roomId, string roomName, RoomVisibility visibility, List<PlayerState> players)
    {
        var lobbyViewModel = new RoomLobbyViewModel(roomId, roomName, visibility, players, this,
            _serviceProvider.GetRequiredService<NetworkService>(),
            _serviceProvider.GetRequiredService<IAudioManager>(),
            _serviceProvider.GetRequiredService<ILogger<RoomLobbyViewModel>>(),
            _serviceProvider.GetRequiredService<IProfileManager>(),
            _serviceProvider);
        CurrentViewModel = lobbyViewModel;
    }
}
