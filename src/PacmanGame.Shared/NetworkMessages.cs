using MessagePack;

namespace PacmanGame.Shared;

[MessagePackObject]
[Union(0, typeof(CreateRoomRequest))]
[Union(1, typeof(CreateRoomResponse))]
[Union(2, typeof(JoinRoomRequest))]
[Union(3, typeof(JoinRoomResponse))]
[Union(4, typeof(AssignRoleRequest))]
[Union(5, typeof(StartGameRequest))]
[Union(6, typeof(PlayerInputMessage))]
[Union(7, typeof(GameStateMessage))]
[Union(8, typeof(GameEventMessage))]
[Union(9, typeof(PauseGameRequest))]
[Union(10, typeof(ResumeGameRequest))]
public abstract class NetworkMessageBase
{
    [Key(0)]
    public abstract MessageType Type { get; }
}

[MessagePackObject]
public class CreateRoomRequest : NetworkMessageBase
{
    public override MessageType Type => MessageType.CreateRoomRequest;

    [Key(1)]
    public string RoomName { get; set; } = string.Empty;

    [Key(2)]
    public RoomVisibility Visibility { get; set; }

    [Key(3)]
    public string? Password { get; set; }
}

[MessagePackObject]
public class CreateRoomResponse : NetworkMessageBase
{
    public override MessageType Type => MessageType.CreateRoomResponse;

    [Key(1)]
    public bool Success { get; set; }

    [Key(2)]
    public string? Message { get; set; }

    [Key(3)]
    public int RoomId { get; set; }

    [Key(4)]
    public string? RoomName { get; set; }
}

[MessagePackObject]
public class JoinRoomRequest : NetworkMessageBase
{
    public override MessageType Type => MessageType.JoinRoomRequest;

    [Key(1)]
    public string RoomName { get; set; } = string.Empty;

    [Key(2)]
    public string? Password { get; set; }
}

[MessagePackObject]
public class JoinRoomResponse : NetworkMessageBase
{
    public override MessageType Type => MessageType.JoinRoomResponse;

    [Key(1)]
    public bool Success { get; set; }

    [Key(2)]
    public string? Message { get; set; }

    [Key(3)]
    public int RoomId { get; set; }

    [Key(4)]
    public string? RoomName { get; set; }
}

[MessagePackObject]
public class AssignRoleRequest : NetworkMessageBase
{
    public override MessageType Type => MessageType.AssignRoleRequest;

    [Key(1)]
    public int PlayerId { get; set; }

    [Key(2)]
    public PlayerRole Role { get; set; }
}

[MessagePackObject]
public class StartGameRequest : NetworkMessageBase
{
    public override MessageType Type => MessageType.StartGameRequest;
}

[MessagePackObject]
public class PlayerInputMessage : NetworkMessageBase
{
    public override MessageType Type => MessageType.PlayerInput;

    [Key(1)]
    public int PlayerId { get; set; }

    [Key(2)]
    public Direction Direction { get; set; }

    [Key(3)]
    public long Timestamp { get; set; }
}

[MessagePackObject]
public class GameStateMessage : NetworkMessageBase
{
    public override MessageType Type => MessageType.GameState;

    [Key(1)]
    public Dictionary<int, PlayerState> PlayerStates { get; set; } = new();

    [Key(2)]
    public int Level { get; set; }

    [Key(3)]
    public int Score { get; set; }

    [Key(4)]
    public int Lives { get; set; }
}

[MessagePackObject]
public class GameEventMessage : NetworkMessageBase
{
    public override MessageType Type => MessageType.GameEvent;
    // Add properties for game events
}

[MessagePackObject]
public class PauseGameRequest : NetworkMessageBase
{
    public override MessageType Type => MessageType.PauseGameRequest;
}

[MessagePackObject]
public class ResumeGameRequest : NetworkMessageBase
{
    public override MessageType Type => MessageType.ResumeGameRequest;
}
