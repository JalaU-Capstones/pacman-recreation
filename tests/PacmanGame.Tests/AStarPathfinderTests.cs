using Xunit;
using Moq;
using FluentAssertions;
using PacmanGame.Services.Pathfinding;
using PacmanGame.Services.Interfaces;
using PacmanGame.Models.Enums;
using PacmanGame.Models.Entities;

namespace PacmanGame.Tests;

public class AStarPathfinderTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly AStarPathfinder _sut;

    public AStarPathfinderTests()
    {
        _mockLogger = new Mock<ILogger>();
        _sut = new AStarPathfinder();
    }

    [Fact]
    public void FindPath_ShouldReturnValidPath_WhenRouteExists()
    {
        // Arrange
        var map = new TileType[10, 10];
        var ghost = new Ghost(0, 0);

        // Act
        var direction = _sut.FindPath(0, 0, 9, 9, map, ghost, _mockLogger.Object);

        // Assert
        direction.Should().NotBe(Direction.None);
    }

    [Fact]
    public void FindPath_ShouldReturnEmptyPath_WhenNoRouteExists()
    {
        // Arrange
        var map = new TileType[10, 10];
        // Trap the ghost at (0,0) completely, considering wrapping
        map[0, 1] = TileType.Wall; // Right
        map[1, 0] = TileType.Wall; // Down
        map[0, 9] = TileType.Wall; // Left (wrapped)
        map[9, 0] = TileType.Wall; // Up (wrapped)

        var ghost = new Ghost(0, 0);

        // Act
        var direction = _sut.FindPath(0, 0, 9, 9, map, ghost, _mockLogger.Object);

        // Assert
        direction.Should().Be(Direction.None);
    }
}
