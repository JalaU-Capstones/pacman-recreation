using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PacmanGame.Models.CustomLevel;

/// <summary>
/// Export metadata for Creative Mode projects.
/// Stored as metadata.json inside .pacproj archives.
/// </summary>
public sealed class ProjectMetadata
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "1.0.1";

    [JsonPropertyName("projectName")]
    public string ProjectName { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }

    [JsonPropertyName("isEditable")]
    public bool IsEditable { get; set; }

    [JsonPropertyName("global")]
    public ProjectMetadataGlobal Global { get; set; } = new();

    [JsonPropertyName("levels")]
    public List<ProjectMetadataLevel> Levels { get; set; } = new();
}

public sealed class ProjectMetadataGlobal
{
    [JsonPropertyName("lives")]
    public int Lives { get; set; }

    [JsonPropertyName("levelCount")]
    public int LevelCount { get; set; }

    [JsonPropertyName("winScore")]
    public int WinScore { get; set; }

    [JsonPropertyName("winScoreMin")]
    public int WinScoreMin { get; set; }

    [JsonPropertyName("winScoreMax")]
    public int WinScoreMax { get; set; }
}

public sealed class ProjectMetadataLevel
{
    [JsonPropertyName("levelNumber")]
    public int LevelNumber { get; set; }

    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;

    [JsonPropertyName("pacmanSpeedMultiplier")]
    public double PacmanSpeedMultiplier { get; set; }

    [JsonPropertyName("ghostSpeedMultiplier")]
    public double GhostSpeedMultiplier { get; set; }

    [JsonPropertyName("speedMinMultiplier")]
    public double SpeedMinMultiplier { get; set; }

    [JsonPropertyName("speedMaxMultiplier")]
    public double SpeedMaxMultiplier { get; set; }

    [JsonPropertyName("frightenedDurationSeconds")]
    public int FrightenedDurationSeconds { get; set; }

    [JsonPropertyName("frightenedMaxSeconds")]
    public int FrightenedMaxSeconds { get; set; }

    [JsonPropertyName("fruitPoints")]
    public int FruitPoints { get; set; }

    [JsonPropertyName("fruitMinPoints")]
    public int FruitMinPoints { get; set; }

    [JsonPropertyName("fruitMaxPoints")]
    public int FruitMaxPoints { get; set; }

    [JsonPropertyName("ghostEatPoints")]
    public int GhostEatPoints { get; set; }

    [JsonPropertyName("ghostEatMinPoints")]
    public int GhostEatMinPoints { get; set; }

    [JsonPropertyName("ghostEatMaxPoints")]
    public int GhostEatMaxPoints { get; set; }

    [JsonPropertyName("hasGhostHouse")]
    public bool HasGhostHouse { get; set; }

    [JsonPropertyName("ghostSpawns")]
    public List<GridPoint> GhostSpawns { get; set; } = new();
}

public sealed class GridPoint
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }
}
