using System;
using PacmanGame.Models.Enums;
using PacmanGame.Helpers;
using PacmanGame.Services.Interfaces;

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

    /// <summary>
    /// Time remaining before respawning after returning to house (seconds). Positive when countdown is active.
    /// </summary>
    public float RespawnTimer { get; set; }

    /// <summary>
    /// Time until this ghost is released from the ghost house (seconds). Positive when in-house.
    /// </summary>
    public float ReleaseTimer { get; set; }

    public Ghost(int x, int y) : base(x, y)
    {
        Type = GhostType.Blinky; // Default, will be set by GameEngine
        State = GhostState.InHouse; // Start in house
        SpawnX = x;
        SpawnY = y;
        VulnerableTime = 0f;
        AnimationFrame = 0;
        RespawnTimer = 0f;
        ReleaseTimer = 0f; // Will be set by GameEngine
    }

    public Ghost(int x, int y, GhostType type) : base(x, y)
    {
        Type = type;
        State = GhostState.InHouse; // Start in house
        SpawnX = x;
        SpawnY = y;
        VulnerableTime = 0f;
        AnimationFrame = 0;
        RespawnTimer = 0f;
        ReleaseTimer = 0f; // Will be set by GameEngine
    }

    /// <summary>
    /// Gets the current speed of the ghost based on its state.
    /// </summary>
    public float GetSpeed()
    {
        return State switch
        {
            GhostState.Vulnerable or GhostState.Warning => Constants.GhostVulnerableSpeed,
            GhostState.Eaten => Constants.GhostEatenSpeed,
            _ => Type switch
            {
                GhostType.Blinky => Constants.GhostNormalSpeed * 1.0f,
                GhostType.Pinky => Constants.GhostNormalSpeed * 0.95f,
                GhostType.Inky => Constants.GhostNormalSpeed * 0.95f,
                GhostType.Clyde => Constants.GhostNormalSpeed * 0.90f,
                _ => Constants.GhostNormalSpeed
            }
        };
    }

    /// <summary>
    /// Make the ghost vulnerable (after power pellet)
    /// </summary>
    public void MakeVulnerable(float duration, ILogger logger)
    {
        if (State != GhostState.Eaten && State != GhostState.InHouse && State != GhostState.ExitingHouse)
        {
            logger.Debug($"MakeVulnerable called for {GetName()} at ({X},{Y}). PrevState={State} Duration={duration}");
            State = GhostState.Vulnerable;
            VulnerableTime = duration;
        }
    }

    /// <summary>
    /// Update vulnerability timer
    /// </summary>
    public void UpdateVulnerability(float deltaTime, ILogger logger)
    {
        if (State == GhostState.Vulnerable || State == GhostState.Warning)
        {
            VulnerableTime -= deltaTime;

            // Start warning when approaching the warning threshold
            if (VulnerableTime <= Constants.PowerPelletWarningTime && State == GhostState.Vulnerable)
            {
                logger.Debug($"{GetName()} entering WARNING at ({X},{Y})");
                State = GhostState.Warning;
            }

            // Return to normal when time runs out
            if (VulnerableTime <= 0)
            {
                logger.Debug($"{GetName()} vulnerability ended at ({X},{Y})");
                State = GhostState.Normal;
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
        RespawnTimer = Constants.GhostRespawnTime; // Set respawn timer
    }

    /// <summary>
    /// Respawn the ghost at its starting position
    /// </summary>
    public void Respawn(ILogger logger)
    {
        ExactX = SpawnX;
        ExactY = SpawnY;
        X = SpawnX;
        Y = SpawnY;
        State = GhostState.Normal;
        CurrentDirection = Direction.Up; // Ghosts start facing up
        VulnerableTime = 0f;
        RespawnTimer = 0f;
        logger.Debug($"{GetName()} respawned at ({X},{Y}) State={State}");
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

        // Wrap tunnels (allow moving through left/right edges)
        int mapHeight = map.GetLength(0);
        int mapWidth = map.GetLength(1);

        if (nextX < 0) nextX = mapWidth - 1;
        else if (nextX >= mapWidth) nextX = 0;
        if (nextY < 0) nextY = mapHeight - 1;
        else if (nextY >= mapHeight) nextY = 0;

        // Check if it's a walkable tile
        TileType tile = map[nextY, nextX];

        // Ghosts can pass through ghost doors, eaten ghosts can enter ghost house
        if (tile == TileType.GhostDoor)
        {
            // Only eaten ghosts can enter the house freely
            // Other states can exit, but not re-enter from outside the house
            return State == GhostState.Eaten || State == GhostState.ExitingHouse || State == GhostState.InHouse;
        }

        // Normal movement rules
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
