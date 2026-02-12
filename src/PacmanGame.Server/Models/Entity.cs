using PacmanGame.Shared;

namespace PacmanGame.Server.Models
{
    public abstract class Entity
    {
        public float X { get; set; }
        public float Y { get; set; }
        public Direction CurrentDirection { get; set; } = Direction.None;
    }
}
