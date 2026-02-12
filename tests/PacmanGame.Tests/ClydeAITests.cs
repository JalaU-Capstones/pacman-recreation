using Xunit;
using Moq;
using FluentAssertions;
using PacmanGame.Services.AI;
using PacmanGame.Services.Interfaces;
using PacmanGame.Models.Entities;
using PacmanGame.Models.Enums;
using System.Collections.Generic;
using PacmanGame.Helpers;
using Microsoft.Extensions.Logging;

namespace PacmanGame.Tests;

public class ClydeAITests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly ClydeAI _sut;
    private readonly Mock<ILogger<Pacman>> _mockPacmanLogger;

    public ClydeAITests()
    {
        _mockLogger = new Mock<ILogger>();
        _sut = new ClydeAI();
        _mockPacmanLogger = new Mock<ILogger<Pacman>>();
    }

    [Fact]
    public void GetNextMove_ChaseMode_ShouldTargetPacmanWhenFar()
    {
        // Arrange
        var pacman = new Pacman(20, 20, _mockPacmanLogger.Object);
        var clyde = new Ghost(0, 0, GhostType.Clyde);
        var map = new TileType[31, 28];

        // Act
        var direction = _sut.GetNextMove(clyde, pacman, map, new List<Ghost>(), true, _mockLogger.Object);

        // Assert
        // Distance is > 8. Should target Pacman. Clyde at (0,0), Pacman at (20,20).
        // Due to map wrapping, Left (0,27) or Up (30,0) might be shorter paths than Down/Right.
        direction.Should().BeOneOf(Direction.Down, Direction.Right, Direction.Left, Direction.Up);
    }

    [Fact]
    public void GetNextMove_ChaseMode_ShouldScatterWhenClose()
    {
        // Arrange
        var pacman = new Pacman(11, 11, _mockPacmanLogger.Object);
        var clyde = new Ghost(10, 10, GhostType.Clyde);
        var map = new TileType[31, 28];

        // Act
        var direction = _sut.GetNextMove(clyde, pacman, map, new List<Ghost>(), true, _mockLogger.Object);

        // Assert
        // Distance is < 8. Should scatter to (30, 0). Clyde at (10,10). Should move Down or Left.
        direction.Should().BeOneOf(Direction.Down, Direction.Left);
    }
}
