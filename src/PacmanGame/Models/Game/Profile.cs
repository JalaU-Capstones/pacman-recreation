using System;

namespace PacmanGame.Models.Game;

/// <summary>
/// Represents a user profile.
/// </summary>
public class Profile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AvatarColor { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastPlayedAt { get; set; }

    // New fields for global leaderboard
    public bool HasCompletedAllLevels { get; set; }
    public string? GlobalProfileId { get; set; }
    public long LastGlobalScoreSubmission { get; set; }

    // Not stored in DB, but useful for UI
    public int HighScore { get; set; }
}
