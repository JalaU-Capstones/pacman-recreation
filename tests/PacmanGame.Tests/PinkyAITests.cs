using Xunit;
using Moq;
using FluentAssertions;
using PacmanGame.Services.AI;
using PacmanGame.Services.Interfaces;
using PacmanGame.Models.Entities;
using PacmanGame.Models.Enums;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace PacmanGame.Tests;

public class PinkyAITests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly PinkyAI _sut;
    private readonly Mock<ILogger<Pacman>> _mockPacmanLogger;

    public PinkyAITests()
    {
        _mockLogger = new Mock<ILogger>();
        _sut = new PinkyAI();
        _mockPacmanLogger = new Mock<ILogger<Pacman>>();
    }

    [Fact]
    public void GetNextMove_ChaseMode_ShouldTargetAheadOfPacman()
    {
        // Arrange
        var pacman = new Pacman(15, 20, _mockPacmanLogger.Object) { CurrentDirection = Direction.Right };
        var pinky = new Ghost(10, 10, GhostType.Pinky);
        var map = new TileType[31, 28];

        // Act
        var direction = _sut.GetNextMove(pinky, pacman, map, new List<Ghost>(), true, _mockLogger.Object);

        // Assert
        // Pacman at (15, 20) moving Right. Target is (15, 24). Pinky at (10, 10). Should move Down or Right.
        direction.Should().BeOneOf(Direction.Down, Direction.Right);
    }

    [Fact]
    public void GetNextMove_ScatterMode_ShouldTargetTopLeftCorner()
    {
        // Arrange
        var pacman = new Pacman(15, 20, _mockPacmanLogger.Object);
        var pinky = new Ghost(10, 10, GhostType.Pinky);
        var map = new TileType[31, 28];

        // Act
        var direction = _sut.GetNextMove(pinky, pacman, map, new List<Ghost>(), false, _mockLogger.Object);

        // Assert
        // Scatter target is (0, 0). Pinky at (10, 10). Should move Up or Left.
        direction.Should().BeOneOf(Direction.Up, Direction.Left);
    }
}
