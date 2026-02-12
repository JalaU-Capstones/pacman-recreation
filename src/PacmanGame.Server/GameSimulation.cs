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
    private readonly ICollisionDetector _collisionDetector;
    private readonly ILogger<GameSimulation> _logger;

    private Pacman? _pacman;
    private List<Ghost> _ghosts = new();
    private List<Collectible> _collectibles = new();
    private TileType[,] _map;

    private int _currentLevel = 1;
    private int _score = 0;
    private int _lives = 3;

    private Dictionary<PlayerRole, Direction> _playerInputs = new();
    private List<PlayerRole> _assignedRoles = new();

    public GameSimulation(IMapLoader mapLoader, ICollisionDetector collisionDetector, ILogger<GameSimulation> logger)
    {
        _mapLoader = mapLoader;
        _collisionDetector = collisionDetector;
        _logger = logger;
        _map = new TileType[0, 0];
    }

    public void Initialize(int roomId, List<PlayerRole> assignedRoles)
    {
        _logger.LogInformation($"[SIMULATION] Initializing game for Room {roomId}");
        _assignedRoles = assignedRoles;
        LoadLevel(1);
        _logger.LogInformation($"[SIMULATION] Game initialized with {assignedRoles.Count} players. Roles: {string.Join(", ", assignedRoles)}");
    }

    public void UpdateAssignedRoles(List<PlayerRole> roles)
    {
        _assignedRoles = roles;
        _logger.LogInformation($"[SIMULATION] Updated assigned roles: {string.Join(", ", roles)}");

        // Add ghosts if they are now assigned but weren't before
        var ghostSpawns = _mapLoader.GetGhostSpawns($"level{_currentLevel}.txt");

        if (_assignedRoles.Contains(PlayerRole.Blinky) && !_ghosts.Any(g => g.Type == GhostType.Blinky))
            _ghosts.Add(new Ghost(GhostType.Blinky, ghostSpawns[0].Row, ghostSpawns[0].Col));

        if (_assignedRoles.Contains(PlayerRole.Pinky) && !_ghosts.Any(g => g.Type == GhostType.Pinky))
            _ghosts.Add(new Ghost(GhostType.Pinky, ghostSpawns[1].Row, ghostSpawns[1].Col));

        if (_assignedRoles.Contains(PlayerRole.Inky) && !_ghosts.Any(g => g.Type == GhostType.Inky))
            _ghosts.Add(new Ghost(GhostType.Inky, ghostSpawns[2].Row, ghostSpawns[2].Col));

        if (_assignedRoles.Contains(PlayerRole.Clyde) && !_ghosts.Any(g => g.Type == GhostType.Clyde))
            _ghosts.Add(new Ghost(GhostType.Clyde, ghostSpawns[3].Row, ghostSpawns[3].Col));

        // Remove ghosts if they are no longer assigned
        _ghosts.RemoveAll(g => !IsGhostAssigned(g.Type));

        // Handle Pac-Man
        if (_assignedRoles.Contains(PlayerRole.Pacman) && _pacman == null)
        {
            var pacmanSpawn = _mapLoader.GetPacmanSpawn($"level{_currentLevel}.txt");
            _pacman = new Pacman(pacmanSpawn.Row, pacmanSpawn.Col);
        }
        else if (!_assignedRoles.Contains(PlayerRole.Pacman))
        {
            _pacman = null;
        }
    }

    private bool IsGhostAssigned(GhostType type)
    {
        return type switch
        {
            GhostType.Blinky => _assignedRoles.Contains(PlayerRole.Blinky),
            GhostType.Pinky => _assignedRoles.Contains(PlayerRole.Pinky),
            GhostType.Inky => _assignedRoles.Contains(PlayerRole.Inky),
            GhostType.Clyde => _assignedRoles.Contains(PlayerRole.Clyde),
            _ => false
        };
    }

    private void LoadLevel(int level)
    {
        _map = _mapLoader.LoadMap($"level{level}.txt");

        // Spawn Pac-Man only if assigned
        if (_assignedRoles.Contains(PlayerRole.Pacman))
        {
            var pacmanSpawn = _mapLoader.GetPacmanSpawn($"level{level}.txt");
            _pacman = new Pacman(pacmanSpawn.Row, pacmanSpawn.Col);
            _logger.LogInformation("[SIMULATION] Spawned Pac-Man");
        }
        else
        {
            _pacman = null;
            _logger.LogInformation("[SIMULATION] Pac-Man role not assigned - skipping spawn");
        }

        var ghostSpawns = _mapLoader.GetGhostSpawns($"level{level}.txt");
        _ghosts.Clear();

        // Spawn Ghosts only if assigned
        if (_assignedRoles.Contains(PlayerRole.Blinky))
        {
            _ghosts.Add(new Ghost(GhostType.Blinky, ghostSpawns[0].Row, ghostSpawns[0].Col));
            _logger.LogInformation("[SIMULATION] Spawned Blinky");
        }

        if (_assignedRoles.Contains(PlayerRole.Pinky))
        {
            _ghosts.Add(new Ghost(GhostType.Pinky, ghostSpawns[1].Row, ghostSpawns[1].Col));
            _logger.LogInformation("[SIMULATION] Spawned Pinky");
        }

        if (_assignedRoles.Contains(PlayerRole.Inky))
        {
            _ghosts.Add(new Ghost(GhostType.Inky, ghostSpawns[2].Row, ghostSpawns[2].Col));
            _logger.LogInformation("[SIMULATION] Spawned Inky");
        }

        if (_assignedRoles.Contains(PlayerRole.Clyde))
        {
            _ghosts.Add(new Ghost(GhostType.Clyde, ghostSpawns[3].Row, ghostSpawns[3].Col));
            _logger.LogInformation("[SIMULATION] Spawned Clyde");
        }

        _collectibles = _mapLoader.GetCollectibles($"level{level}.txt");

        _currentLevel = level;
    }

    public void Update(float deltaTime)
    {
        // Update Pac-Man
        if (_playerInputs.TryGetValue(PlayerRole.Pacman, out var pacmanDir))
        {
            MovePacman(pacmanDir, deltaTime);
        }

        // Update Ghosts
        foreach (var ghost in _ghosts)
        {
            UpdateGhost(ghost, deltaTime);
        }

        _playerInputs.Clear();

        CheckCollisions();
        CheckGameEnd();
    }

    private void UpdateGhost(Ghost ghost, float deltaTime)
    {
        // Handle Eaten/Respawn state
        if (ghost.State == GhostStateEnum.Eaten)
        {
            ghost.RespawnTimer -= deltaTime;
            if (ghost.RespawnTimer <= 0)
            {
                // Respawn logic
                var ghostSpawns = _mapLoader.GetGhostSpawns($"level{_currentLevel}.txt");
                int index = ghost.Type switch
                {
                    GhostType.Blinky => 0,
                    GhostType.Pinky => 1,
                    GhostType.Inky => 2,
                    GhostType.Clyde => 3,
                    _ => 0
                };

                if (index < ghostSpawns.Count)
                {
                    ghost.X = ghostSpawns[index].Col;
                    ghost.Y = ghostSpawns[index].Row;
                }

                ghost.State = GhostStateEnum.Normal;
                ghost.CurrentDirection = Direction.None;
            }
            return; // Don't move while eaten/respawning
        }

        // Determine movement source
        Direction moveDirection = Direction.None;
        PlayerRole ghostRole = GetRoleForGhost(ghost.Type);

        if (_assignedRoles.Contains(ghostRole))
        {
            // Player controlled
            if (_playerInputs.TryGetValue(ghostRole, out var inputDir))
            {
                moveDirection = inputDir;
            }
        }
        else
        {
            // AI controlled (Simple random movement for now if not assigned)
            // In a real implementation, this would call an AI service
            // For now, we'll just keep moving in current direction or pick random valid
            moveDirection = ghost.CurrentDirection;
            if (moveDirection == Direction.None || !_collisionDetector.CanMove(ghost, moveDirection, _map))
            {
                // Pick random valid direction
                var validMoves = new List<Direction> { Direction.Up, Direction.Down, Direction.Left, Direction.Right }
                    .Where(d => _collisionDetector.CanMove(ghost, d, _map))
                    .ToList();

                if (validMoves.Any())
                {
                    moveDirection = validMoves[new Random().Next(validMoves.Count)];
                }
            }
        }

        // Apply movement
        if (moveDirection != Direction.None)
        {
            MoveGhost(ghost, moveDirection, deltaTime);
        }
    }

    private PlayerRole GetRoleForGhost(GhostType type)
    {
        return type switch
        {
            GhostType.Blinky => PlayerRole.Blinky,
            GhostType.Pinky => PlayerRole.Pinky,
            GhostType.Inky => PlayerRole.Inky,
            GhostType.Clyde => PlayerRole.Clyde,
            _ => PlayerRole.None
        };
    }

    private void MovePacman(Direction direction, float deltaTime)
    {
        if (_pacman == null) return;

        // Try to move
        if (direction != Direction.None)
        {
            _pacman.CurrentDirection = direction;
        }

        if (_pacman.CurrentDirection != Direction.None)
        {
            var (dx, dy) = GetDirectionDeltas(_pacman.CurrentDirection);
            float newX = _pacman.X + dx * 4.0f * deltaTime;
            float newY = _pacman.Y + dy * 4.0f * deltaTime;

            // Check collision at new position (simplified)
            // We need to check grid coordinates
            int gridX = (int)Math.Round(newX);
            int gridY = (int)Math.Round(newY);

            if (CanMoveTo(gridX, gridY))
            {
                _pacman.X = newX;
                _pacman.Y = newY;
            }
        }
    }

    private void MoveGhost(Ghost ghost, Direction direction, float deltaTime)
    {
        // Only allow movement if in Normal state (player controlled)
        // If Vulnerable or Eaten, server AI should take over (not implemented yet for this step)
        if (ghost.State != GhostStateEnum.Normal && ghost.State != GhostStateEnum.Vulnerable) return;

        if (direction != Direction.None)
        {
            ghost.CurrentDirection = direction;
        }

        if (ghost.CurrentDirection != Direction.None)
        {
            var (dx, dy) = GetDirectionDeltas(ghost.CurrentDirection);
            float newX = ghost.X + dx * 3.7f * deltaTime;
            float newY = ghost.Y + dy * 3.7f * deltaTime;

            int gridX = (int)Math.Round(newX);
            int gridY = (int)Math.Round(newY);

            if (CanMoveTo(gridX, gridY))
            {
                ghost.X = newX;
                ghost.Y = newY;
            }
        }
    }

    private bool CanMoveTo(int x, int y)
    {
        if (x < 0 || x >= _map.GetLength(1) || y < 0 || y >= _map.GetLength(0))
            return false;

        return _map[y, x] != TileType.Wall;
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
        if (_pacman == null) return;

        // Check collectibles
        var collected = _collectibles.FirstOrDefault(c => c.IsActive &&
            Math.Abs(c.X - _pacman.X) < 0.5f && Math.Abs(c.Y - _pacman.Y) < 0.5f);

        if (collected != null)
        {
            collected.IsActive = false;
            _score += 10; // Simplified score

            if (collected.Type == CollectibleType.PowerPellet)
            {
                // Make ghosts vulnerable
                foreach (var ghost in _ghosts)
                {
                    if (ghost.State == GhostStateEnum.Normal)
                    {
                        ghost.State = GhostStateEnum.Vulnerable;
                        // TODO: Set timer to revert state
                    }
                }
            }
        }

        // Check ghosts
        foreach (var ghost in _ghosts)
        {
            if (Math.Abs(ghost.X - _pacman.X) < 0.5f && Math.Abs(ghost.Y - _pacman.Y) < 0.5f)
            {
                if (ghost.State == GhostStateEnum.Normal)
                {
                    // Pac-Man dies
                    _lives--;
                    ResetPositions();
                }
                else if (ghost.State == GhostStateEnum.Vulnerable)
                {
                    // Ghost eaten
                    ghost.State = GhostStateEnum.Eaten;
                    ghost.RespawnTimer = 3.0f; // 3 seconds respawn
                    _score += 200;

                    // Move to spawn immediately (visual effect handled by state)
                    var ghostSpawns = _mapLoader.GetGhostSpawns($"level{_currentLevel}.txt");
                    // We don't move it yet, let it stay "eyes" for a bit or move it to house
                    // For simplicity, let's move it to house immediately but keep state Eaten
                    // In a real game, eyes travel back. Here we just wait.
                }
            }
        }
    }

    private void ResetPositions()
    {
        // Reset Pac-Man
        if (_pacman != null)
        {
            var pacmanSpawn = _mapLoader.GetPacmanSpawn($"level{_currentLevel}.txt");
            _pacman.X = pacmanSpawn.Col;
            _pacman.Y = pacmanSpawn.Row;
            _pacman.CurrentDirection = Direction.None;
        }

        // Reset Ghosts
        var ghostSpawns = _mapLoader.GetGhostSpawns($"level{_currentLevel}.txt");
        foreach (var ghost in _ghosts)
        {
            int index = ghost.Type switch
            {
                GhostType.Blinky => 0,
                GhostType.Pinky => 1,
                GhostType.Inky => 2,
                GhostType.Clyde => 3,
                _ => 0
            };

            if (index < ghostSpawns.Count)
            {
                ghost.X = ghostSpawns[index].Col;
                ghost.Y = ghostSpawns[index].Row;
                ghost.CurrentDirection = Direction.None;
                ghost.State = GhostStateEnum.Normal;
                ghost.RespawnTimer = 0;
            }
        }
    }

    private void CheckGameEnd()
    {
        if (_lives <= 0)
        {
            // Game Over
        }

        if (_collectibles.All(c => !c.IsActive))
        {
            // Level Complete
        }
    }

    public GameStateMessage GetState()
    {
        return new GameStateMessage
        {
            PacmanPosition = _pacman != null ? new EntityPosition
            {
                X = _pacman.X,
                Y = _pacman.Y,
                Direction = _pacman.CurrentDirection
            } : null,
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
