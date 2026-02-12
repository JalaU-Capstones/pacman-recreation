namespace PacmanGame.Server.Models
{
    public class Collectible : Entity
    {
        public int Id { get; }
        public bool IsActive { get; set; } = true;

        public Collectible(int id, int row, int col)
        {
            Id = id;
            Y = row;
            X = col;
        }
    }
}
