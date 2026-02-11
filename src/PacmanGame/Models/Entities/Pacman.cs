using PacmanGame.Models.Enums;
using PacmanGame.Helpers;

namespace PacmanGame.Models.Entities;

/// <summary>
/// Represents the player-controlled Pac-Man character
/// </summary>
public class Pacman : Entity
{
    /// <summary>
    /// Current animation frame
    /// </summary>
    public int AnimationFrame { get; set; }

    /// <summary>
    /// Is Pac-Man currently invulnerable (after eating power pellet)
    /// </summary>
    public bool IsInvulnerable { get; set; }

    /// <summary>
    /// Time remaining for invulnerability (in seconds)
    /// </summary>
    public float InvulnerabilityTime { get; set; }

    /// <summary>
    /// Is Pac-Man in death animation
    /// </summary>
    public bool IsDying { get; set; }

    /// <summary>
    /// Duration of the power pellet effect for the current level
    /// </summary>
    public float PowerPelletDuration { get; set; }

    public Pacman(int x, int y) : base(x, y)
    {
        Speed = Constants.PacmanSpeed;
        AnimationFrame = 0;
        IsInvulnerable = false;
        InvulnerabilityTime = 0f;
        IsDying = false;
        PowerPelletDuration = Constants.Level1PowerPelletDuration;
    }

    /// <summary>
    /// Activate power pellet effect
    /// </summary>
    public void ActivatePowerPellet()
    {
        IsInvulnerable = true;
        InvulnerabilityTime = PowerPelletDuration;
    }

    /// <summary>
    /// Update invulnerability timer
    /// </summary>
    public void UpdateInvulnerability(float deltaTime)
    {
        if (IsInvulnerable)
        {
            InvulnerabilityTime -= deltaTime;
            if (InvulnerabilityTime <= 0)
            {
                IsInvulnerable = false;
                InvulnerabilityTime = 0f;
            }
        }
    }

    /// <summary>
    /// Check if Pac-Man can move in the specified direction
    /// </summary>
    public override bool CanMove(Direction direction, TileType[,] map)
    {
        int nextX = X;
        int nextY = Y;

        switch (direction)
        {
            case Direction.Up:
                nextY--;
                break;
            case Direction.Down:
                nextY++;
                break;
            case Direction.Left:
                nextX--;
                break;
            case Direction.Right:
                nextX++;
                break;
            default:
                return false;
        }

        // Check bounds
        if (nextY < 0 || nextY >= map.GetLength(0) || nextX < 0 || nextX >= map.GetLength(1))
            return false;

        // Check if it's a walkable tile
        TileType tile = map[nextY, nextX];
        return tile != TileType.Wall && tile != TileType.GhostDoor;
    }
}
