using PacmanGame.Models.Enums;

namespace PacmanGame.Models.Entities;

/// <summary>
/// Base class for all game entities (Pac-Man, Ghosts)
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// X position in the grid (column)
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Y position in the grid (row)
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Current movement direction
    /// </summary>
    public Direction CurrentDirection { get; set; }

    /// <summary>
    /// Desired next direction (for smooth turning)
    /// </summary>
    public Direction NextDirection { get; set; }

    /// <summary>
    /// Movement speed (tiles per second)
    /// </summary>
    public float Speed { get; set; }

    /// <summary>
    /// Is the entity currently moving
    /// </summary>
    public bool IsMoving { get; set; }

    protected Entity(int x, int y)
    {
        X = x;
        Y = y;
        CurrentDirection = Direction.None;
        NextDirection = Direction.None;
        Speed = 1.0f;
        IsMoving = false;
    }

    /// <summary>
    /// Check if entity can move in the specified direction
    /// </summary>
    public abstract bool CanMove(Direction direction, TileType[,] map);
}
