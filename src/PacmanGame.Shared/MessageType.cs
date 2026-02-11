namespace PacmanGame.Shared;

public enum MessageType : byte
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
    ResumeGameRequest
}
