using System.Collections.Generic;
using PacmanGame.Models.Entities;
using PacmanGame.Models.Enums;

namespace PacmanGame.Services.Interfaces;

/// <summary>
/// Interface for detecting collisions between game entities
/// </summary>
public interface ICollisionDetector
{
    /// <summary>
    /// Check if Pac-Man collides with any ghost
    /// </summary>
    /// <param name="pacman">Pac-Man entity</param>
    /// <param name="ghosts">List of ghost entities</param>
    /// <returns>The ghost that Pac-Man collided with, or null</returns>
    Ghost? CheckPacmanGhostCollision(Pacman pacman, List<Ghost> ghosts);

    /// <summary>
    /// Check if Pac-Man collides with a collectible
    /// </summary>
    /// <param name="pacman">Pac-Man entity</param>
    /// <param name="collectibles">List of collectible items</param>
    /// <returns>The collectible that was collected, or null</returns>
    Collectible? CheckPacmanCollectibleCollision(Pacman pacman, List<Collectible> collectibles);

    /// <summary>
    /// Check if a position would collide with a wall
    /// </summary>
    /// <param name="row">Row position</param>
    /// <param name="col">Column position</param>
    /// <param name="map">The tile map</param>
    /// <returns>True if there's a wall at that position</returns>
    bool IsWallCollision(int row, int col, TileType[,] map);

    /// <summary>
    /// Check if an entity can move to a specific position
    /// </summary>
    /// <param name="entity">The entity trying to move</param>
    /// <param name="newRow">Target row</param>
    /// <param name="newCol">Target column</param>
    /// <param name="map">The tile map</param>
    /// <returns>True if the entity can move there</returns>
    bool CanMoveTo(Entity entity, int newRow, int newCol, TileType[,] map);

    /// <summary>
    /// Get the distance between two entities
    /// </summary>
    /// <param name="entity1">First entity</param>
    /// <param name="entity2">Second entity</param>
    /// <returns>Euclidean distance</returns>
    float GetDistance(Entity entity1, Entity entity2);

    /// <summary>
    /// Get the Manhattan distance between two positions
    /// </summary>
    /// <param name="row1">First row</param>
    /// <param name="col1">First column</param>
    /// <param name="row2">Second row</param>
    /// <param name="col2">Second column</param>
    /// <returns>Manhattan distance</returns>
    int GetManhattanDistance(int row1, int col1, int row2, int col2);
}
