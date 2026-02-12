namespace PacmanGame.Shared;

public enum MessageType
{
    // Connection
    ConnectionRequest,
    ConnectionResponse,

    // Room Management
    CreateRoomRequest,
    CreateRoomResponse,
    JoinRoomRequest,
    JoinRoomResponse,
    LeaveRoomRequest,
    LeaveRoomConfirmation, // Server -> Client
    RoomStateUpdate,
    GetRoomListRequest,
    GetRoomListResponse,

    // Player Actions
    KickPlayerRequest,
    Kicked,

    // Game Logic
    AssignRoleRequest,
    RoleAssigned,
    StartGameRequest,
    GameStartEvent,
    PlayerInput,
    GameState,
    GameEvent,
    PauseGameRequest,
    ResumeGameRequest,
    GamePausedEvent
}
