using PacmanGame.Shared;

namespace PacmanGame.Server.Models
{
    public class Ghost : Entity
    {
        public GhostType Type { get; }
        public GhostStateEnum State { get; set; }
        public float RespawnTimer { get; set; }

        public Ghost(GhostType type, int row, int col)
        {
            Type = type;
            Y = row;
            X = col;
            State = GhostStateEnum.Normal;
            RespawnTimer = 0f;
        }
    }
}
