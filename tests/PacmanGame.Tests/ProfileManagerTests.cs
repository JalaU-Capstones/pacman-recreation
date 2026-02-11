using Xunit;
using Moq;
using FluentAssertions;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
using System;
using System.IO;
using System.Linq;

namespace PacmanGame.Tests;

public class ProfileManagerTests : IDisposable
{
    private readonly ProfileManager _sut;
    private readonly string _testDbPath;
    private readonly Mock<ILogger> _mockLogger;

    public ProfileManagerTests()
    {
        _mockLogger = new Mock<ILogger>();
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        _sut = new ProfileManager(_mockLogger.Object, _testDbPath);
    }

    public void Dispose()
    {
        // A bit of a hack to make sure the connection is closed before deleting.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }
    }

    [Fact]
    public void CreateProfile_ShouldPersistToDatabase()
    {
        // Act
        var profile = _sut.CreateProfile("TestPlayer", "#FF0000");

        // Assert
        profile.Should().NotBeNull();
        profile.Name.Should().Be("TestPlayer");

        var profiles = _sut.GetAllProfiles();
        profiles.Should().ContainSingle(p => p.Name == "TestPlayer");
    }

    [Fact]
    public void SaveScore_ShouldPersistScore()
    {
        // Arrange
        var profile = _sut.CreateProfile("TestPlayer", "#FF0000");

        // Act
        _sut.SaveScore(profile.Id, 12345, 3);

        // Assert
        var scores = _sut.GetTopScores(10);
        scores.Should().ContainSingle(s => s.Score == 12345 && s.PlayerName == "TestPlayer");
    }
}
