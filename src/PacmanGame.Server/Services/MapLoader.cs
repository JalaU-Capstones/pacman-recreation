using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PacmanGame.Server.Models;
using PacmanGame.Shared;

namespace PacmanGame.Server.Services
{
    public class MapLoader : IMapLoader
    {
        private readonly ILogger _logger;

        public MapLoader(ILogger logger)
        {
            _logger = logger;
        }

        public TileType[,] LoadMap(string mapName)
        {
            // This is a simplified implementation. A real implementation would read from a file.
            _logger.LogInfo($"Loading map: {mapName}");
            return new TileType[28, 31];
        }

        public (int Row, int Col) GetPacmanSpawn(string mapName)
        {
            return (23, 13);
        }

        public List<(int Row, int Col)> GetGhostSpawns(string mapName)
        {
            return new List<(int, int)>
            {
                (11, 13), (14, 11), (14, 13), (14, 15)
            };
        }

        public List<Collectible> GetCollectibles(string mapName)
        {
            return new List<Collectible>();
        }
    }
}
