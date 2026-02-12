using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
using PacmanGame.ViewModels;
using PacmanGame.Views;
using System.Threading.Tasks;

namespace PacmanGame;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(configure =>
            {
                configure.AddDebug();
                configure.AddConsole().SetMinimumLevel(LogLevel.Debug);
            });

            // Register services
            services.AddSingleton<IProfileManager, ProfileManager>();
            services.AddSingleton<NetworkService>();
            services.AddSingleton<IAudioManager, AudioManager>();
            services.AddSingleton<IMapLoader, MapLoader>();
            services.AddSingleton<ISpriteManager, SpriteManager>();
            services.AddSingleton<ICollisionDetector, CollisionDetector>();
            services.AddTransient<IGameEngine, GameEngine>();

            // Register ViewModels
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<MainMenuViewModel>();
            services.AddTransient<GameViewModel>();
            services.AddTransient<MultiplayerMenuViewModel>();
            services.AddTransient<RoomLobbyViewModel>();
            services.AddTransient<CreateRoomViewModel>();
            services.AddTransient<RoomListViewModel>();
            services.AddTransient<ScoreBoardViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<ProfileCreationViewModel>();
            services.AddTransient<ProfileSelectionViewModel>();

            var serviceProvider = services.BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<App>>();
            logger.LogInformation("DI container built.");

            // Initialize audio manager early
            var audioManager = serviceProvider.GetRequiredService<IAudioManager>();
            audioManager.Initialize();
            logger.LogInformation("Audio Manager initialized.");

            var mainWindowViewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel,
            };

            logger.LogInformation("MainWindow created and DataContext set.");

            // Asynchronously initialize the UI and services
            Task.Run(() => mainWindowViewModel.InitializeAsync());
            logger.LogInformation("MainWindowViewModel.InitializeAsync() started in background.");
        }

        base.OnFrameworkInitializationCompleted();
    }
}
