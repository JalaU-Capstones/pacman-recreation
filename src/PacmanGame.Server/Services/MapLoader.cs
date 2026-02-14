using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using PacmanGame.Server.Models;
using PacmanGame.Shared;

namespace PacmanGame.Server.Services
{
    public class MapLoader : IMapLoader
    {
        private readonly ILogger<MapLoader> _logger;
        private readonly string _mapsPath;

        // Constants for map parsing (must match client)
        private const char WallChar = '#';
        private const char SmallDotChar = '.';
        private const char PowerPelletChar = 'o';
        private const char PacmanChar = 'P';
        private const char GhostChar = 'G';
        private const char GhostDoorChar = '-';
        private const char FruitChar = 'F';

        private const int MapWidth = 28;
        private const int MapHeight = 31;

        public MapLoader(ILogger<MapLoader> logger)
        {
            _logger = logger;
            // Use AppDomain.CurrentDomain.BaseDirectory to ensure we look in the output directory
            _mapsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Maps");
            _logger.LogInformation($"MapLoader initialized. Maps path: {_mapsPath}");
        }

        public TileType[,] LoadMap(string mapName)
        {
            string filePath = Path.Combine(_mapsPath, mapName);

            if (!File.Exists(filePath))
            {
                _logger.LogError($"Map file not found: {filePath}");
                // Fallback to empty map to prevent crash, but log error
                return new TileType[MapHeight, MapWidth];
            }

            string[] lines = File.ReadAllLines(filePath);
            TileType[,] tiles = new TileType[MapHeight, MapWidth];

            for (int row = 0; row < Math.Min(lines.Length, MapHeight); row++)
            {
                string line = lines[row];
                for (int col = 0; col < Math.Min(line.Length, MapWidth); col++)
                {
                    tiles[row, col] = CharToTileType(line[col]);
                }
            }

            return tiles;
        }

        public (int Row, int Col) GetPacmanSpawn(string mapName)
        {
            string filePath = Path.Combine(_mapsPath, mapName);
            if (!File.Exists(filePath)) return (23, 13); // Default fallback

            string[] lines = File.ReadAllLines(filePath);
            for (int row = 0; row < lines.Length; row++)
            {
                int col = lines[row].IndexOf(PacmanChar);
                if (col >= 0) return (row, col);
            }
            return (23, 13);
        }

        public List<(int Row, int Col)> GetGhostSpawns(string mapName)
        {
            var spawns = new List<(int, int)>();
            string filePath = Path.Combine(_mapsPath, mapName);

            if (!File.Exists(filePath))
            {
                // Default fallbacks
                return new List<(int, int)> { (11, 13), (14, 11), (14, 13), (14, 15) };
            }

            string[] lines = File.ReadAllLines(filePath);
            for (int row = 0; row < lines.Length; row++)
            {
                for (int col = 0; col < lines[row].Length; col++)
                {
                    if (lines[row][col] == GhostChar)
                    {
                        spawns.Add((row, col));
                    }
                }
            }
            return spawns;
        }

        public List<Collectible> GetCollectibles(string mapName)
        {
            var collectibles = new List<Collectible>();
            string filePath = Path.Combine(_mapsPath, mapName);

            if (!File.Exists(filePath)) return collectibles;

            string[] lines = File.ReadAllLines(filePath);

            for (int row = 0; row < lines.Length; row++)
            {
                for (int col = 0; col < lines[row].Length; col++)
                {
                    char c = lines[row][col];
                    CollectibleType? type = CharToCollectibleType(c);

                    if (type.HasValue)
                    {
                        // Use (row * 100 + col) to match client ID generation
                        int id = row * 100 + col;
                        collectibles.Add(new Collectible(id, row, col, type.Value));
                    }
                }
            }
            return collectibles;
        }

        private TileType CharToTileType(char c)
        {
            return c switch
            {
                WallChar => TileType.Wall,
                GhostDoorChar => TileType.GhostDoor,
                _ => TileType.Empty
            };
        }

        private CollectibleType? CharToCollectibleType(char c)
        {
            return c switch
            {
                SmallDotChar => CollectibleType.SmallDot,
                PowerPelletChar => CollectibleType.PowerPellet,
                FruitChar => CollectibleType.Cherry,
                _ => null
            };
        }
    }
}
