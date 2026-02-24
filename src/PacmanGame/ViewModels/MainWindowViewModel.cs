using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
using PacmanGame.Shared;
using PacmanGame.ViewModels.Creative;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PacmanGame.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MainWindowViewModel> _logger;
    public ConsoleViewModel ConsoleViewModel { get; }
    private ViewModelBase _currentViewModel;
    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => this.RaiseAndSetIfChanged(ref _currentViewModel, value);
    }

    private bool _isMuted;
    public bool IsMuted
    {
        get => _isMuted;
        set => this.RaiseAndSetIfChanged(ref _isMuted, value);
    }

    // Default constructor for Moq
    public MainWindowViewModel()
    {
        _serviceProvider = null!;
        _logger = null!;
        ConsoleViewModel = null!;
        _currentViewModel = new ViewModelBase();
        _isMuted = false;
    }

    public MainWindowViewModel(IServiceProvider serviceProvider, ILogger<MainWindowViewModel> logger, ConsoleViewModel consoleViewModel)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _currentViewModel = new ViewModelBase(); // Placeholder
        ConsoleViewModel = consoleViewModel;
        _isMuted = false;
    }

    // Back-compat constructor for tests/mocks that only pass serviceProvider + logger.
    // Runtime DI should use the 3-arg ctor so ConsoleViewModel is provided.
    public MainWindowViewModel(IServiceProvider serviceProvider, ILogger<MainWindowViewModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _currentViewModel = new ViewModelBase(); // Placeholder
        ConsoleViewModel = null!;
        _isMuted = false;
    }

    public virtual async Task InitializeAsync()
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

    public virtual void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        CurrentViewModel = _serviceProvider.GetRequiredService<TViewModel>();
    }

    public virtual TViewModel CreateViewModel<TViewModel>() where TViewModel : ViewModelBase
    {
        return _serviceProvider.GetRequiredService<TViewModel>();
    }

    public virtual void NavigateTo(ViewModelBase viewModel)
    {
        CurrentViewModel = viewModel;
    }

    public virtual void NavigateToRoomLobby(int roomId, string roomName, RoomVisibility visibility, List<PlayerState> players)
    {
        var lobbyViewModel = new RoomLobbyViewModel(roomId, roomName, visibility, players, this,
            _serviceProvider.GetRequiredService<NetworkService>(),
            _serviceProvider.GetRequiredService<IAudioManager>(),
            _serviceProvider.GetRequiredService<ILogger<RoomLobbyViewModel>>(),
            _serviceProvider.GetRequiredService<IProfileManager>(),
            _serviceProvider);
        CurrentViewModel = lobbyViewModel;
    }

    public void NavigateToCreativeMode()
    {
        if (_serviceProvider == null) return;
        var creativeViewModel = _serviceProvider.GetRequiredService<CreativeModeViewModel>();
        CurrentViewModel = creativeViewModel;
    }
}
