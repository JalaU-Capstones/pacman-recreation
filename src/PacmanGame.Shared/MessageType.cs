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
    RoomStateUpdate, // Full state sync for the lobby

    // Player Actions
    KickPlayerRequest,
    Kicked, // Event sent to the kicked player

    // Game Logic
    AssignRoleRequest,
    RoleAssigned, // Event confirming role change
    StartGameRequest,
    GameStartEvent, // Broadcast to all players to start the game
    PlayerInput,
    GameState,
    GameEvent,
    PauseGameRequest,
    ResumeGameRequest
}
