using Xunit;
using FluentAssertions;
using PacmanGame.Services;
using PacmanGame.Models.Entities;
using PacmanGame.Models.Enums;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;

namespace PacmanGame.Tests;

public class CollisionDetectorTests
{
    private readonly CollisionDetector _sut;
    private readonly Mock<ILogger<Pacman>> _mockPacmanLogger;

    public CollisionDetectorTests()
    {
        _sut = new CollisionDetector();
        _mockPacmanLogger = new Mock<ILogger<Pacman>>();
    }

    [Fact]
    public void CheckPacmanGhostCollision_ShouldDetectCollision_WhenOnSameTile()
    {
        // Arrange
        var pacman = new Pacman(10, 10, _mockPacmanLogger.Object);
        var ghost = new Ghost(10, 10);
        var ghosts = new List<Ghost> { ghost };

        // Act
        var result = _sut.CheckPacmanGhostCollision(pacman, ghosts);

        // Assert
        result.Should().Be(ghost);
    }

    [Fact]
    public void CheckPacmanGhostCollision_ShouldReturnNull_WhenFarApart()
    {
        // Arrange
        var pacman = new Pacman(10, 10, _mockPacmanLogger.Object);
        var ghost = new Ghost(20, 20);
        var ghosts = new List<Ghost> { ghost };

        // Act
        var result = _sut.CheckPacmanGhostCollision(pacman, ghosts);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CheckPacmanCollectibleCollision_ShouldDetectCollision()
    {
        // Arrange
        var pacman = new Pacman(5, 5, _mockPacmanLogger.Object);
        var collectible = new Collectible(5, 5, CollectibleType.SmallDot);
        var collectibles = new List<Collectible> { collectible };

        // Act
        var result = _sut.CheckPacmanCollectibleCollision(pacman, collectibles);

        // Assert
        result.Should().Be(collectible);
    }

    [Fact]
    public void IsWallCollision_ShouldReturnTrue_ForWall()
    {
        // Arrange
        var map = new TileType[10, 10];
        map[5, 5] = TileType.Wall;

        // Act
        var result = _sut.IsWallCollision(5, 5, map);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetDistance_ShouldCalculateCorrectly()
    {
        // Arrange
        var e1 = new Pacman(0, 0, _mockPacmanLogger.Object);
        var e2 = new Ghost(3, 4);

        // Act
        var distance = _sut.GetDistance(e1, e2);

        // Assert
        distance.Should().Be(5.0f);
    }
}
