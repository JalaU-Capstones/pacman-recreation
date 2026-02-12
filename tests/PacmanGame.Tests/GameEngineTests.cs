using Xunit;
using Moq;
using FluentAssertions;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
using PacmanGame.Models.Enums;
using PacmanGame.Models.Entities;
using System.Collections.Generic;
using PacmanGame.Helpers;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace PacmanGame.Tests;

public class GameEngineTests
{
    private readonly Mock<IMapLoader> _mockMapLoader;
    private readonly Mock<ISpriteManager> _mockSpriteManager;
    private readonly Mock<IAudioManager> _mockAudioManager;
    private readonly Mock<ICollisionDetector> _mockCollisionDetector;
    private readonly Mock<ILogger<GameEngine>> _mockLogger;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly GameEngine _sut;

    public GameEngineTests()
    {
        _mockMapLoader = new Mock<IMapLoader>();
        _mockSpriteManager = new Mock<ISpriteManager>();
        _mockAudioManager = new Mock<IAudioManager>();
        _mockCollisionDetector = new Mock<ICollisionDetector>();
        _mockLogger = new Mock<ILogger<GameEngine>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

        _mockMapLoader.Setup(m => m.LoadMap(It.IsAny<string>())).Returns(new TileType[31, 28]);
        _mockMapLoader.Setup(m => m.GetPacmanSpawn(It.IsAny<string>())).Returns((1, 1));
        _mockMapLoader.Setup(m => m.GetGhostSpawns(It.IsAny<string>())).Returns(new List<(int, int)> { (1, 2), (1, 3), (1, 4), (1, 5) });
        _mockMapLoader.Setup(m => m.GetCollectibles(It.IsAny<string>())).Returns(new List<(int, int, CollectibleType)>());

        _sut = new GameEngine(
            _mockLogger.Object,
            _mockLoggerFactory.Object,
            _mockMapLoader.Object,
            _mockSpriteManager.Object,
            _mockAudioManager.Object,
            _mockCollisionDetector.Object
        );
    }

    [Fact]
    public void LoadLevel_ShouldInitializeGameState()
    {
        // Act
        _sut.LoadLevel(1);

        // Assert
        _sut.Pacman.Should().NotBeNull();
        _sut.Ghosts.Should().HaveCount(4);
        _mockMapLoader.Verify(m => m.LoadMap("level1.txt"), Times.Once);
    }

    [Fact]
    public void Start_ShouldSetRunningState()
    {
        // Arrange
        _sut.LoadLevel(1);

        // Act
        _sut.Start();

        // Assert
        _sut.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void Update_ShouldIncreaseScore_WhenCollectibleEaten()
    {
        // Arrange
        var collectibles = new List<Collectible> { new Collectible(1, 1, CollectibleType.SmallDot) };
        _mockMapLoader.Setup(m => m.GetCollectibles(It.IsAny<string>())).Returns(collectibles.Select(c => (c.Y, c.X, c.Type)).ToList());
        _sut.LoadLevel(1);
        _sut.Start(); // Start the game loop
        _mockCollisionDetector.Setup(c => c.CheckPacmanCollectibleCollision(_sut.Pacman, It.IsAny<List<Collectible>>())).Returns(collectibles[0]);
        int score = 0;
        _sut.ScoreChanged += (s) => score += s;

        // Act
        _sut.Update(0.1f);

        // Assert
        score.Should().Be(Constants.SmallDotPoints);
    }

    [Fact]
    public void Update_ShouldStartDeathSequence_WhenPacmanHitsGhost()
    {
        // Arrange
        _sut.LoadLevel(1);
        _sut.Start(); // Start the game loop
        var ghost = _sut.Ghosts[0];
        ghost.State = GhostState.Normal;
        _sut.Pacman.CurrentDirection = Direction.None;
        _sut.Pacman.NextDirection = Direction.None;

        // Use It.IsAny to be safe against reference changes, though references should be stable here
        _mockCollisionDetector.Setup(c => c.CheckPacmanGhostCollision(It.IsAny<Pacman>(), It.IsAny<List<Ghost>>())).Returns(ghost);

        // Act
        _sut.Update(0.1f);

        // Assert
        _sut.Pacman.IsDying.Should().BeTrue();
    }
}
