using PacmanGame.Models.Enums;

namespace PacmanGame.Models.Entities;

/// <summary>
/// Represents a ghost enemy with AI behavior
/// </summary>
public class Ghost : Entity
{
    /// <summary>
    /// Type of ghost (determines AI behavior)
    /// </summary>
    public GhostType Type { get; set; }

    /// <summary>
    /// Current state of the ghost
    /// </summary>
    public GhostState State { get; set; }

    /// <summary>
    /// Initial spawn position X (for respawning)
    /// </summary>
    public int SpawnX { get; set; }

    /// <summary>
    /// Initial spawn position Y (for respawning)
    /// </summary>
    public int SpawnY { get; set; }

    /// <summary>
    /// Time remaining in vulnerable state (in seconds)
    /// </summary>
    public float VulnerableTime { get; set; }

    /// <summary>
    /// Current animation frame
    /// </summary>
    public int AnimationFrame { get; set; }

    public Ghost(int x, int y, GhostType type) : base(x, y)
    {
        Type = type;
        State = GhostState.Normal;
        SpawnX = x;
        SpawnY = y;
        Speed = GetSpeedForType(type);
        VulnerableTime = 0f;
        AnimationFrame = 0;
    }

    /// <summary>
    /// Get the appropriate speed for each ghost type
    /// </summary>
    private static float GetSpeedForType(GhostType type)
    {
        return type switch
        {
            GhostType.Blinky => 3.8f,  // Slightly slower than Pac-Man
            GhostType.Pinky => 3.7f,
            GhostType.Inky => 3.6f,
            GhostType.Clyde => 3.5f,
            _ => 3.5f
        };
    }

    /// <summary>
    /// Make the ghost vulnerable (after power pellet)
    /// </summary>
    public void MakeVulnerable(float duration = 6.0f)
    {
        if (State != GhostState.Eaten)
        {
            State = GhostState.Vulnerable;
            VulnerableTime = duration;
            Speed = 2.0f; // Vulnerable ghosts are slower
        }
    }

    /// <summary>
    /// Update vulnerability timer
    /// </summary>
    public void UpdateVulnerability(float deltaTime)
    {
        if (State == GhostState.Vulnerable || State == GhostState.Warning)
        {
            VulnerableTime -= deltaTime;

            // Start warning when 2 seconds remain
            if (VulnerableTime <= 2.0f && State == GhostState.Vulnerable)
            {
                State = GhostState.Warning;
            }

            // Return to normal when time runs out
            if (VulnerableTime <= 0)
            {
                State = GhostState.Normal;
                Speed = GetSpeedForType(Type);
                VulnerableTime = 0f;
            }
        }
    }

    /// <summary>
    /// Mark ghost as eaten
    /// </summary>
    public void GetEaten()
    {
        State = GhostState.Eaten;
        Speed = 6.0f; // Eaten ghosts return to base quickly
    }

    /// <summary>
    /// Respawn the ghost at its starting position
    /// </summary>
    public void Respawn()
    {
        X = SpawnX;
        Y = SpawnY;
        State = GhostState.Normal;
        CurrentDirection = Direction.Up; // Ghosts start facing up
        Speed = GetSpeedForType(Type);
        VulnerableTime = 0f;
    }

    /// <summary>
    /// Check if ghost can move in the specified direction
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
        
        // Ghosts can pass through ghost doors, eaten ghosts can enter ghost house
        if (tile == TileType.GhostDoor)
            return State == GhostState.Eaten; // Only eaten ghosts can enter
        
        return tile == TileType.Empty || tile == TileType.TeleportLeft || tile == TileType.TeleportRight;
    }

    /// <summary>
    /// Get the name of the ghost
    /// </summary>
    public string GetName()
    {
        return Type switch
        {
            GhostType.Blinky => "Blinky",
            GhostType.Pinky => "Pinky",
            GhostType.Inky => "Inky",
            GhostType.Clyde => "Clyde",
            _ => "Ghost"
        };
    }
}
