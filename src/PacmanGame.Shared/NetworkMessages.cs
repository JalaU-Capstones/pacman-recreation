using MessagePack;
using System.Collections.Generic;

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
[Union(11, typeof(RoomStateUpdateMessage))]
[Union(12, typeof(LeaveRoomRequest))]
[Union(13, typeof(KickPlayerRequest))]
[Union(14, typeof(KickedEvent))]
[Union(15, typeof(RoleAssignedEvent))]
public abstract class NetworkMessageBase
{
    [Key(0)]
    public abstract MessageType Type { get; }
}

#region Room Management Messages

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
    [Key(4)]
    public string PlayerName { get; set; } = string.Empty;
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
    [Key(5)]
    public RoomVisibility Visibility { get; set; }
    [Key(6)]
    public List<PlayerState> Players { get; set; } = new();
}

[MessagePackObject]
public class JoinRoomRequest : NetworkMessageBase
{
    public override MessageType Type => MessageType.JoinRoomRequest;
    [Key(1)]
    public string RoomName { get; set; } = string.Empty;
    [Key(2)]
    public string? Password { get; set; }
    [Key(3)]
    public string PlayerName { get; set; } = string.Empty;
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
    [Key(5)]
    public RoomVisibility Visibility { get; set; }
    [Key(6)]
    public List<PlayerState> Players { get; set; } = new();
}

[MessagePackObject]
public class LeaveRoomRequest : NetworkMessageBase
{
    public override MessageType Type => MessageType.LeaveRoomRequest;
}

[MessagePackObject]
public class RoomStateUpdateMessage : NetworkMessageBase
{
    public override MessageType Type => MessageType.RoomStateUpdate;
    [Key(1)]
    public List<PlayerState> Players { get; set; } = new();
}

#endregion

#region Player Action Messages

[MessagePackObject]
public class KickPlayerRequest : NetworkMessageBase
{
    public override MessageType Type => MessageType.KickPlayerRequest;
    [Key(1)]
    public int PlayerIdToKick { get; set; }
}

[MessagePackObject]
public class KickedEvent : NetworkMessageBase
{
    public override MessageType Type => MessageType.Kicked;
    [Key(1)]
    public string Reason { get; set; } = string.Empty;
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
public class RoleAssignedEvent : NetworkMessageBase
{
    public override MessageType Type => MessageType.RoleAssigned;
    [Key(1)]
    public int PlayerId { get; set; }
    [Key(2)]
    public PlayerRole Role { get; set; }
}

#endregion

#region Game Flow Messages

[MessagePackObject]
public class StartGameRequest : NetworkMessageBase
{
    public override MessageType Type => MessageType.StartGameRequest;
}

[MessagePackObject]
public class GameStartEvent : NetworkMessageBase
{
    public override MessageType Type => MessageType.GameStartEvent;
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

#endregion
