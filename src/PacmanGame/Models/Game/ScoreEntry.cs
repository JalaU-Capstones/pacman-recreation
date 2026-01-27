using System;

namespace PacmanGame.Models.Game;

/// <summary>
/// Represents a single entry in the high score table
/// </summary>
public class ScoreEntry
{
    /// <summary>
    /// Rank position (1st, 2nd, 3rd, etc.)
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Player name/initials (3 characters)
    /// </summary>
    public string PlayerName { get; set; } = "AAA";

    /// <summary>
    /// Score achieved
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Date the score was achieved
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Level reached
    /// </summary>
    public int Level { get; set; }

    public ScoreEntry()
    {
        Date = DateTime.Now;
    }

    public override string ToString()
    {
        return $"{Rank}. {PlayerName} - {Score:N0} pts (Level {Level})";
    }
}
