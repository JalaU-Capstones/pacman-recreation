using Avalonia.Media.Imaging;

namespace PacmanGame.Services.Interfaces;

/// <summary>
/// Interface for managing sprite loading and retrieval
/// </summary>
public interface ISpriteManager
{
    /// <summary>
    /// Initialize and load all sprite sheets
    /// </summary>
    void Initialize();

    /// <summary>
    /// Get a Pac-Man sprite
    /// </summary>
    /// <param name="direction">Direction Pac-Man is facing</param>
    /// <param name="frame">Animation frame (0-2)</param>
    /// <returns>Cropped sprite bitmap</returns>
    CroppedBitmap? GetPacmanSprite(string direction, int frame);

    /// <summary>
    /// Get a ghost sprite
    /// </summary>
    /// <param name="ghostType">Type of ghost (blinky, pinky, etc.)</param>
    /// <param name="direction">Direction ghost is facing</param>
    /// <param name="frame">Animation frame (0-1)</param>
    /// <returns>Cropped sprite bitmap</returns>
    CroppedBitmap? GetGhostSprite(string ghostType, string direction, int frame);

    /// <summary>
    /// Get a vulnerable ghost sprite
    /// </summary>
    /// <param name="frame">Animation frame (0-1)</param>
    /// <returns>Cropped sprite bitmap</returns>
    CroppedBitmap? GetVulnerableGhostSprite(int frame);

    /// <summary>
    /// Get a warning (flashing) ghost sprite
    /// </summary>
    /// <param name="frame">Animation frame (0-1)</param>
    /// <returns>Cropped sprite bitmap</returns>
    CroppedBitmap? GetWarningGhostSprite(int frame);

    /// <summary>
    /// Get ghost eyes sprite (when eaten)
    /// </summary>
    /// <param name="direction">Direction eyes are facing</param>
    /// <returns>Cropped sprite bitmap</returns>
    CroppedBitmap? GetGhostEyesSprite(string direction);

    /// <summary>
    /// Get a collectible item sprite
    /// </summary>
    /// <param name="itemType">Type of item (dot, pellet, fruit)</param>
    /// <param name="frame">Animation frame (for animated items)</param>
    /// <returns>Cropped sprite bitmap</returns>
    CroppedBitmap? GetItemSprite(string itemType, int frame = 0);

    /// <summary>
    /// Get a tile sprite
    /// </summary>
    /// <param name="tileType">Type of tile</param>
    /// <returns>Cropped sprite bitmap</returns>
    CroppedBitmap? GetTileSprite(string tileType);

    /// <summary>
    /// Get Pac-Man death animation sprite
    /// </summary>
    /// <param name="frame">Death animation frame (0-10)</param>
    /// <returns>Cropped sprite bitmap</returns>
    CroppedBitmap? GetDeathSprite(int frame);
}
