using PacmanGame.Server.Models;
using PacmanGame.Shared;

namespace PacmanGame.Server.Services
{
    public interface ICollisionDetector
    {
        bool CanMove(Entity entity, Direction direction, TileType[,] map);
    }
}
