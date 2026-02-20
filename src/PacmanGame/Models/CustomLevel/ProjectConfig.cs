using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PacmanGame.Models.CustomLevel;

public sealed class ProjectConfig
{
    [JsonPropertyName("projectName")]
    public string ProjectName { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }

    [JsonPropertyName("isEditable")]
    public bool IsEditable { get; set; } = true;

    [JsonPropertyName("globalConfig")]
    public GlobalConfig GlobalConfig { get; set; } = new();

    [JsonPropertyName("levelConfigs")]
    public List<LevelConfig> LevelConfigs { get; set; } = new();
}

public sealed class GlobalConfig
{
    [JsonPropertyName("lives")]
    public int Lives { get; set; }

    [JsonPropertyName("winScore")]
    public int WinScore { get; set; }

    [JsonPropertyName("levelCount")]
    public int LevelCount { get; set; }
}

public sealed class LevelConfig
{
    [JsonPropertyName("levelNumber")]
    public int LevelNumber { get; set; }

    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;

    [JsonPropertyName("pacmanSpeedMultiplier")]
    public double PacmanSpeedMultiplier { get; set; }

    [JsonPropertyName("ghostSpeedMultiplier")]
    public double GhostSpeedMultiplier { get; set; }

    [JsonPropertyName("frightenedDuration")]
    public int FrightenedDuration { get; set; }

    [JsonPropertyName("fruitPoints")]
    public int FruitPoints { get; set; }

    [JsonPropertyName("ghostEatPoints")]
    public int GhostEatPoints { get; set; }

    [JsonIgnore]
    public int MaxFrightenedDuration => Math.Max(1, 20 - ((LevelNumber - 1) * 2));

    [JsonIgnore]
    public int MaxFruitPoints => 5 + ((LevelNumber - 1) * 5);

    [JsonIgnore]
    public int MaxGhostEatPoints => 30 + ((LevelNumber - 1) * 15);
}
