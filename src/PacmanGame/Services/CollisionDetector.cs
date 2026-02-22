using System;
using System.Collections.Generic;
using System.Linq;
using PacmanGame.Models.Entities;
using PacmanGame.Models.Enums;
using PacmanGame.Services.Interfaces;

namespace PacmanGame.Services;

/// <summary>
/// Service for detecting collisions between game entities.
/// Uses grid-based collision detection.
/// </summary>
public class CollisionDetector : ICollisionDetector
{
    private const float CollisionThreshold = 0.5f; // Collision distance threshold

    /// <summary>
    /// Check if Pac-Man collides with any ghost
    /// </summary>
    public Ghost? CheckPacmanGhostCollision(Pacman pacman, List<Ghost> ghosts)
    {
        foreach (var ghost in ghosts)
        {
            // Eyes returning to the ghost house should not block collisions with other ghosts
            // and should not be treated as a hittable entity.
            if (ghost.State == GhostState.Eaten)
            {
                continue;
            }

            // Check if they're in the same grid position or very close
            if (pacman.X == ghost.X && pacman.Y == ghost.Y)
            {
                return ghost;
            }

            // Also check if they're within collision threshold
            float distance = GetDistance(pacman, ghost);
            if (distance < CollisionThreshold)
            {
                return ghost;
            }
        }

        return null;
    }

    /// <summary>
    /// Check if Pac-Man collides with a collectible
    /// </summary>
    public Collectible? CheckPacmanCollectibleCollision(Pacman pacman, List<Collectible> collectibles)
    {
        // Find active collectibles at Pac-Man's position
        return collectibles.FirstOrDefault(c => 
            c.IsActive && 
            c.X == pacman.X && 
            c.Y == pacman.Y);
    }

    /// <summary>
    /// Check if a position would collide with a wall
    /// </summary>
    public bool IsWallCollision(int row, int col, TileType[,] map)
    {
        // Check bounds
        if (row < 0 || row >= map.GetLength(0) || 
            col < 0 || col >= map.GetLength(1))
        {
            return true; // Out of bounds counts as wall
        }

        // Check if it's a wall tile
        return map[row, col] == TileType.Wall;
    }

    /// <summary>
    /// Check if an entity can move to a specific position
    /// </summary>
    public bool CanMoveTo(Entity entity, int newRow, int newCol, TileType[,] map)
    {
        // Check bounds
        if (newRow < 0 || newRow >= map.GetLength(0) || 
            newCol < 0 || newCol >= map.GetLength(1))
        {
            return false;
        }

        TileType tile = map[newRow, newCol];

        // Walls block everyone
        if (tile == TileType.Wall)
        {
            return false;
        }

        // Ghost doors are special
        if (tile == TileType.GhostDoor)
        {
            // Only eaten ghosts can pass through ghost doors
            if (entity is Ghost ghost)
            {
                return ghost.State == GhostState.Eaten;
            }
            // Pac-Man cannot pass through ghost doors
            return false;
        }

        // Empty tiles and teleport tiles are walkable
        return true;
    }

    /// <summary>
    /// Get the Euclidean distance between two entities
    /// </summary>
    public float GetDistance(Entity entity1, Entity entity2)
    {
        int dx = entity1.X - entity2.X;
        int dy = entity1.Y - entity2.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Get the Manhattan distance between two positions
    /// </summary>
    public int GetManhattanDistance(int row1, int col1, int row2, int col2)
    {
        return Math.Abs(row1 - row2) + Math.Abs(col1 - col2);
    }
}
