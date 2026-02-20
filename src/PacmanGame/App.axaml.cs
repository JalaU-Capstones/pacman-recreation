using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PacmanGame.Services;
using PacmanGame.Services.ConsoleCommands;
using PacmanGame.Services.Interfaces;
using PacmanGame.ViewModels;
using PacmanGame.ViewModels.Creative;
using PacmanGame.Views;
using System;
using System.Threading.Tasks;

namespace PacmanGame;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();

            // Configure Dependency Injection
            var services = new ServiceCollection();
            services.AddSingleton<IStorageProvider>(mainWindow.StorageProvider);

            // Logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Services
            services.AddSingleton<IMapLoader, MapLoader>();
            services.AddSingleton<ISpriteManager, SpriteManager>();
            services.AddSingleton<IAudioManager, AudioManager>();
            services.AddSingleton<ICollisionDetector, CollisionDetector>();
            services.AddSingleton<IProfileManager, ProfileManager>();
            services.AddTransient<IGameEngine, GameEngine>();
            services.AddSingleton<NetworkService>();
            services.AddSingleton<GlobalLeaderboardCache>();
            services.AddSingleton<IConsoleService, ConsoleService>();
            services.AddTransient<IConsoleCommand, HelpCommand>();
            services.AddTransient<IConsoleCommand, ClearCommand>();
            services.AddTransient<IConsoleCommand, ExitCommand>();
            services.AddTransient<IConsoleCommand, ActiveCommand>();
            services.AddSingleton<ICustomLevelManagerService, CustomLevelManagerService>();

            // ViewModels
            services.AddSingleton<ConsoleViewModel>();
            services.AddTransient<LevelCanvasViewModel>();
            services.AddTransient<ToolboxViewModel>();
            services.AddTransient<CreativeModeViewModel>();
            services.AddTransient<CustomLevelsLibraryViewModel>();
            services.AddSingleton(provider =>
                new MainWindowViewModel(
                    provider,
                    provider.GetRequiredService<ILogger<MainWindowViewModel>>(),
                    provider.GetRequiredService<ConsoleViewModel>()));
            services.AddTransient<MainMenuViewModel>();
            services.AddTransient<GameViewModel>();
            services.AddTransient<ScoreBoardViewModel>();
            services.AddTransient<ProfileCreationViewModel>();
            services.AddTransient<ProfileSelectionViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<RoomLobbyViewModel>();
            services.AddTransient<GlobalLeaderboardViewModel>();
            services.AddTransient<MultiplayerMenuViewModel>();
            services.AddTransient<CreateRoomViewModel>();
            services.AddTransient<RoomListViewModel>();
            services.AddTransient<MultiplayerGameViewModel>();

            _serviceProvider = services.BuildServiceProvider();

            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);

            var mainWindowViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = mainWindow;
            mainWindow.DataContext = mainWindowViewModel;

            // Initialize ProfileManager and set initial view
            var profileManager = _serviceProvider.GetRequiredService<IProfileManager>();
            try
            {
                await profileManager.InitializeAsync();
                var profiles = await Task.Run(() => profileManager.GetAllProfiles());

                ViewModelBase initialViewModel;
                if (profiles.Count == 0)
                {
                    initialViewModel = _serviceProvider.GetRequiredService<ProfileCreationViewModel>();
                }
                else
                {
                    initialViewModel = _serviceProvider.GetRequiredService<ProfileSelectionViewModel>();
                }

                mainWindowViewModel.CurrentViewModel = initialViewModel;
            }
            catch (Exception ex)
            {
                var logger = _serviceProvider.GetService<ILogger<App>>();
                logger?.LogCritical(ex, "FATAL: Failed to initialize application");
            }

            desktop.MainWindow.Show();

            // Hook up exit event
            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (_serviceProvider != null)
        {
            var logger = _serviceProvider.GetService<ILogger<App>>();
            var cache = _serviceProvider.GetService<GlobalLeaderboardCache>();
            if (cache != null)
            {
                // Fire and forget flush, but we try to wait a bit
                try
                {
                    Task.Run(async () => await cache.FlushPendingUpdatesAsync()).Wait(6000);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error flushing global leaderboard cache on exit");
                }
            }

            var networkService = _serviceProvider.GetService<NetworkService>();
            networkService?.Stop();
        }
    }

    // Helper to access services from ViewModels if needed (Service Locator pattern)
    // Prefer constructor injection where possible.
    public static T? GetService<T>() where T : class
    {
        if (Current is App app && app._serviceProvider != null)
        {
            return app._serviceProvider.GetService<T>();
        }
        return null;
    }
}
