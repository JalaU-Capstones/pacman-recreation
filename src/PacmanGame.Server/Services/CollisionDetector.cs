using System;
using PacmanGame.Server.Models;
using PacmanGame.Shared;

namespace PacmanGame.Server.Services
{
    public class CollisionDetector : ICollisionDetector
    {
        public bool CanMove(Entity entity, Direction direction, TileType[,] map)
        {
            int currentX = (int)Math.Round(entity.X);
            int currentY = (int)Math.Round(entity.Y);

            int nextX = currentX;
            int nextY = currentY;

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

            if (nextY < 0 || nextY >= map.GetLength(0) || nextX < 0 || nextX >= map.GetLength(1))
                return false;

            TileType tile = map[nextY, nextX];
            return tile != TileType.Wall && tile != TileType.GhostDoor;
        }

        public bool IsWall(float x, float y, TileType[,] map)
        {
            int gridX = (int)Math.Round(x);
            int gridY = (int)Math.Round(y);

            if (gridY < 0 || gridY >= map.GetLength(0) || gridX < 0 || gridX >= map.GetLength(1))
                return true; // Out of bounds is a wall

            return map[gridY, gridX] == TileType.Wall;
        }
    }
}
