using PacmanGame.Server.Models;

namespace PacmanGame.Server;

public class GameSimulation
{
    private readonly Room _room;
    private GameState _gameState = new();

    public GameSimulation(Room room)
    {
        _room = room;
    }

    public void Start()
    {
        // Initialize game state
    }

    public void Update(float deltaTime)
    {
        // Update game state based on player inputs and game logic
    }

    public GameState GetGameState()
    {
        return _gameState;
    }
}
