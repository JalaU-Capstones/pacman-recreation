namespace PacmanGame.Models.Game;

/// <summary>
/// Represents user-specific settings.
/// </summary>
public class Settings
{
    public int ProfileId { get; set; }
    public double MenuMusicVolume { get; set; } = 0.7;
    public double GameMusicVolume { get; set; } = 0.7;
    public double SfxVolume { get; set; } = 0.8;
    public bool IsMuted { get; set; } = false;
}
