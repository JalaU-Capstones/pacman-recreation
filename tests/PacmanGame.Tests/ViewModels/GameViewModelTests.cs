using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using PacmanGame.ViewModels;
using PacmanGame.Services.Interfaces;
using PacmanGame.Models.Enums;
using System;

namespace PacmanGame.Tests.ViewModels;

public class GameViewModelTests
{
    private readonly Mock<MainWindowViewModel> _mockMainWindowViewModel;
    private readonly Mock<IProfileManager> _mockProfileManager;
    private readonly Mock<IAudioManager> _mockAudioManager;
    private readonly Mock<IGameEngine> _mockGameEngine;
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
            _mockLogger.Object
        );
        viewModel.StartGame();

        // Act
        viewModel.PauseGameCommand.Execute(null);

        // Assert
        _mockGameEngine.Verify(e => e.Pause(), Times.Once);
        Assert.True(viewModel.IsPaused);
    }
}
