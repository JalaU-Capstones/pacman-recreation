using Xunit;
using Moq;
using FluentAssertions;
using PacmanGame.Services.AI;
using PacmanGame.Services.Interfaces;
using PacmanGame.Models.Entities;
using PacmanGame.Models.Enums;
using System.Collections.Generic;
using PacmanGame.Helpers;

namespace PacmanGame.Tests;

public class BlinkyAITests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly BlinkyAI _sut;

    public BlinkyAITests()
    {
        _mockLogger = new Mock<ILogger>();
        _sut = new BlinkyAI();
    }

    [Fact]
    public void GetNextMove_ChaseMode_ShouldTargetPacman()
    {
        // Arrange
        var pacman = new Pacman(15, 20);
        var blinky = new Ghost(10, 10, GhostType.Blinky);
        var map = new TileType[31, 28]; // Empty map

        // Act
        var direction = _sut.GetNextMove(blinky, pacman, map, new List<Ghost>(), true, _mockLogger.Object);

        // Assert
        // Pacman is at (15, 20), Blinky at (10, 10). Should move Down or Right.
        direction.Should().BeOneOf(Direction.Down, Direction.Right);
    }

    [Fact]
    public void GetNextMove_ScatterMode_ShouldTargetTopRightCorner()
    {
        // Arrange
        var pacman = new Pacman(15, 20);
        var blinky = new Ghost(10, 10, GhostType.Blinky);
        var map = new TileType[31, 28];

        // Act
        var direction = _sut.GetNextMove(blinky, pacman, map, new List<Ghost>(), false, _mockLogger.Object);

        // Assert
        // Scatter target is (0, 27). Blinky at (10, 10). Should move Up or Right.
        direction.Should().BeOneOf(Direction.Up, Direction.Right);
    }
}
