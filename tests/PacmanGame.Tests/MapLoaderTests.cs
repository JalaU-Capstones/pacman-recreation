using Xunit;
using Moq;
using FluentAssertions;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
using PacmanGame.Models.Enums;
using System.IO;
using System;
using Microsoft.Extensions.Logging;

namespace PacmanGame.Tests;

public class MapLoaderTests : IDisposable
{
    private readonly Mock<ILogger<MapLoader>> _mockLogger;
    private readonly string _tempMapDir;

    public MapLoaderTests()
    {
        _mockLogger = new Mock<ILogger<MapLoader>>();
        _tempMapDir = Path.Combine(Path.GetTempPath(), "PacmanTestMaps");
        Directory.CreateDirectory(_tempMapDir);
    }

    public void Dispose()
    {
        Directory.Delete(_tempMapDir, true);
    }

    private MapLoader CreateMapLoaderWithTempPath()
    {
        // This is a bit of a hack since the path is internal.
        // A better approach would be to inject the path.
        // For this test, we'll rely on the default behavior and place files where it expects them.
        var assemblyDir = AppContext.BaseDirectory;
        var targetDir = Path.Combine(assemblyDir, "Assets", "Maps");
        Directory.CreateDirectory(targetDir);
        File.Copy(Path.Combine(_tempMapDir, "testmap.txt"), Path.Combine(targetDir, "testmap.txt"), true);
        return new MapLoader(_mockLogger.Object);
    }

    [Fact]
    public void LoadMap_ShouldParseWallsCorrectly()
    {
        // Arrange
        var mapContent = new string[31];
        for (int i = 0; i < 31; i++)
        {
            mapContent[i] = new string('#', 28);
        }
        File.WriteAllLines(Path.Combine(_tempMapDir, "testmap.txt"), mapContent);
        var mapLoader = CreateMapLoaderWithTempPath();

        // Act
        var map = mapLoader.LoadMap("testmap.txt");

        // Assert
        map.Should().NotBeNull();
        map[0, 0].Should().Be(TileType.Wall);
    }
}
