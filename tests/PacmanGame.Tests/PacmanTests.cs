using Xunit;
using FluentAssertions;
using PacmanGame.Models.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace PacmanGame.Tests;

public class PacmanTests
{
    private readonly Mock<ILogger<Pacman>> _mockLogger;

    public PacmanTests()
    {
        _mockLogger = new Mock<ILogger<Pacman>>();
    }

    [Fact]
    public void ActivatePowerPellet_ShouldMakePacmanInvulnerable()
    {
        // Arrange
        var pacman = new Pacman(10, 10, _mockLogger.Object);
        pacman.PowerPelletDuration = 6.0f;

        // Act
        pacman.ActivatePowerPellet();

        // Assert
        pacman.IsInvulnerable.Should().BeTrue();
        pacman.InvulnerabilityTime.Should().Be(6.0f);
    }

    [Fact]
    public void UpdateInvulnerability_ShouldDecrementTimer()
    {
        // Arrange
        var pacman = new Pacman(10, 10, _mockLogger.Object);
        pacman.PowerPelletDuration = 6.0f;
        pacman.ActivatePowerPellet();

        // Act
        pacman.UpdateInvulnerability(1.0f);

        // Assert
        pacman.InvulnerabilityTime.Should().Be(5.0f);
        pacman.IsInvulnerable.Should().BeTrue();
    }

    [Fact]
    public void UpdateInvulnerability_ShouldReset_WhenTimerExpires()
    {
        // Arrange
        var pacman = new Pacman(10, 10, _mockLogger.Object);
        pacman.PowerPelletDuration = 1.0f;
        pacman.ActivatePowerPellet();

        // Act
        pacman.UpdateInvulnerability(1.1f);

        // Assert
        pacman.IsInvulnerable.Should().BeFalse();
        pacman.InvulnerabilityTime.Should().Be(0f);
    }
}
