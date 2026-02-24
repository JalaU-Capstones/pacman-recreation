using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using PacmanGame.ViewModels;
using PacmanGame.Services.Interfaces;
using PacmanGame.Models.Enums;
using System;
using System.Threading.Tasks;
using PacmanGame.Models.Game;

namespace PacmanGame.Tests.ViewModels;

public class GameViewModelTests
{
    private readonly Mock<MainWindowViewModel> _mockMainWindowViewModel;
    private readonly Mock<IProfileManager> _mockProfileManager;
    private readonly Mock<IAudioManager> _mockAudioManager;
    private readonly Mock<IGameEngine> _mockGameEngine;
    private readonly Mock<IKeyBindingService> _mockKeyBindings;
    private readonly Mock<ILogger<GameViewModel>> _mockLogger;

    public GameViewModelTests()
    {
        // Mock dependencies for MainWindowViewModel
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLogger = new Mock<ILogger<MainWindowViewModel>>();

        // Create mock with constructor arguments
        _mockMainWindowViewModel = new Mock<MainWindowViewModel>(mockServiceProvider.Object, mockLogger.Object);

        _mockProfileManager = new Mock<IProfileManager>();
        _mockAudioManager = new Mock<IAudioManager>();
        _mockGameEngine = new Mock<IGameEngine>();
        _mockKeyBindings = new Mock<IKeyBindingService>();
        _mockLogger = new Mock<ILogger<GameViewModel>>();
    }

    [Fact]
    public void StartGame_LoadsLevel1()
    {
        // Arrange
        var viewModel = new GameViewModel(
            _mockMainWindowViewModel.Object,
            _mockProfileManager.Object,
            _mockAudioManager.Object,
            _mockGameEngine.Object,
            _mockKeyBindings.Object,
            _mockLogger.Object
        );

        // Act
        viewModel.StartGame();

        // Assert
        _mockGameEngine.Verify(e => e.LoadLevel(1), Times.Once);
        _mockGameEngine.Verify(e => e.Start(), Times.Once);
        Assert.True(viewModel.IsGameRunning);
    }

    [Fact]
    public void PauseGameCommand_PausesEngine()
    {
        // Arrange
        var viewModel = new GameViewModel(
            _mockMainWindowViewModel.Object,
            _mockProfileManager.Object,
            _mockAudioManager.Object,
            _mockGameEngine.Object,
            _mockKeyBindings.Object,
            _mockLogger.Object
        );
        viewModel.StartGame();

        // Act
        viewModel.PauseGameCommand.Execute(null);

        // Assert
        _mockGameEngine.Verify(e => e.Pause(), Times.Once);
        Assert.True(viewModel.IsPaused);
    }

    [Fact]
    public async Task Victory_OnLevel3_MarksProfileCompletedAllLevels()
    {
        // Arrange
        var profile = new Profile
        {
            Id = 1,
            Name = "TestPlayer",
            AvatarColor = "#FF0000",
            HasCompletedAllLevels = false
        };

        var updatedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        _mockGameEngine.SetupGet(e => e.IsMultiplayerClient).Returns(false);
        _mockGameEngine.SetupGet(e => e.CurrentLevel).Returns(3);
        _mockProfileManager.Setup(p => p.GetCurrentProfileAsync()).ReturnsAsync(profile);
        _mockProfileManager
            .Setup(p => p.UpdateProfileAsync(It.IsAny<Profile>()))
            .Returns<Profile>(p =>
            {
                updatedTcs.TrySetResult(p.HasCompletedAllLevels);
                return Task.CompletedTask;
            });

        var viewModel = new GameViewModel(
            _mockMainWindowViewModel.Object,
            _mockProfileManager.Object,
            _mockAudioManager.Object,
            _mockGameEngine.Object,
            _mockKeyBindings.Object,
            _mockLogger.Object
        );

        // Act: raise engine victory
        _mockGameEngine.Raise(e => e.Victory += null);

        // Assert
        var updated = await updatedTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(updated);
        _mockProfileManager.Verify(p => p.UpdateProfileAsync(It.Is<Profile>(x => x.HasCompletedAllLevels)), Times.Once);
        Assert.Equal("Creative Mode and Global Leaderboard unlocked!", viewModel.VictoryUnlockMessage);
    }
}
