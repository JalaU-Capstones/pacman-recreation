using System.Collections.Generic;
using PacmanGame.Models.Enums;

namespace PacmanGame.Services.Interfaces;

/// <summary>
/// Interface for loading and parsing game maps from text files
/// </summary>
public interface IMapLoader
{
    /// <summary>
    /// Load a map from a text file
    /// </summary>
    /// <param name="fileName">Name of the map file (e.g., "level1.txt")</param>
    /// <returns>2D array representing the map tiles</returns>
    TileType[,] LoadMap(string fileName);

    /// <summary>
    /// Get Pac-Man's starting position from the map
    /// </summary>
    /// <param name="fileName">Name of the map file</param>
    /// <returns>Tuple with (row, column) position</returns>
    (int Row, int Col) GetPacmanSpawn(string fileName);

    /// <summary>
    /// Get all ghost spawn positions from the map
    /// </summary>
    /// <param name="fileName">Name of the map file</param>
    /// <returns>List of (row, column) positions</returns>
    List<(int Row, int Col)> GetGhostSpawns(string fileName);

    /// <summary>
    /// Get all collectible positions from the map
    /// </summary>
    /// <param name="fileName">Name of the map file</param>
    /// <returns>List of (row, column, type) tuples</returns>
    List<(int Row, int Col, CollectibleType Type)> GetCollectibles(string fileName);

    /// <summary>
    /// Count total dots in the map (for win condition)
    /// </summary>
    /// <param name="fileName">Name of the map file</param>
    /// <returns>Total number of dots (small dots + power pellets)</returns>
    int CountDots(string fileName);
}
