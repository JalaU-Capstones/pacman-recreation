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

public class InkyAITests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly InkyAI _sut;
    private readonly Mock<ILogger<Pacman>> _mockPacmanLogger;

    public InkyAITests()
    {
        _mockLogger = new Mock<ILogger>();
        _sut = new InkyAI();
        _mockPacmanLogger = new Mock<ILogger<Pacman>>();
    }

    [Fact]
    public void GetNextMove_ChaseMode_ShouldUseBlinkyPosition()
    {
        // Arrange
        var pacman = new Pacman(15, 20, _mockPacmanLogger.Object) { CurrentDirection = Direction.Right };
        var inky = new Ghost(10, 10, GhostType.Inky);
        var blinky = new Ghost(5, 5, GhostType.Blinky);
        var map = new TileType[31, 28];
        var ghosts = new List<Ghost> { blinky, inky };

        // Act
        var direction = _sut.GetNextMove(inky, pacman, map, ghosts, true, _mockLogger.Object);

        // Assert
        // Pivot is (15, 22). Vector from Blinky (5,5) to pivot is (10, 17).
        // Target is (15, 22) + (10, 17) = (25, 39), clamped to (25, 27).
        // Inky at (10, 10). Should move Down or Right.
        direction.Should().BeOneOf(Direction.Down, Direction.Right);
    }
}
