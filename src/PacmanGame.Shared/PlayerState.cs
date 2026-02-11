using MessagePack;

namespace PacmanGame.Shared;

[MessagePackObject]
public class PlayerState
{
    [Key(0)]
    public float X { get; set; }

    [Key(1)]
    public float Y { get; set; }

    [Key(2)]
    public PlayerRole Role { get; set; }
}
