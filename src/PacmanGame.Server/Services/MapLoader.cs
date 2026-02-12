using Microsoft.Extensions.Logging;
using PacmanGame.Server.Models;

namespace PacmanGame.Server.Services
{
    public class MapLoader : IMapLoader
    {
        private readonly ILogger<MapLoader> _logger;

        public MapLoader(ILogger<MapLoader> logger)
        {
            _logger = logger;
            _logger.LogInformation("MapLoader initialized successfully");
        }

        public TileType[,] LoadMap(string mapName)
        {
            // This is a simplified implementation. A real implementation would read from a file.
            _logger.LogInformation($"Loading map: {mapName}");
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
