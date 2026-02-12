using System.Collections.Generic;
using PacmanGame.Server.Models;

namespace PacmanGame.Server.Services
{
    public interface IMapLoader
    {
        TileType[,] LoadMap(string mapName);
        (int Row, int Col) GetPacmanSpawn(string mapName);
        List<(int Row, int Col)> GetGhostSpawns(string mapName);
        List<Collectible> GetCollectibles(string mapName);
    }
}
