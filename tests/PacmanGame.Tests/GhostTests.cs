using Xunit;
using Moq;
using FluentAssertions;
using PacmanGame.Models.Entities;
using PacmanGame.Models.Enums;
using PacmanGame.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace PacmanGame.Tests;

public class GhostTests
{
    private readonly Mock<ILogger> _mockLogger;

    public GhostTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public void MakeVulnerable_ShouldChangeState()
    {
        // Arrange
        var ghost = new Ghost(10, 10, GhostType.Blinky) { State = GhostState.Normal };

        // Act
        ghost.MakeVulnerable(6.0f, _mockLogger.Object);

        // Assert
        ghost.State.Should().Be(GhostState.Vulnerable);
        ghost.VulnerableTime.Should().Be(6.0f);
    }

    [Fact]
    public void UpdateVulnerability_ShouldEnterWarningState()
    {
        // Arrange
        var ghost = new Ghost(10, 10, GhostType.Blinky) { State = GhostState.Normal };
        ghost.MakeVulnerable(2.1f, _mockLogger.Object);

        // Act
        ghost.UpdateVulnerability(0.2f, _mockLogger.Object);

        // Assert
        ghost.State.Should().Be(GhostState.Warning);
    }

    [Fact]
    public void GetEaten_ShouldChangeStateToEaten()
    {
        // Arrange
        var ghost = new Ghost(10, 10, GhostType.Blinky) { State = GhostState.Vulnerable };

        // Act
        ghost.GetEaten();

        // Assert
        ghost.State.Should().Be(GhostState.Eaten);
    }

    [Fact]
    public void Respawn_ShouldResetState()
    {
        // Arrange
        var ghost = new Ghost(10, 10, GhostType.Blinky) { State = GhostState.Eaten };
        ghost.SpawnX = 1;
        ghost.SpawnY = 1;

        // Act
        ghost.Respawn(_mockLogger.Object);

        // Assert
        ghost.State.Should().Be(GhostState.Normal);
        ghost.X.Should().Be(1);
        ghost.Y.Should().Be(1);
    }
}
