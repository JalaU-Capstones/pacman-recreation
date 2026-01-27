namespace PacmanGame.Models.Enums;

/// <summary>
/// Types of collectible items in the game
/// </summary>
public enum CollectibleType
{
    None = 0,
    SmallDot = 1,      // Regular dot (+10 points)
    PowerPellet = 2,   // Power pellet (+50 points, makes ghosts vulnerable)
    Cherry = 3,        // Fruit bonus (+100 points)
    Strawberry = 4,    // Fruit bonus (+300 points)
    Orange = 5,        // Fruit bonus (+500 points)
    Apple = 6,         // Fruit bonus (+700 points)
    Melon = 7          // Fruit bonus (+1000 points)
}

/// <summary>
/// Types of tiles in the maze
/// </summary>
public enum TileType
{
    Empty = 0,           // Walkable empty space
    Wall = 1,            // Solid wall
    GhostDoor = 2,       // Ghost house door (only ghosts can pass)
    TeleportLeft = 3,    // Left side of tunnel
    TeleportRight = 4    // Right side of tunnel
}
