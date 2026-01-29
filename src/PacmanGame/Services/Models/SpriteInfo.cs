using System.Collections.Generic;

namespace PacmanGame.Services.Models;

/// <summary>
/// Information about a sprite's location in a sprite sheet
/// </summary>
public class SpriteInfo
{
    /// <summary>
    /// X coordinate of the sprite in the sheet
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Y coordinate of the sprite in the sheet
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Width of the sprite
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Height of the sprite
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Name/identifier of the sprite
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Container for sprite sheet metadata loaded from JSON
/// </summary>
public class SpriteSheet
{
    /// <summary>
    /// Dictionary mapping sprite names to their coordinates
    /// </summary>
    public Dictionary<string, SpriteInfo> Sprites { get; set; } = new();
}
