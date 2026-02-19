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
[Union(16, typeof(GetRoomListRequest))]
[Union(17, typeof(GetRoomListResponse))]
[Union(18, typeof(LeaveRoomConfirmation))]
[Union(19, typeof(GameStartEvent))]
[Union(20, typeof(GamePausedEvent))]
[Union(21, typeof(RestartGameRequest))]
[Union(22, typeof(SpectatorPromotionEvent))]
[Union(23, typeof(NewPlayerJoinedEvent))]
[Union(24, typeof(SpectatorPromotionFailedEvent))]
[Union(30, typeof(LeaderboardGetTop10Request))]
[Union(31, typeof(LeaderboardGetTop10Response))]
[Union(32, typeof(LeaderboardSubmitScoreRequest))]
[Union(33, typeof(LeaderboardSubmitScoreResponse))]
public abstract class NetworkMessageBase
{
    [IgnoreMember]
    public abstract MessageType Type { get; }
}

#region Room Management Messages

[MessagePackObject]
public class RoomDetails
{
    [Key(0)]
    public List<PlayerState> PlayerStates { get; set; } = new();
}

[MessagePackObject]
public class CreateRoomRequest : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.CreateRoomRequest;
    [Key(0)]
    public string RoomName { get; set; } = string.Empty;
    [Key(1)]
    public RoomVisibility Visibility { get; set; }
    [Key(2)]
    public string? Password { get; set; }
    [Key(3)]
    public string PlayerName { get; set; } = string.Empty;
}

[MessagePackObject]
public class CreateRoomResponse : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.CreateRoomResponse;
    [Key(0)]
    public bool Success { get; set; }
    [Key(1)]
    public string? Message { get; set; }
    [Key(2)]
    public int RoomId { get; set; }
    [Key(3)]
    public string? RoomName { get; set; }
    [Key(4)]
    public RoomVisibility Visibility { get; set; }
    [Key(5)]
    public List<PlayerState> Players { get; set; } = new();
}

[MessagePackObject]
public class JoinRoomRequest : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.JoinRoomRequest;
    [Key(0)]
    public string RoomName { get; set; } = string.Empty;
    [Key(1)]
    public string? Password { get; set; }
    [Key(2)]
    public string PlayerName { get; set; } = string.Empty;
    [Key(3)]
    public bool JoinAsSpectator { get; set; } // New field to request spectator join explicitly
}

public enum JoinRoomFailureReason
{
    None,
    RoomNotFound,
    IncorrectPassword,
    RoomFull,
    DuplicateUsername,
    AlreadyInRoom
}

[MessagePackObject]
public class JoinRoomResponse : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.JoinRoomResponse;
    [Key(0)]
    public bool Success { get; set; }
    [Key(1)]
    public string? Message { get; set; }
    [Key(2)]
    public int RoomId { get; set; }
    [Key(3)]
    public string? RoomName { get; set; }
    [Key(4)]
    public RoomVisibility Visibility { get; set; }
    [Key(5)]
    public List<PlayerState> Players { get; set; } = new();
    [Key(6)]
    public bool CanJoinAsSpectator { get; set; } // New field to prompt user
    [Key(7)]
    public bool IsGameStarted { get; set; }
    [Key(8)]
    public JoinRoomFailureReason FailureReason { get; set; }
}

[MessagePackObject]
public class LeaveRoomRequest : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.LeaveRoomRequest;
}

[MessagePackObject]
public class LeaveRoomConfirmation : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.LeaveRoomConfirmation;
}

[MessagePackObject]
public class RoomStateUpdateMessage : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.RoomStateUpdate;
    [Key(0)]
    public List<PlayerState> Players { get; set; } = new();
}

#endregion

#region Room Discovery

[MessagePackObject]
public class RoomInfo
{
    [Key(0)]
    public int RoomId { get; set; }
    [Key(1)]
    public string Name { get; set; } = string.Empty;
    [Key(2)]
    public int PlayerCount { get; set; }
    [Key(3)]
    public int MaxPlayers { get; set; }
}

[MessagePackObject]
public class GetRoomListRequest : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.GetRoomListRequest;
}

[MessagePackObject]
public class GetRoomListResponse : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.GetRoomListResponse;
    [Key(0)]
    public List<RoomInfo> Rooms { get; set; } = new();
}

#endregion

#region Player Action Messages

[MessagePackObject]
public class KickPlayerRequest : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.KickPlayerRequest;
    [Key(0)]
    public int PlayerIdToKick { get; set; }
}

[MessagePackObject]
public class KickedEvent : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.Kicked;
    [Key(0)]
    public string Reason { get; set; } = string.Empty;
}

[MessagePackObject]
public class AssignRoleRequest : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.AssignRoleRequest;
    [Key(0)]
    public int PlayerId { get; set; }
    [Key(1)]
    public PlayerRole Role { get; set; }
}

[MessagePackObject]
public class RoleAssignedEvent : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.RoleAssigned;
    [Key(0)]
    public int PlayerId { get; set; }
    [Key(1)]
    public PlayerRole Role { get; set; }
}

[MessagePackObject]
public class SpectatorPromotionEvent : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.SpectatorPromotionEvent;
    [Key(0)]
    public string PreviousPlayerName { get; set; } = string.Empty;
    [Key(1)]
    public PlayerRole NewRole { get; set; }
    [Key(2)]
    public int PreparationTimeSeconds { get; set; }
}

[MessagePackObject]
public class SpectatorPromotionFailedEvent : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.SpectatorPromotionFailedEvent;
    [Key(0)]
    public string Reason { get; set; } = string.Empty;
}

#endregion

#region Game Flow Messages

public enum GameEventType
{
    DotCollected,
    PowerPelletCollected,
    GhostEaten,
    FruitCollected,
    PacmanDied,
    LevelComplete,
    GameOver,
    Victory
}

public enum CollectibleType
{
    SmallDot,
    PowerPellet,
    Cherry,
    Strawberry,
    Orange,
    Apple,
    Melon,
    Galaxian,
    Bell,
    Key
}

[MessagePackObject]
public class EntityPosition
{
    [Key(0)]
    public float X { get; set; }
    [Key(1)]
    public float Y { get; set; }
    [Key(2)]
    public Direction Direction { get; set; }
}

public enum GhostStateEnum
{
    Normal,
    Vulnerable,
    Eaten
}

[MessagePackObject]
public class GhostState
{
    [Key(0)]
    public string Type { get; set; } = string.Empty;
    [Key(1)]
    public EntityPosition Position { get; set; } = new();
    [Key(2)]
    public GhostStateEnum State { get; set; }
}


[MessagePackObject]
public class StartGameRequest : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.StartGameRequest;
}

[MessagePackObject]
public class RestartGameRequest : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.RestartGameRequest;
}

[MessagePackObject]
public class GameStartEvent : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.GameStartEvent;
    [Key(0)]
    public List<PlayerState> PlayerStates { get; set; } = new();
    [Key(1)]
    public string MapName { get; set; } = string.Empty;
}

[MessagePackObject]
public class NewPlayerJoinedEvent : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.NewPlayerJoinedEvent;
    [Key(0)]
    public int PlayerId { get; set; }
    [Key(1)]
    public PlayerRole Role { get; set; }
    [Key(2)]
    public string PlayerName { get; set; } = string.Empty;
}

[MessagePackObject]
public class PlayerInputMessage : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.PlayerInput;
    [Key(0)]
    public int RoomId { get; set; }
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
    [IgnoreMember]
    public override MessageType Type => MessageType.GameState;
    [Key(0)]
    public EntityPosition? PacmanPosition { get; set; }
    [Key(1)]
    public List<GhostState> Ghosts { get; set; } = new();
    [Key(2)]
    public List<int> CollectedItems { get; set; } = new();
    [Key(3)]
    public int Score { get; set; }
    [Key(4)]
    public int Lives { get; set; }
    [Key(5)]
    public int CurrentLevel { get; set; }
}

[MessagePackObject]
public class GameEventMessage : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.GameEvent;
    [Key(0)]
    public GameEventType EventType { get; set; }
}

[MessagePackObject]
public class PauseGameRequest : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.PauseGameRequest;
}

[MessagePackObject]
public class ResumeGameRequest : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.ResumeGameRequest;
}

[MessagePackObject]
public class GamePausedEvent : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.GamePausedEvent;
    [Key(0)]
    public bool IsPaused { get; set; }
}

#endregion

#region Leaderboard Messages

[MessagePackObject]
public class LeaderboardGetTop10Request : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.LeaderboardGetTop10Request;
    [Key(0)] public long CacheTimestamp { get; set; }
}

[MessagePackObject]
public class LeaderboardGetTop10Response : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.LeaderboardGetTop10Response;
    [Key(0)] public List<LeaderboardEntry> Top10 { get; set; } = new();
    [Key(1)] public long ServerTimestamp { get; set; }
}

[MessagePackObject]
public class LeaderboardSubmitScoreRequest : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.LeaderboardSubmitScoreRequest;
    [Key(0)] public string ProfileId { get; set; } = string.Empty;
    [Key(1)] public string ProfileName { get; set; } = string.Empty;
    [Key(2)] public int HighScore { get; set; }
    [Key(3)] public long ClientTimestamp { get; set; }
}

[MessagePackObject]
public class LeaderboardSubmitScoreResponse : NetworkMessageBase
{
    [IgnoreMember]
    public override MessageType Type => MessageType.LeaderboardSubmitScoreResponse;
    [Key(0)] public bool Success { get; set; }
    [Key(1)] public string Message { get; set; } = string.Empty;
    [Key(2)] public int? NewRank { get; set; } // Null if not in top 10
    [Key(3)] public LeaderboardEntry? ReplacedEntry { get; set; } // Who was kicked out
}

[MessagePackObject]
public class LeaderboardEntry
{
    [Key(0)] public string ProfileId { get; set; } = string.Empty;
    [Key(1)] public string ProfileName { get; set; } = string.Empty;
    [Key(2)] public int HighScore { get; set; }
    [Key(3)] public long LastUpdated { get; set; }
}

#endregion
