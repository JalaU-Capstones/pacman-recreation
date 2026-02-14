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

    private readonly Dictionary<PlayerRole, Direction> _playerInputs = new();
    private List<PlayerRole> _assignedRoles = new();

    public event Action<GameEventMessage>? OnGameEvent;

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
        _playerInputs.Clear();
        foreach (var role in Enum.GetValues(typeof(PlayerRole)))
        {
            if (role is PlayerRole r) _playerInputs[r] = Direction.None;
        }
        LoadLevel(1);
        _frameCount = 0;
        _logger.LogInformation($"[SIMULATION] Game initialized. Roles: {string.Join(", ", assignedRoles)}");
    }

    public void UpdateAssignedRoles(List<PlayerRole> roles)
    {
        _assignedRoles = roles;
        _logger.LogInformation($"[SIMULATION] Updated assigned roles: {string.Join(", ", roles)}");
        // Reload level to spawn/despawn entities based on roles if needed, or just update internal list
        // For now, we keep entities but only update those with assigned roles
    }

    public void RemovePlayerRole(PlayerRole role)
    {
        _logger.LogInformation($"Removing player role: {role}");
        _assignedRoles.Remove(role);
        _playerInputs[role] = Direction.None;

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
        // Always spawn Pacman if the role is assigned
        if (_assignedRoles.Contains(PlayerRole.Pacman))
        {
            var pacmanSpawn = _mapLoader.GetPacmanSpawn($"level{level}.txt");
            _pacman = new Pacman(pacmanSpawn.Row, pacmanSpawn.Col) { CurrentDirection = Direction.None };
            _playerInputs[PlayerRole.Pacman] = Direction.None;
        }

        _ghosts.Clear();
        var ghostSpawns = _mapLoader.GetGhostSpawns($"level{level}.txt");

        // Helper to add ghost if role is assigned
        void AddGhostIfAssigned(PlayerRole role, GhostType type, int index)
        {
            if (_assignedRoles.Contains(role) && index < ghostSpawns.Count)
            {
                var spawn = ghostSpawns[index];
                var ghost = new Ghost(type, spawn.Row, spawn.Col) { CurrentDirection = Direction.None };
                _ghosts.Add(ghost);
                _playerInputs[role] = Direction.None;
            }
        }

        AddGhostIfAssigned(PlayerRole.Blinky, GhostType.Blinky, 0);
        AddGhostIfAssigned(PlayerRole.Pinky, GhostType.Pinky, 1);
        AddGhostIfAssigned(PlayerRole.Inky, GhostType.Inky, 2);
        AddGhostIfAssigned(PlayerRole.Clyde, GhostType.Clyde, 3);

        _collectibles = _mapLoader.GetCollectibles($"level{level}.txt");
        _currentLevel = level;
    }

    public void SetPlayerInput(PlayerRole role, Direction direction)
    {
        if (role == PlayerRole.None || role == PlayerRole.Spectator)
        {
            _logger.LogWarning($"[SIMULATION] Ignoring input for non-playable role: {role}");
            return;
        }

        _playerInputs[role] = direction;

        _logger.LogDebug($"[SIMULATION] Input stored: {role} -> {direction}");
    }

    public void Update(float deltaTime)
    {
        if (_map.Length == 0) return;
        _frameCount++;

        if (_pacman != null) UpdatePacman(_pacman, deltaTime);

        foreach (var ghost in _ghosts)
        {
            UpdateGhost(ghost, deltaTime);
        }

        CheckCollisions();
        // CheckGameEnd(); // TODO: Implement game end logic
    }

    private void UpdatePacman(Pacman pacman, float deltaTime)
    {
        HandleTurning(pacman, PlayerRole.Pacman);
        float speed = GetPacmanSpeed();
        MoveEntity(pacman, speed, deltaTime);
    }

    private void UpdateGhost(Ghost ghost, float deltaTime)
    {
        if (ghost.State == GhostStateEnum.Eaten) return;

        PlayerRole ghostRole = GetRoleForGhost(ghost.Type);

        // Check if this ghost has player input
        if (!_playerInputs.TryGetValue(ghostRole, out var desiredDirection))
        {
            // NO INPUT -> Ghost must STOP
            ghost.CurrentDirection = Direction.None;
            ghost.X = (float)Math.Round(ghost.X);
            ghost.Y = (float)Math.Round(ghost.Y);

            _logger.LogDebug($"[SIMULATION] Ghost {ghost.Type} has no input - stopped at ({ghost.X}, {ghost.Y})");
            return; // EXIT EARLY, do NOT move
        }

        // Has input -> process movement
        _logger.LogDebug($"[SIMULATION] Ghost {ghost.Type} received input: {desiredDirection}");

        HandleTurning(ghost, ghostRole, desiredDirection);

        float speed = GetGhostSpeed(ghost);
        MoveEntity(ghost, speed, deltaTime);
    }

    private void HandleTurning(Entity entity, PlayerRole role, Direction? overrideDirection = null)
    {
        Direction desiredDirection;
        if (overrideDirection.HasValue)
        {
            desiredDirection = overrideDirection.Value;
        }
        else if (!_playerInputs.TryGetValue(role, out desiredDirection))
        {
            desiredDirection = Direction.None;
        }

        if (desiredDirection == Direction.None) return;

        // If we want to turn, check if we can
        if (desiredDirection != entity.CurrentDirection)
        {
            // Center check
            float roundedX = (float)Math.Round(entity.X);
            float roundedY = (float)Math.Round(entity.Y);

            // Tolerance for being "at the center" of a tile
            float tolerance = 0.15f;

            bool isAtCenter = Math.Abs(entity.X - roundedX) < tolerance &&
                              Math.Abs(entity.Y - roundedY) < tolerance;

            bool isOpposite = IsOppositeDirection(entity.CurrentDirection, desiredDirection);

            if (isAtCenter || isOpposite)
            {
                var (dx, dy) = GetDirectionDeltas(desiredDirection);
                // Check if the target tile is valid
                // If at center, we check the immediate neighbor in desired direction
                // If opposite, we are just reversing, which is usually valid unless we just entered a wall (unlikely)

                // For center turn, we need to ensure we are exactly at center to turn cleanly
                if (isAtCenter)
                {
                    // Snap to center to avoid drift
                    entity.X = roundedX;
                    entity.Y = roundedY;

                    // Check if we can move in the new direction
                    if (!IsCollision(roundedX + dx, roundedY + dy))
                    {
                        entity.CurrentDirection = desiredDirection;
                        _logger.LogDebug($"[SIMULATION] {role} turned to {desiredDirection} at ({entity.X}, {entity.Y})");
                    }
                }
                else if (isOpposite)
                {
                    entity.CurrentDirection = desiredDirection;
                    _logger.LogDebug($"[SIMULATION] {role} reversed to {desiredDirection}");
                }
            }
        }
    }

    private void MoveEntity(Entity entity, float speed, float deltaTime)
    {
        if (entity.CurrentDirection == Direction.None) return;

        var (dx, dy) = GetDirectionDeltas(entity.CurrentDirection);

        float nextX = entity.X + dx * speed * deltaTime;
        float nextY = entity.Y + dy * speed * deltaTime;

        // Check collision at EXACT next position (center of the entity)
        // We treat the entity as a point for wall collision in this simplified grid model
        // But to be safe, we check the tile we are entering

        // Calculate the tile we would be in
        // If we are moving right, we check the right side of the entity
        // But since we are point-based, let's check the target coordinate

        if (!IsCollision(nextX, nextY))
        {
            // Safe to move
            entity.X = nextX;
            entity.Y = nextY;
            _logger.LogDebug($"[SIMULATION] Moving to ({nextX}, {nextY})");
        }
        else
        {
            // Hit wall -> stop at current tile center
            entity.X = (float)Math.Round(entity.X);
            entity.Y = (float)Math.Round(entity.Y);
            entity.CurrentDirection = Direction.None;

            string name = entity is Ghost g ? g.Type.ToString() : "Pacman";
            _logger.LogWarning($"[COLLISION] {name} blocked by wall at ({entity.X}, {entity.Y})");
        }
    }

    private bool IsCollision(float x, float y)
    {
        // Bounds check
        if (x < 0 || x >= _mapWidth || y < 0 || y >= _mapHeight)
            return true;

        // Convert to tile coordinates
        // We use a small epsilon to avoid floating point issues at boundaries
        int tileX = (int)Math.Floor(x + 0.01f); // Add small epsilon
        int tileY = (int)Math.Floor(y + 0.01f);

        // Double-check bounds after Floor
        if (tileX < 0 || tileX >= _mapWidth || tileY < 0 || tileY >= _mapHeight)
            return true;

        // Check tile type (Map is [row, col] = [y, x])
        return _map[tileY, tileX] == TileType.Wall;
    }

    private float GetGhostSpeed(Ghost ghost)
    {
        // Base speed for Normal state
        float baseSpeed = _currentLevel switch
        {
            1 => 4.0f,  // Level 1: Moderate speed
            2 => 4.2f,  // Level 2: 5% faster
            3 => 4.5f,  // Level 3: 12.5% faster
            _ => 4.0f
        };

        // Adjust for ghost state
        float speed = ghost.State switch
        {
            GhostStateEnum.Normal => baseSpeed,
            GhostStateEnum.Vulnerable => baseSpeed * 0.5f,  // 50% speed when vulnerable
            GhostStateEnum.Eaten => baseSpeed * 1.5f,       // 150% speed returning to spawn
            _ => baseSpeed
        };

        return speed;
    }

    private float GetPacmanSpeed()
    {
        // Pac-Man speed (slightly slower than ghosts for balance)
        return _currentLevel switch
        {
            1 => 3.8f,
            2 => 4.0f,
            3 => 4.2f,
            _ => 3.8f
        };
    }

    private (int dx, int dy) GetDirectionDeltas(Direction direction) => direction switch
    {
        Direction.Up => (0, -1),
        Direction.Down => (0, 1),
        Direction.Left => (-1, 0),
        Direction.Right => (1, 0),
        _ => (0, 0)
    };

    private bool IsOppositeDirection(Direction current, Direction desired)
    {
        if (current == Direction.Up && desired == Direction.Down) return true;
        if (current == Direction.Down && desired == Direction.Up) return true;
        if (current == Direction.Left && desired == Direction.Right) return true;
        if (current == Direction.Right && desired == Direction.Left) return true;
        return false;
    }

    private PlayerRole GetRoleForGhost(GhostType type) => type switch
    {
        GhostType.Blinky => PlayerRole.Blinky,
        GhostType.Pinky => PlayerRole.Pinky,
        GhostType.Inky => PlayerRole.Inky,
        GhostType.Clyde => PlayerRole.Clyde,
        _ => PlayerRole.None
    };

    private void CheckCollisions()
    {
        if (_pacman == null) return;

        // Pacman vs Dots
        // Simple distance check
        for (int i = _collectibles.Count - 1; i >= 0; i--)
        {
            var c = _collectibles[i];
            // Use X and Y from Entity base class instead of Col and Row
            if (c.IsActive && Math.Abs(_pacman.X - c.X) < 0.5f && Math.Abs(_pacman.Y - c.Y) < 0.5f)
            {
                c.IsActive = false;
                _score += 10; // TODO: Different scores for different items
                OnGameEvent?.Invoke(new GameEventMessage { EventType = GameEventType.DotCollected });
            }
        }

        // Pacman vs Ghosts
        foreach (var ghost in _ghosts)
        {
            if (Math.Abs(_pacman.X - ghost.X) < 0.5f && Math.Abs(_pacman.Y - ghost.Y) < 0.5f)
            {
                if (ghost.State == GhostStateEnum.Vulnerable)
                {
                    ghost.State = GhostStateEnum.Eaten;
                    _score += 200;
                    OnGameEvent?.Invoke(new GameEventMessage { EventType = GameEventType.GhostEaten });
                }
                else if (ghost.State == GhostStateEnum.Normal)
                {
                    // Pacman dies
                    _lives--;
                    OnGameEvent?.Invoke(new GameEventMessage { EventType = GameEventType.PacmanDied });
                    // Reset positions?
                }
            }
        }
    }

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
}
