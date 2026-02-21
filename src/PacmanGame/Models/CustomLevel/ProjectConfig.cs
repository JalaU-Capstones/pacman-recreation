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
    public const double MinSpeedMultiplier = 0.5;
    public const double MaxSpeedMultiplier = 2.0;

    [JsonPropertyName("levelNumber")]
    public int LevelNumber { get; set; }

    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;

    [JsonPropertyName("pacmanSpeedMultiplier")]
    public double PacmanSpeedMultiplier
    {
        get => _pacmanSpeedMultiplier;
        set => _pacmanSpeedMultiplier = Math.Clamp(value, MinSpeedMultiplier, MaxSpeedMultiplier);
    }

    [JsonPropertyName("ghostSpeedMultiplier")]
    public double GhostSpeedMultiplier
    {
        get => _ghostSpeedMultiplier;
        set => _ghostSpeedMultiplier = Math.Clamp(value, MinSpeedMultiplier, MaxSpeedMultiplier);
    }

    [JsonPropertyName("frightenedDuration")]
    public int FrightenedDuration
    {
        get => _frightenedDuration;
        set => _frightenedDuration = Math.Clamp(value, 1, MaxFrightenedDuration);
    }

    [JsonPropertyName("fruitPoints")]
    public int FruitPoints
    {
        get => _fruitPoints;
        set => _fruitPoints = Math.Clamp(value, 1, MaxFruitPoints);
    }

    [JsonPropertyName("ghostEatPoints")]
    public int GhostEatPoints
    {
        get => _ghostEatPoints;
        set => _ghostEatPoints = Math.Clamp(value, 10, MaxGhostEatPoints);
    }

    [JsonIgnore]
    public int MaxFrightenedDuration => Math.Max(1, 20 - ((LevelNumber - 1) * 2));

    [JsonIgnore]
    public int MaxFruitPoints => 5 + ((LevelNumber - 1) * 5);

    [JsonIgnore]
    public int MaxGhostEatPoints => 30 + ((LevelNumber - 1) * 15);

    [JsonIgnore]
    public double MinAllowedSpeedMultiplier => MinSpeedMultiplier;

    [JsonIgnore]
    public double MaxAllowedSpeedMultiplier => MaxSpeedMultiplier;

    private double _pacmanSpeedMultiplier = 1.0;
    private double _ghostSpeedMultiplier = 0.9;
    private int _frightenedDuration = 10;
    private int _fruitPoints = 5;
    private int _ghostEatPoints = 30;
}
