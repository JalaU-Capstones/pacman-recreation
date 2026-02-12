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

    [Key(3)]
    public int PlayerId { get; set; }

    [Key(4)]
    public string Name { get; set; } = string.Empty;

    [Key(5)]
    public bool IsAdmin { get; set; }
}
