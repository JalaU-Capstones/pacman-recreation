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
    SpectatorPromotionFailedEvent
}
