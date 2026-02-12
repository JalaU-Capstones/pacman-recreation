using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using PacmanGame.Helpers;
using PacmanGame.Models.Enums;
using PacmanGame.Services.Interfaces;

namespace PacmanGame.Services;

/// <summary>
/// Service for loading and parsing game maps from text files.
/// Maps are stored in Assets/Maps/ as .txt files with character-based representation.
/// </summary>
public class MapLoader : IMapLoader
{
    private readonly ILogger<MapLoader> _logger;
    private readonly string _mapsPath;

    public MapLoader(ILogger<MapLoader> logger)
    {
        _logger = logger;
        _mapsPath = Path.Combine(AppContext.BaseDirectory, Constants.MapsPath);
    }

    /// <summary>
    /// Load a map from a text file and convert to TileType array
    /// </summary>
    public TileType[,] LoadMap(string fileName)
    {
        string filePath = Path.Combine(_mapsPath, fileName);

        if (!File.Exists(filePath))
        {
            _logger.LogError($"Map file not found: {filePath}");
            throw new FileNotFoundException($"Map file not found: {filePath}");
        }

        _logger.LogInformation($"Loading map: {fileName}");
        // Read all lines from the file
        string[] lines = File.ReadAllLines(filePath);

        if (lines.Length != Constants.MapHeight)
        {
            var ex = new InvalidOperationException(
                $"Map height mismatch. Expected {Constants.MapHeight}, got {lines.Length}");
            _logger.LogError(ex, $"Error loading map {fileName}");
            throw ex;
        }

        // Create the tile array
        TileType[,] tiles = new TileType[Constants.MapHeight, Constants.MapWidth];

        // Parse each line
        for (int row = 0; row < lines.Length; row++)
        {
            string line = lines[row];

            if (line.Length > Constants.MapWidth)
            {
                var ex = new InvalidOperationException(
                    $"Map width mismatch on line {row + 1}. Expected max {Constants.MapWidth}, got {line.Length}");
                _logger.LogError(ex, $"Error loading map {fileName}");
                throw ex;
            }

            for (int col = 0; col < Constants.MapWidth; col++)
            {
                char c = col < line.Length ? line[col] : ' ';
                tiles[row, col] = CharToTileType(c);
            }
        }

        return tiles;
    }

    /// <summary>
    /// Get Pac-Man's starting position from the map
    /// </summary>
    public (int Row, int Col) GetPacmanSpawn(string fileName)
    {
        string filePath = Path.Combine(_mapsPath, fileName);
        string[] lines = File.ReadAllLines(filePath);

        for (int row = 0; row < lines.Length; row++)
        {
            int col = lines[row].IndexOf(Constants.PacmanChar);
            if (col >= 0)
            {
                return (row, col);
            }
        }

        var ex = new InvalidOperationException($"Pac-Man spawn point '{Constants.PacmanChar}' not found in map {fileName}");
        _logger.LogError(ex, $"Error finding Pac-Man spawn in {fileName}");
        throw ex;
    }

    /// <summary>
    /// Get all ghost spawn positions from the map
    /// </summary>
    public List<(int Row, int Col)> GetGhostSpawns(string fileName)
    {
        string filePath = Path.Combine(_mapsPath, fileName);
        string[] lines = File.ReadAllLines(filePath);

        var spawns = new List<(int Row, int Col)>();

        for (int row = 0; row < lines.Length; row++)
        {
            for (int col = 0; col < lines[row].Length; col++)
            {
                if (lines[row][col] == Constants.GhostChar)
                {
                    spawns.Add((row, col));
                }
            }
        }

        if (spawns.Count == 0)
        {
            var ex = new InvalidOperationException($"No ghost spawn points '{Constants.GhostChar}' found in map {fileName}");
            _logger.LogError(ex, $"Error finding ghost spawns in {fileName}");
            throw ex;
        }

        return spawns;
    }

    /// <summary>
    /// Get all collectible positions and types from the map
    /// </summary>
    public List<(int Row, int Col, CollectibleType Type)> GetCollectibles(string fileName)
    {
        string filePath = Path.Combine(_mapsPath, fileName);
        string[] lines = File.ReadAllLines(filePath);

        var collectibles = new List<(int Row, int Col, CollectibleType Type)>();

        for (int row = 0; row < lines.Length; row++)
        {
            for (int col = 0; col < lines[row].Length; col++)
            {
                char c = lines[row][col];
                CollectibleType? type = CharToCollectibleType(c);

                if (type.HasValue)
                {
                    collectibles.Add((row, col, type.Value));
                }
            }
        }

        return collectibles;
    }

    /// <summary>
    /// Count total dots in the map (for win condition)
    /// </summary>
    public int CountDots(string fileName)
    {
        string filePath = Path.Combine(_mapsPath, fileName);
        string[] lines = File.ReadAllLines(filePath);

        int count = 0;

        foreach (string line in lines)
        {
            foreach (char c in line)
            {
                if (c == Constants.SmallDotChar || c == Constants.PowerPelletChar)
                {
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Convert a map character to a TileType
    /// </summary>
    private TileType CharToTileType(char c)
    {
        return c switch
        {
            Constants.WallChar => TileType.Wall,
            Constants.GhostDoorChar => TileType.GhostDoor,
            // Everything else is empty (dots, pellets, entities are not tiles)
            _ => TileType.Empty
        };
    }

    /// <summary>
    /// Convert a map character to a CollectibleType (if applicable)
    /// </summary>
    private CollectibleType? CharToCollectibleType(char c)
    {
        return c switch
        {
            Constants.SmallDotChar => CollectibleType.SmallDot,
            Constants.PowerPelletChar => CollectibleType.PowerPellet,
            Constants.FruitChar => CollectibleType.Cherry, // Default fruit
            _ => null
        };
    }
}
