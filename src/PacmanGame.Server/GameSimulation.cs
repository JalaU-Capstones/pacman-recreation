using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using PacmanGame.Server.Models;
using PacmanGame.Server.Services;
using PacmanGame.Shared;

namespace PacmanGame.Server;

public class GameSimulation
{
    private readonly IMapLoader _mapLoader;
    private readonly ICollisionDetector _collisionDetector;
    private readonly ILogger<GameSimulation> _logger;

    private Pacman _pacman;
    private List<Ghost> _ghosts = new();
    private List<Collectible> _collectibles = new();
    private TileType[,] _map;

    private int _currentLevel = 1;
    private int _score = 0;
    private int _lives = 3;

    private Dictionary<PlayerRole, Direction> _playerInputs = new();

    public GameSimulation(IMapLoader mapLoader, ICollisionDetector collisionDetector, ILogger<GameSimulation> logger)
    {
        _mapLoader = mapLoader;
        _collisionDetector = collisionDetector;
        _logger = logger;
        _pacman = new Pacman(0, 0);
        _map = new TileType[0, 0];
    }

    public void Initialize(int roomId, List<PlayerRole> assignedRoles)
    {
        _logger.LogInformation($"[SIMULATION] Initializing game for Room {roomId}");
        LoadLevel(1);
        _logger.LogInformation($"[SIMULATION] Game initialized with {assignedRoles.Count} players");
    }

    private void LoadLevel(int level)
    {
        _map = _mapLoader.LoadMap($"level{level}.txt");

        var pacmanSpawn = _mapLoader.GetPacmanSpawn($"level{level}.txt");
        _pacman = new Pacman(pacmanSpawn.Row, pacmanSpawn.Col);

        var ghostSpawns = _mapLoader.GetGhostSpawns($"level{level}.txt");
        _ghosts.Clear();
        _ghosts.Add(new Ghost(GhostType.Blinky, ghostSpawns[0].Row, ghostSpawns[0].Col));
        _ghosts.Add(new Ghost(GhostType.Pinky, ghostSpawns[1].Row, ghostSpawns[1].Col));
        _ghosts.Add(new Ghost(GhostType.Inky, ghostSpawns[2].Row, ghostSpawns[2].Col));
        _ghosts.Add(new Ghost(GhostType.Clyde, ghostSpawns[3].Row, ghostSpawns[3].Col));

        _collectibles = _mapLoader.GetCollectibles($"level{level}.txt");

        _currentLevel = level;
    }

    public void Update(float deltaTime)
    {
        foreach (var input in _playerInputs)
        {
            if (input.Key == PlayerRole.Pacman)
            {
                MovePacman(input.Value, deltaTime);
            }
            else
            {
                MoveGhost((GhostType)System.Enum.Parse(typeof(GhostType), input.Key.ToString()), input.Value, deltaTime);
            }
        }
        _playerInputs.Clear();

        CheckCollisions();
        CheckGameEnd();
    }

    private void MovePacman(Direction direction, float deltaTime)
    {
        _logger.LogInformation($"[GAMESIMULATION] MovePacman called with direction: {direction}");
        if (_collisionDetector.CanMove(_pacman, direction, _map))
        {
            _pacman.CurrentDirection = direction;
            var (dx, dy) = GetDirectionDeltas(direction);
            _pacman.X += dx * 4.0f * deltaTime;
            _pacman.Y += dy * 4.0f * deltaTime;
        }
    }

    private void MoveGhost(GhostType ghostType, Direction direction, float deltaTime)
    {
        var ghost = _ghosts.FirstOrDefault(g => g.Type == ghostType);
        if (ghost != null && _collisionDetector.CanMove(ghost, direction, _map))
        {
            ghost.CurrentDirection = direction;
            var (dx, dy) = GetDirectionDeltas(direction);
            ghost.X += dx * 3.7f * deltaTime;
            ghost.Y += dy * 3.7f * deltaTime;
        }
    }

    private (int dx, int dy) GetDirectionDeltas(Direction direction)
    {
        return direction switch
        {
            Direction.Up => (0, -1),
            Direction.Down => (0, 1),
            Direction.Left => (-1, 0),
            Direction.Right => (1, 0),
            _ => (0, 0)
        };
    }

    private void CheckCollisions()
    {
        // Implement collision detection logic
    }

    private void CheckGameEnd()
    {
        // Implement game end logic
    }

    public GameStateMessage GetState()
    {
        return new GameStateMessage
        {
            PacmanPosition = new EntityPosition
            {
                X = _pacman.X,
                Y = _pacman.Y,
                Direction = _pacman.CurrentDirection
            },
            Ghosts = _ghosts.Select(g => new GhostState
            {
                Type = g.Type.ToString(),
                Position = new EntityPosition { X = g.X, Y = g.Y, Direction = g.CurrentDirection },
                State = (GhostStateEnum)g.State
            }).ToList(),
            CollectedItems = _collectibles.Where(c => !c.IsActive).Select(c => c.Id).ToList(),
            Score = _score,
            Lives = _lives,
            CurrentLevel = _currentLevel
        };
    }

    public void SetPlayerInput(PlayerRole role, Direction direction)
    {
        _playerInputs[role] = direction;
    }
}
