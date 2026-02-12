using System;
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
    private readonly ILogger<GameSimulation> _logger;

    private Pacman? _pacman;
    private List<Ghost> _ghosts = new();
    private List<Collectible> _collectibles = new();
    private TileType[,] _map;
    private int _mapWidth;
    private int _mapHeight;

    private int _currentLevel = 1;
    private int _score = 0;
    private int _lives = 3;
    private ulong _frameCount = 0;

    private readonly Dictionary<PlayerRole, Direction> _lastIntents = new();
    private List<PlayerRole> _assignedRoles = new();

    public event Action<GameEventMessage>? OnGameEvent;

    private const float TILE_CENTER_TOLERANCE = 0.35f;

    public GameSimulation(IMapLoader mapLoader, ILogger<GameSimulation> logger)
    {
        _mapLoader = mapLoader;
        _logger = logger;
        _map = new TileType[0, 0];
    }

    public void Initialize(int roomId, List<PlayerRole> assignedRoles)
    {
        _logger.LogInformation($"[SIMULATION] Initializing game for Room {roomId}");
        _assignedRoles = assignedRoles;
        _lastIntents.Clear();
        foreach (var role in Enum.GetValues(typeof(PlayerRole)))
        {
            if (role is PlayerRole r) _lastIntents[r] = Direction.None;
        }
        LoadLevel(1);
        _frameCount = 0;
        _logger.LogInformation($"[SIMULATION] Game initialized. Roles: {string.Join(", ", assignedRoles)}");
    }

    public void UpdateAssignedRoles(List<PlayerRole> roles)
    {
        _assignedRoles = roles;
        _logger.LogInformation($"[SIMULATION] Updated assigned roles: {string.Join(", ", roles)}");
        LoadLevel(_currentLevel);
    }

    private void LoadLevel(int level)
    {
        _map = _mapLoader.LoadMap($"level{level}.txt");
        if (_map.Length == 0)
        {
            _logger.LogCritical($"[SIMULATION] FATAL: Map 'level{level}.txt' failed to load.");
            return;
        }
        _mapHeight = _map.GetLength(0);
        _mapWidth = _map.GetLength(1);

        _pacman = null;
        if (_assignedRoles.Contains(PlayerRole.Pacman))
        {
            var pacmanSpawn = _mapLoader.GetPacmanSpawn($"level{level}.txt");
            _pacman = new Pacman(pacmanSpawn.Row, pacmanSpawn.Col) { CurrentDirection = Direction.None };
            _lastIntents[PlayerRole.Pacman] = Direction.None;
        }

        _ghosts.Clear();
        var ghostSpawns = _mapLoader.GetGhostSpawns($"level{level}.txt");
        Action<Ghost, PlayerRole> addGhost = (ghost, role) =>
        {
            ghost.CurrentDirection = Direction.None;
            _ghosts.Add(ghost);
            _lastIntents[role] = Direction.None;
        };

        if (_assignedRoles.Contains(PlayerRole.Blinky)) addGhost(new Ghost(GhostType.Blinky, ghostSpawns[0].Row, ghostSpawns[0].Col), PlayerRole.Blinky);
        if (_assignedRoles.Contains(PlayerRole.Pinky)) addGhost(new Ghost(GhostType.Pinky, ghostSpawns[1].Row, ghostSpawns[1].Col), PlayerRole.Pinky);
        if (_assignedRoles.Contains(PlayerRole.Inky)) addGhost(new Ghost(GhostType.Inky, ghostSpawns[2].Row, ghostSpawns[2].Col), PlayerRole.Inky);
        if (_assignedRoles.Contains(PlayerRole.Clyde)) addGhost(new Ghost(GhostType.Clyde, ghostSpawns[3].Row, ghostSpawns[3].Col), PlayerRole.Clyde);

        foreach (var ghost in _ghosts)
        {
            ghost.CurrentDirection = Direction.None;
        }

        _collectibles = _mapLoader.GetCollectibles($"level{level}.txt");
        _currentLevel = level;
    }

    public void Update(float deltaTime)
    {
        if (_map.Length == 0) return;
        _frameCount++;

        if (_pacman != null) UpdateEntity(_pacman, 4.0f, deltaTime);
        foreach (var ghost in _ghosts)
        {
            UpdateEntity(ghost, ghost.State == GhostStateEnum.Vulnerable ? 2.5f : 4.5f, deltaTime);
        }

        CheckCollisions();
        CheckGameEnd();
    }

    private void UpdateEntity(Entity entity, float speed, float deltaTime)
    {
        var role = GetRoleForEntity(entity);
        if (role == PlayerRole.None || !_assignedRoles.Contains(role)) return;

        if (entity is Ghost g && g.State == GhostStateEnum.Eaten) return;

        HandleTurning(entity, role);
        MoveEntity(entity, speed, deltaTime);
    }

    private void HandleTurning(Entity entity, PlayerRole role)
    {
        if (!_lastIntents.TryGetValue(role, out var desiredDirection) || desiredDirection == Direction.None) return;

        if (desiredDirection == entity.CurrentDirection) return;

        float roundedX = (float)Math.Round(entity.X);
        float roundedY = (float)Math.Round(entity.Y);

        bool isAtCenter = Math.Abs(entity.X - roundedX) < TILE_CENTER_TOLERANCE &&
                          Math.Abs(entity.Y - roundedY) < TILE_CENTER_TOLERANCE;

        bool isOpposite = IsOppositeDirection(entity.CurrentDirection, desiredDirection);

        if (isAtCenter || isOpposite)
        {
            var (dx, dy) = GetDirectionDeltas(desiredDirection);
            float checkX = roundedX + dx * 0.5f;
            float checkY = roundedY + dy * 0.5f;

            if (IsValidMove(entity, checkX, checkY))
            {
                entity.CurrentDirection = desiredDirection;
                if (isAtCenter)
                {
                    entity.X = roundedX;
                    entity.Y = roundedY;
                }
            }
        }
    }

    private void MoveEntity(Entity entity, float speed, float deltaTime)
    {
        var role = GetRoleForEntity(entity);

        // Handle starting from a standstill (the "dead keys" fix)
        if (entity.CurrentDirection == Direction.None)
        {
            if (_lastIntents.TryGetValue(role, out var intent) && intent != Direction.None)
            {
                var (dx, dy) = GetDirectionDeltas(intent);
                // Check if a small move in the intended direction is valid
                if (IsValidMove(entity, entity.X + dx * 0.1f, entity.Y + dy * 0.1f))
                {
                    entity.CurrentDirection = intent; // Immediately update direction
                }
            }
        }

        // If still no direction, do nothing
        if (entity.CurrentDirection == Direction.None) return;

        // Proceed with movement
        var (dx_move, dy_move) = GetDirectionDeltas(entity.CurrentDirection);
        float nextX = entity.X + dx_move * speed * deltaTime;
        float nextY = entity.Y + dy_move * speed * deltaTime;

        if (IsValidMove(entity, nextX, nextY))
        {
            entity.X = nextX;
            entity.Y = nextY;
        }
        else // Hit a wall
        {
            // Snap to the grid and stop
            float snappedX = (float)Math.Round(entity.X);
            float snappedY = (float)Math.Round(entity.Y);

            if (!IsCollision(entity, snappedX, snappedY))
            {
                entity.X = snappedX;
                entity.Y = snappedY;
            }
            entity.CurrentDirection = Direction.None;
        }
    }

    public void RemovePlayerRole(PlayerRole role)
    {
        _logger.LogInformation($"Removing player role: {role}");
        _assignedRoles.Remove(role);
        _lastIntents[role] = Direction.None;

        if (role == PlayerRole.Pacman)
        {
            _pacman = null;
        }
        else
        {
            var ghostToRemove = _ghosts.FirstOrDefault(g => GetRoleForGhost(g.Type) == role);
            if (ghostToRemove != null)
            {
                _ghosts.Remove(ghostToRemove);
            }
        }
    }

    private bool IsValidMove(Entity entity, float nextX, float nextY)
    {
        float r = 0.45f; // Collision radius
        return !IsCollision(entity, nextX - r, nextY - r) &&
               !IsCollision(entity, nextX + r, nextY - r) &&
               !IsCollision(entity, nextX - r, nextY + r) &&
               !IsCollision(entity, nextX + r, nextY + r);
    }

    private bool IsCollision(Entity? entity, float x, float y)
    {
        int col = (int)Math.Floor(x + 1e-4f);
        int row = (int)Math.Floor(y + 1e-4f);

        if (row < 0 || row >= _mapHeight || col < 0 || col >= _mapWidth) return true;

        var tile = _map[row, col];
        if (tile == TileType.Wall) return true;
        if (tile == TileType.GhostDoor && entity is not Ghost) return true;

        return false;
    }

    private bool IsOppositeDirection(Direction current, Direction desired)
    {
        if (current == Direction.Up && desired == Direction.Down) return true;
        if (current == Direction.Down && desired == Direction.Up) return true;
        if (current == Direction.Left && desired == Direction.Right) return true;
        if (current == Direction.Right && desired == Direction.Left) return true;
        return false;
    }

    private PlayerRole GetRoleForEntity(Entity entity)
    {
        if (entity is Pacman) return PlayerRole.Pacman;
        if (entity is Ghost g) return GetRoleForGhost(g.Type);
        return PlayerRole.None;
    }

    private PlayerRole GetRoleForGhost(GhostType type) => type switch
    {
        GhostType.Blinky => PlayerRole.Blinky,
        GhostType.Pinky => PlayerRole.Pinky,
        GhostType.Inky => PlayerRole.Inky,
        GhostType.Clyde => PlayerRole.Clyde,
        _ => PlayerRole.None
    };

    private (int dx, int dy) GetDirectionDeltas(Direction direction) => direction switch
    {
        Direction.Up => (0, -1),
        Direction.Down => (0, 1),
        Direction.Left => (-1, 0),
        Direction.Right => (1, 0),
        _ => (0, 0)
    };

    private void CheckCollisions()
    {
        if (_pacman == null) return;
        // ... collision logic
    }

    private void ResetPositions() { }
    private void CheckGameEnd() { }

    public GameStateMessage GetState()
    {
        return new GameStateMessage
        {
            PacmanPosition = _pacman != null ? new EntityPosition { X = _pacman.X, Y = _pacman.Y, Direction = _pacman.CurrentDirection } : null,
            Ghosts = _ghosts.Select(g => new GhostState
            {
                Type = g.Type.ToString(),
                Position = new EntityPosition { X = g.X, Y = g.Y, Direction = g.CurrentDirection },
                State = g.State
            }).ToList(),
            CollectedItems = _collectibles.Where(c => !c.IsActive).Select(c => c.Id).ToList(),
            Score = _score,
            Lives = _lives,
            CurrentLevel = _currentLevel
        };
    }

    public void SetPlayerInput(PlayerRole role, Direction direction)
    {
        if (_assignedRoles.Contains(role))
        {
            _logger.LogDebug($"[SIMULATION] Input received for {role}: {direction}");
            _lastIntents[role] = direction;
        }
        else
        {
            _logger.LogWarning($"[SIMULATION] Received input for unassigned role: {role}");
        }
    }
}
