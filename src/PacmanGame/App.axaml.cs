using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
using PacmanGame.ViewModels;
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
        Console.WriteLine("OnFrameworkInitializationCompleted started");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Console.WriteLine("Desktop lifetime detected");

            // Configure Dependency Injection
            var services = new ServiceCollection();

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

            // ViewModels
            services.AddSingleton<MainWindowViewModel>();
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
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };

            // Initialize ProfileManager and set initial view
            var profileManager = _serviceProvider.GetRequiredService<IProfileManager>();
            try
            {
                await profileManager.InitializeAsync();
                Console.WriteLine("ProfileManager initialized");

                var profiles = await Task.Run(() => profileManager.GetAllProfiles());
                Console.WriteLine($"Found {profiles.Count} profiles");

                ViewModelBase initialViewModel;
                if (profiles.Count == 0)
                {
                    initialViewModel = _serviceProvider.GetRequiredService<ProfileCreationViewModel>();
                }
                else
                {
                    initialViewModel = _serviceProvider.GetRequiredService<ProfileSelectionViewModel>();
                }
                Console.WriteLine($"Initial ViewModel: {initialViewModel.GetType().Name}");

                mainWindowViewModel.CurrentViewModel = initialViewModel;
                Console.WriteLine("MainWindowViewModel created with CurrentViewModel set");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL: Failed to initialize: {ex}");
            }

            desktop.MainWindow.Show();
            Console.WriteLine("MainWindow.Show() called");

            // Hook up exit event
            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (_serviceProvider != null)
        {
            var cache = _serviceProvider.GetService<GlobalLeaderboardCache>();
            if (cache != null)
            {
                // Fire and forget flush, but we try to wait a bit
                try
                {
                    Task.Run(async () => await cache.FlushPendingUpdatesAsync()).Wait(2000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error flushing cache on exit: {ex}");
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
