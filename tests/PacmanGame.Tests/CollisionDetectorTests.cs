using Xunit;
using FluentAssertions;
using PacmanGame.Services;
using PacmanGame.Models.Entities;
using PacmanGame.Models.Enums;
using System.Collections.Generic;

namespace PacmanGame.Tests;

public class CollisionDetectorTests
{
    private readonly CollisionDetector _sut;

    public CollisionDetectorTests()
    {
        _sut = new CollisionDetector();
    }

    [Fact]
    public void CheckPacmanGhostCollision_ShouldDetectCollision_WhenOnSameTile()
    {
        // Arrange
        var pacman = new Pacman(10, 10);
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
        var pacman = new Pacman(10, 10);
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
        var pacman = new Pacman(5, 5);
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
        var e1 = new Pacman(0, 0);
        var e2 = new Ghost(3, 4);

        // Act
        var distance = _sut.GetDistance(e1, e2);

        // Assert
        distance.Should().Be(5.0f);
    }
}
