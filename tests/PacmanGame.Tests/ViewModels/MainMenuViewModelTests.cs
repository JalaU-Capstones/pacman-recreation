using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using PacmanGame.ViewModels;
using PacmanGame.Services.Interfaces;
using System.Reactive;
using System;

namespace PacmanGame.Tests.ViewModels;

public class MainMenuViewModelTests
{
    private readonly Mock<MainWindowViewModel> _mockMainWindowViewModel;
    private readonly Mock<IAudioManager> _mockAudioManager;
    private readonly Mock<ILogger<MainMenuViewModel>> _mockLogger;

    public MainMenuViewModelTests()
    {
        // Use parameterless constructor for the mock to avoid dependency issues
        _mockMainWindowViewModel = new Mock<MainWindowViewModel>();

        _mockAudioManager = new Mock<IAudioManager>();
        _mockLogger = new Mock<ILogger<MainMenuViewModel>>();
    }

    [Fact]
    public void StartGameCommand_NavigatesToGameView()
    {
        // Arrange
        var viewModel = new MainMenuViewModel(
            _mockMainWindowViewModel.Object,
            _mockAudioManager.Object,
            _mockLogger.Object
        );

        // Act
        // Subscribe to ensure execution and exception propagation
        viewModel.StartGameCommand.Execute(Unit.Default).Subscribe();

        // Assert
        _mockMainWindowViewModel.Verify(m => m.NavigateTo<GameViewModel>(), Times.Once);
    }
}
