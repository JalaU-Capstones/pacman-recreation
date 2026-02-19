namespace PacmanGame.Shared;

public enum MessageType
{
    CreateRoomRequest,
    CreateRoomResponse,
    JoinRoomRequest,
    JoinRoomResponse,
    AssignRoleRequest,
    StartGameRequest,
    PlayerInput,
    GameState,
    GameEvent,
    PauseGameRequest,
    ResumeGameRequest,
    RoomStateUpdate,
    LeaveRoomRequest,
    KickPlayerRequest,
    Kicked,
    RoleAssigned,
    GetRoomListRequest,
    GetRoomListResponse,
    LeaveRoomConfirmation,
    GameStartEvent,
    GamePausedEvent,
    RestartGameRequest,
    SpectatorPromotionEvent,
    NewPlayerJoinedEvent,
    SpectatorPromotionFailedEvent,
    LeaderboardGetTop10Request = 30,
    LeaderboardGetTop10Response = 31,
    LeaderboardSubmitScoreRequest = 32,
    LeaderboardSubmitScoreResponse = 33
}
