using PacmanGame.Shared;

namespace PacmanGame.Server.Models
{
    public class Collectible : Entity
    {
        public int Id { get; }
        public CollectibleType Type { get; }
        public bool IsActive { get; set; } = true;

        public Collectible(int id, int row, int col, CollectibleType type)
        {
            Id = id;
            Y = row;
            X = col;
            Type = type;
        }
    }
}
