using System;

namespace PacmanGame.Models.CustomLevel;

public sealed class CustomLevelSummary
{
    public string Id { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public DateTime CreatedDate { get; init; }
    public int LevelCount { get; init; }
    public bool IsEditable { get; init; }
    public string DirectoryPath { get; init; } = string.Empty;
}
