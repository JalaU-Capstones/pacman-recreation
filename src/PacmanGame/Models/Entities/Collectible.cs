using PacmanGame.Models.Enums;

namespace PacmanGame.Models.Entities;

/// <summary>
/// Represents a collectible item in the game (dot, power pellet, fruit)
/// </summary>
public class Collectible
{
    /// <summary>
    /// Unique identifier for the collectible
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// X position in the grid
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Y position in the grid
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Type of collectible
    /// </summary>
    public CollectibleType Type { get; set; }

    /// <summary>
    /// Points awarded for collecting
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    /// Is the collectible currently active (not yet collected)
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Is this collectible currently visible (for blinking power pellets)
    /// </summary>
    public bool IsVisible { get; set; }

    public Collectible(int id, int x, int y, CollectibleType type)
    {
        Id = id;
        X = x;
        Y = y;
        Type = type;
        Points = GetPointsForType(type);
        IsActive = true;
        IsVisible = true;
    }

    public Collectible(int x, int y, CollectibleType type) : this(y * 100 + x, x, y, type)
    {
    }

    /// <summary>
    /// Get the point value for each collectible type
    /// </summary>
    private static int GetPointsForType(CollectibleType type)
    {
        return type switch
        {
            CollectibleType.SmallDot => 10,
            CollectibleType.PowerPellet => 50,
            CollectibleType.Cherry => 100,
            CollectibleType.Strawberry => 300,
            CollectibleType.Orange => 500,
            CollectibleType.Apple => 700,
            CollectibleType.Melon => 1000,
            _ => 0
        };
    }

    /// <summary>
    /// Collect this item
    /// </summary>
    public void Collect()
    {
        IsActive = false;
    }

    /// <summary>
    /// Reset the collectible (for new level)
    /// </summary>
    public void Reset()
    {
        IsActive = true;
        IsVisible = true;
    }
}
