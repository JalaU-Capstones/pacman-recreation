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
    private int _roomId;

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
    private bool _isGameOver = false;

    private readonly Dictionary<PlayerRole, Direction> _playerInputs = new();
    private List<PlayerRole> _assignedRoles = new();

    private float _powerPelletTimer = 0f;
    private readonly Dictionary<GhostType, (int Row, int Col)> _ghostSpawns = new();

    public event Action<GameEventMessage>? OnGameEvent;
    public event Action<RoundResetEvent>? OnRoundReset;

    public GameSimulation(IMapLoader mapLoader, ILogger<GameSimulation> logger)
    {
        _mapLoader = mapLoader;
        _logger = logger;
        _map = new TileType[0, 0];
    }

    public void Initialize(int roomId, List<PlayerRole> assignedRoles)
    {
        _roomId = roomId;
        _logger.LogInformation($"[SIMULATION] Initializing game for Room {roomId}");
        _assignedRoles = assignedRoles;
        _playerInputs.Clear();
        foreach (var role in Enum.GetValues(typeof(PlayerRole)))
        {
            if (role is PlayerRole r) _playerInputs[r] = Direction.None;
        }
        LoadLevel(1);
        _frameCount = 0;
        _isGameOver = false;
        _logger.LogInformation($"[SIMULATION] Game initialized. Roles: {string.Join(", ", assignedRoles)}");
    }

    public void UpdateAssignedRoles(List<PlayerRole> roles)
    {
        _assignedRoles = roles;
        _logger.LogInformation($"[SIMULATION] Updated assigned roles: {string.Join(", ", roles)}");
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

    public void AddPlayerRole(PlayerRole role)
    {
        if (_assignedRoles.Contains(role))
        {
            _logger.LogWarning($"[SIMULATION] Role {role} is already assigned.");
            return;
        }

        _assignedRoles.Add(role);
        _playerInputs[role] = Direction.None;

        _logger.LogInformation($"[SIMULATION] Adding player with role: {role}");

        if (role == PlayerRole.Pacman)
        {
            if (_pacman == null)
            {
                var pacmanSpawn = _mapLoader.GetPacmanSpawn($"level{_currentLevel}.txt");
                _pacman = new Pacman(pacmanSpawn.Row, pacmanSpawn.Col) { CurrentDirection = Direction.None };
                _logger.LogInformation($"[SIMULATION] Created Pacman entity.");
            }
        }
        else
        {
            GhostType? ghostType = GetGhostTypeForRole(role);
            if (ghostType.HasValue)
            {
                if (!_ghosts.Any(g => g.Type == ghostType.Value))
                {
                    var ghostSpawnsData = _mapLoader.GetGhostSpawns($"level{_currentLevel}.txt");
                    int ghostIndex = GetGhostIndex(ghostType.Value);

                    if (ghostIndex != -1 && ghostIndex < ghostSpawnsData.Count)
                    {
                        var spawn = ghostSpawnsData[ghostIndex];
                        if (!_ghostSpawns.ContainsKey(ghostType.Value))
                        {
                            _ghostSpawns[ghostType.Value] = spawn;
                        }

                        var ghost = new Ghost(ghostType.Value, spawn.Row, spawn.Col)
                        {
                            CurrentDirection = Direction.None,
                            IsAIControlled = false
                        };
                        _ghosts.Add(ghost);
                        _logger.LogInformation($"[SIMULATION] Created {ghostType.Value} entity.");
                    }
                }
            }
        }
    }

    private GhostType? GetGhostTypeForRole(PlayerRole role) => role switch
    {
        PlayerRole.Blinky => GhostType.Blinky,
        PlayerRole.Pinky => GhostType.Pinky,
        PlayerRole.Inky => GhostType.Inky,
        PlayerRole.Clyde => GhostType.Clyde,
        _ => null
    };

    private int GetGhostIndex(GhostType type) => type switch
    {
        GhostType.Blinky => 0,
        GhostType.Pinky => 1,
        GhostType.Inky => 2,
        GhostType.Clyde => 3,
        _ => -1
    };

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
            _playerInputs[PlayerRole.Pacman] = Direction.None;
        }

        _ghosts.Clear();
        _ghostSpawns.Clear();
        var ghostSpawnsData = _mapLoader.GetGhostSpawns($"level{level}.txt");

        // Some maps may contain multiple 'G' markers (e.g., ghost house interior filler).
        // Pick the 4 spawns closest to the map center for deterministic "ghost house" spawns.
        var centerRow = _mapHeight / 2;
        var centerCol = _mapWidth / 2;
        var canonicalSpawns = ghostSpawnsData
            .OrderBy(p => Math.Abs(p.Row - centerRow) + Math.Abs(p.Col - centerCol))
            .Take(4)
            .OrderBy(p => p.Row)
            .ThenBy(p => p.Col)
            .ToList();

        void AddGhostIfAssigned(PlayerRole role, GhostType type, int index)
        {
            if (_assignedRoles.Contains(role) && index < canonicalSpawns.Count)
            {
                var spawn = canonicalSpawns[index];
                _ghostSpawns[type] = spawn;
                var ghost = new Ghost(type, spawn.Row, spawn.Col)
                {
                    CurrentDirection = Direction.None,
                    IsAIControlled = false
                };
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
        _lives = 3;
        _score = 0;
        _powerPelletTimer = 0;
    }

    public void SetPlayerInput(PlayerRole role, Direction direction)
    {
        if (_isGameOver) return; // Ignore input if game is over

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
        if (_map.Length == 0 || _isGameOver) return;
        _frameCount++;

        UpdateTimers(deltaTime);

        if (_pacman != null) UpdatePacman(_pacman, deltaTime);

        foreach (var ghost in _ghosts)
        {
            UpdateGhost(ghost, deltaTime);
        }

        CheckCollisions();
    }

    private void UpdateTimers(float deltaTime)
    {
        if (_powerPelletTimer > 0)
        {
            _powerPelletTimer -= deltaTime;
            if (_powerPelletTimer <= 0)
            {
                foreach (var ghost in _ghosts.Where(g => g.State == GhostStateEnum.Vulnerable))
                {
                    ghost.State = GhostStateEnum.Normal;
                }
            }
        }

        foreach (var ghost in _ghosts.Where(g => g.State == GhostStateEnum.Eaten))
        {
            ghost.RespawnTimer -= deltaTime;
            if (ghost.RespawnTimer <= 0)
            {
                ghost.State = GhostStateEnum.Normal;
                // Ensure it's at spawn
                if (_ghostSpawns.TryGetValue(ghost.Type, out var spawn))
                {
                    ghost.X = spawn.Col;
                    ghost.Y = spawn.Row;
                }
            }
        }
    }

    private void ResetAllEntitiesAndBroadcastReady(int readySeconds)
    {
        // Clear any active frightened state (power pellet) immediately.
        _powerPelletTimer = 0f;

        // Reset Pac-Man.
        if (_pacman != null)
        {
            var pacmanSpawn = _mapLoader.GetPacmanSpawn($"level{_currentLevel}.txt");
            _pacman.X = pacmanSpawn.Col;
            _pacman.Y = pacmanSpawn.Row;
            _pacman.CurrentDirection = Direction.None;
            _playerInputs[PlayerRole.Pacman] = Direction.None;
        }

        // Reset all ghosts.
        foreach (var ghost in _ghosts)
        {
            if (_ghostSpawns.TryGetValue(ghost.Type, out var spawn))
            {
                ghost.X = spawn.Col;
                ghost.Y = spawn.Row;
            }
            ghost.CurrentDirection = Direction.None;
            ghost.State = GhostStateEnum.Normal;
            _playerInputs[GetRoleForGhost(ghost.Type)] = Direction.None;
        }

        var resetEvent = new RoundResetEvent
        {
            RoomId = 0, // populated by server; left as 0 here
            ReadySeconds = readySeconds,
            PacmanPosition = _pacman != null
                ? new EntityPosition { X = _pacman.X, Y = _pacman.Y, Direction = _pacman.CurrentDirection }
                : null,
            Ghosts = _ghosts.Select(g => new GhostState
            {
                Type = g.Type.ToString(),
                Position = new EntityPosition { X = g.X, Y = g.Y, Direction = g.CurrentDirection },
                State = g.State
            }).ToList(),
            Lives = _lives
        };

        _logger.LogInformation("[SIMULATION] Multiplayer life lost in room {RoomId}. Broadcasting global reset (ready {Seconds}s).", _roomId, readySeconds);
        OnRoundReset?.Invoke(resetEvent);
    }

    private void UpdatePacman(Pacman pacman, float deltaTime)
    {
        HandleTurning(pacman, PlayerRole.Pacman);
        float speed = GetPacmanSpeed();
        MoveEntity(pacman, speed, deltaTime);
    }

    private void UpdateGhost(Ghost ghost, float deltaTime)
    {
        if (ghost.State == GhostStateEnum.Eaten) return; // Wait for respawn timer

        if (ghost.IsAIControlled) return;

        PlayerRole ghostRole = GetRoleForGhost(ghost.Type);
        if (!_playerInputs.TryGetValue(ghostRole, out var desiredDirection))
        {
            ghost.CurrentDirection = Direction.None;
            return;
        }

        HandleTurning(ghost, ghostRole, desiredDirection);
        float speed = GetGhostSpeed(ghost);
        MoveEntity(ghost, speed, deltaTime);
    }

    private void HandleTurning(Entity entity, PlayerRole role, Direction? overrideDirection = null)
    {
        Direction desiredDirection = overrideDirection ?? _playerInputs.GetValueOrDefault(role, Direction.None);
        if (desiredDirection == Direction.None) return;

        if (desiredDirection != entity.CurrentDirection)
        {
            float roundedX = (float)Math.Round(entity.X);
            float roundedY = (float)Math.Round(entity.Y);
            float tolerance = 0.15f;
            bool isAtCenter = Math.Abs(entity.X - roundedX) < tolerance && Math.Abs(entity.Y - roundedY) < tolerance;
            bool isOpposite = IsOppositeDirection(entity.CurrentDirection, desiredDirection);

            if (isAtCenter || isOpposite)
            {
                var (dx, dy) = GetDirectionDeltas(desiredDirection);
                if (IsValidMove(roundedX + dx, roundedY + dy))
                {
                    if (isAtCenter)
                    {
                        entity.X = roundedX;
                        entity.Y = roundedY;
                    }
                    entity.CurrentDirection = desiredDirection;
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

        if (IsValidMove(nextX, nextY))
        {
            entity.X = nextX;
            entity.Y = nextY;
        }
        else
        {
            entity.X = (float)Math.Round(entity.X);
            entity.Y = (float)Math.Round(entity.Y);
            entity.CurrentDirection = Direction.None;
        }
    }

    private bool IsValidMove(float x, float y)
    {
        if (_map.Length == 0) return false;
        if (x < 0 || x >= _mapWidth || y < 0 || y >= _mapHeight) return false;
        int tileX = (int)Math.Round(x);
        int tileY = (int)Math.Round(y);
        if (tileX < 0 || tileX >= _mapWidth || tileY < 0 || tileY >= _mapHeight) return false;
        return _map[tileY, tileX] != TileType.Wall;
    }

    private float GetGhostSpeed(Ghost ghost)
    {
        float baseSpeed = 4.0f;
        return ghost.State switch
        {
            GhostStateEnum.Normal => baseSpeed,
            GhostStateEnum.Vulnerable => baseSpeed * 0.5f,
            GhostStateEnum.Eaten => baseSpeed * 1.5f,
            _ => baseSpeed
        };
    }

    private float GetPacmanSpeed() => 3.8f;

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
        return (current == Direction.Up && desired == Direction.Down) ||
               (current == Direction.Down && desired == Direction.Up) ||
               (current == Direction.Left && desired == Direction.Right) ||
               (current == Direction.Right && desired == Direction.Left);
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

        // Check for victory condition (all dots collected)
        bool allDotsCollected = !_collectibles.Any(c => c.IsActive && (c.Type == CollectibleType.SmallDot || c.Type == CollectibleType.PowerPellet));
        if (allDotsCollected)
        {
            _isGameOver = true;
            OnGameEvent?.Invoke(new GameEventMessage { EventType = GameEventType.LevelComplete });
            return;
        }

        for (int i = _collectibles.Count - 1; i >= 0; i--)
        {
            var c = _collectibles[i];
            if (c.IsActive && Math.Abs(_pacman.X - c.X) < 0.5f && Math.Abs(_pacman.Y - c.Y) < 0.5f)
            {
                c.IsActive = false;
                if (c.Type == CollectibleType.PowerPellet)
                {
                    _score += 50;
                    _powerPelletTimer = 6f;
                    foreach (var ghost in _ghosts.Where(g => g.State == GhostStateEnum.Normal))
                    {
                        ghost.State = GhostStateEnum.Vulnerable;
                    }
                    OnGameEvent?.Invoke(new GameEventMessage { EventType = GameEventType.PowerPelletCollected });
                }
                else
                {
                    _score += 10;
                    OnGameEvent?.Invoke(new GameEventMessage { EventType = GameEventType.DotCollected });
                }
            }
        }

        foreach (var ghost in _ghosts)
        {
            if (Math.Abs(_pacman.X - ghost.X) < 0.5f && Math.Abs(_pacman.Y - ghost.Y) < 0.5f)
            {
                if (ghost.State == GhostStateEnum.Vulnerable)
                {
                    ghost.State = GhostStateEnum.Eaten;
                    ghost.RespawnTimer = 3f;
                    if (_ghostSpawns.TryGetValue(ghost.Type, out var spawn))
                    {
                        ghost.X = spawn.Col;
                        ghost.Y = spawn.Row;
                    }
                    _score += 200;
                    OnGameEvent?.Invoke(new GameEventMessage { EventType = GameEventType.GhostEaten });
                }
                else if (ghost.State == GhostStateEnum.Normal)
                {
                    _lives--;
                    OnGameEvent?.Invoke(new GameEventMessage { EventType = GameEventType.PacmanDied });
                    if (_lives <= 0)
                    {
                        _lives = 0; // Prevent negative lives
                        _isGameOver = true;
                        OnGameEvent?.Invoke(new GameEventMessage { EventType = GameEventType.GameOver });
                    }
                    else
                    {
                        // Global entity reset with a short READY freeze.
                        ResetAllEntitiesAndBroadcastReady(2);
                    }
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

    public void TriggerGameOver()
    {
        if (!_isGameOver)
        {
            _isGameOver = true;
            OnGameEvent?.Invoke(new GameEventMessage { EventType = GameEventType.GameOver });
        }
    }
}
