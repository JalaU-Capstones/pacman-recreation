using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using PacmanGame.Server;
using PacmanGame.Server.Services;
using PacmanGame.Shared;
using PacmanGame.Server.Models;
using System.Collections.Generic;

namespace PacmanGame.Server.Tests;

public class GameSimulationTests
{
    private readonly Mock<ILogger<GameSimulation>> _mockLogger;
    private readonly Mock<IMapLoader> _mockMapLoader;
    private readonly Mock<ICollisionDetector> _mockCollisionDetector;
    private readonly GameSimulation _simulation;

    public GameSimulationTests()
    {
        _mockLogger = new Mock<ILogger<GameSimulation>>();
        _mockMapLoader = new Mock<IMapLoader>();
        _mockCollisionDetector = new Mock<ICollisionDetector>();

        _simulation = new GameSimulation(
            _mockMapLoader.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public void Initialize_WithPacmanRole_SpawnsPacman()
    {
        // Arrange
        var roles = new List<PlayerRole> { PlayerRole.Pacman };
        _mockMapLoader.Setup(m => m.LoadMap(It.IsAny<string>())).Returns(new TileType[31, 28]);
        // Mock returns (Row, Col) -> (13, 23)
        _mockMapLoader.Setup(m => m.GetPacmanSpawn(It.IsAny<string>())).Returns((13, 23));
        _mockMapLoader.Setup(m => m.GetGhostSpawns(It.IsAny<string>())).Returns(new List<(int Row, int Col)>());
        _mockMapLoader.Setup(m => m.GetCollectibles(It.IsAny<string>())).Returns(new List<Collectible>());

        // Act
        _simulation.Initialize(1, roles);
        var state = _simulation.GetState();

        // Assert
        Assert.NotNull(state.PacmanPosition);
        // Pacman constructor takes (row, col) and assigns Y=row, X=col
        // So X should be 23 (Col), Y should be 13 (Row)
        Assert.Equal(23, state.PacmanPosition!.X);
        Assert.Equal(13, state.PacmanPosition!.Y);
    }

    [Fact]
    public void Initialize_WithBlinkyRole_SpawnsBlinky()
    {
        // Arrange
        var roles = new List<PlayerRole> { PlayerRole.Blinky };
        _mockMapLoader.Setup(m => m.LoadMap(It.IsAny<string>())).Returns(new TileType[31, 28]);
        _mockMapLoader.Setup(m => m.GetGhostSpawns(It.IsAny<string>())).Returns(new List<(int Row, int Col)>
        {
            (11, 12), (11, 13), (11, 14), (11, 15)
        });
        _mockMapLoader.Setup(m => m.GetCollectibles(It.IsAny<string>())).Returns(new List<Collectible>());

        // Act
        _simulation.Initialize(1, roles);
        var state = _simulation.GetState();

        // Assert
        Assert.Single(state.Ghosts);
        Assert.Equal("Blinky", state.Ghosts[0].Type);
    }

    [Fact]
    public void Initialize_WithoutPacmanRole_DoesNotSpawnPacman()
    {
        // Arrange
        var roles = new List<PlayerRole> { PlayerRole.Blinky };
        _mockMapLoader.Setup(m => m.LoadMap(It.IsAny<string>())).Returns(new TileType[31, 28]);
        _mockMapLoader.Setup(m => m.GetGhostSpawns(It.IsAny<string>())).Returns(new List<(int Row, int Col)>
        {
            (11, 12), (11, 13), (11, 14), (11, 15)
        });
        _mockMapLoader.Setup(m => m.GetCollectibles(It.IsAny<string>())).Returns(new List<Collectible>());

        // Act
        _simulation.Initialize(1, roles);
        var state = _simulation.GetState();

        // Assert
        Assert.Null(state.PacmanPosition);
    }

    [Fact]
    public void SetPlayerInput_StoresInputForRole()
    {
        // Arrange
        var roles = new List<PlayerRole> { PlayerRole.Pacman };
        _mockMapLoader.Setup(m => m.LoadMap(It.IsAny<string>())).Returns(new TileType[31, 28]);
        _mockMapLoader.Setup(m => m.GetPacmanSpawn(It.IsAny<string>())).Returns((13, 23));
        _mockMapLoader.Setup(m => m.GetGhostSpawns(It.IsAny<string>())).Returns(new List<(int Row, int Col)>());
        _mockMapLoader.Setup(m => m.GetCollectibles(It.IsAny<string>())).Returns(new List<Collectible>());
        _simulation.Initialize(1, roles);

        // Act
        _simulation.SetPlayerInput(PlayerRole.Pacman, Direction.Right);
        var stateBefore = _simulation.GetState();
        _simulation.Update(0.016f);
        var stateAfter = _simulation.GetState();

        // Assert
        Assert.True(stateAfter.PacmanPosition!.X >= stateBefore.PacmanPosition!.X);
    }
}
