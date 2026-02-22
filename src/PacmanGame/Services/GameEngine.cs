using Avalonia.Controls;
using Avalonia.Media.Imaging;
using PacmanGame.Helpers;
using PacmanGame.Models.CustomLevel;
using PacmanGame.Models.Entities;
using PacmanGame.Models.Enums;
using PacmanGame.Services.Interfaces;
using PacmanGame.Services.AI;
using PacmanGame.Services.Pathfinding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace PacmanGame.Services;

public class GameEngine : IGameEngine, IGameEngineInternal
{
    private readonly ILogger<GameEngine> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMapLoader _mapLoader;
    private readonly ISpriteManager _spriteManager;
    private readonly IAudioManager _audioManager;
    private readonly ICollisionDetector _collisionDetector;
    private readonly Random _random;

    private bool _isRunning;
    private bool _isPaused;
    private TileType[,] _map;
    private Pacman? _pacman;
    private List<Ghost> _ghosts;
    private List<Collectible> _collectibles;
    private float _animationAccumulator;
    private int _ghostsEatenThisRound;
    private int _currentLevel;
    private float _ghostRespawnTime;

    private bool _isCustomLevel;
    private (int Row, int Col)? _customPacmanSpawn;
    private (int DoorY, int CenterX, int ExitY)? _ghostDoorInfo;

    private int? _customFruitPoints;
    private int? _customGhostEatBasePoints;

    private readonly Dictionary<GhostType, IGhostAI> _ghostAIs;
    private readonly AStarPathfinder _pathfinder;
    private bool _isChaseMode = false;
    private float _modeTimer = 0f;
    private float _chaseDuration;
    private float _scatterDuration;

    private float _ghostReleaseTimer = 0f;

    private float _deathAnimationTimer = 0f;

    public event Action<int>? ScoreChanged;
    public event Action? LifeLost;
    public event Action? LevelComplete;
    public event Action? GameOver;
    public event Action? Victory;

    public TileType[,] Map => _map;
    public Pacman? Pacman { get => _pacman; set => _pacman = value; }
    public List<Ghost> Ghosts => _ghosts;
    public List<Collectible> Collectibles => _collectibles;
    public ISpriteManager SpriteManager => _spriteManager;
    public bool IsRunning => _isRunning;
    public bool IsPaused => _isPaused;
    public int CurrentLevel => _currentLevel;
    public bool IsMultiplayerClient { get; set; }

    public GameEngine(ILogger<GameEngine> logger, ILoggerFactory loggerFactory, IMapLoader mapLoader, ISpriteManager spriteManager, IAudioManager audioManager, ICollisionDetector collisionDetector)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _mapLoader = mapLoader;
        _spriteManager = spriteManager;
        _audioManager = audioManager;
        _collisionDetector = collisionDetector;
        _random = new Random();

        _isRunning = false;
        _isPaused = false;
        _map = new TileType[0, 0];
        _pacman = new Pacman(0, 0, _loggerFactory.CreateLogger<Pacman>());
        _ghosts = new List<Ghost>();
        _collectibles = new List<Collectible>();
        _animationAccumulator = 0f;
        _ghostsEatenThisRound = 0;
        _currentLevel = 1;
        _ghostRespawnTime = Constants.Level1GhostRespawnTime;

        _pathfinder = new AStarPathfinder();
        _ghostAIs = new Dictionary<GhostType, IGhostAI>
        {
            { GhostType.Blinky, new BlinkyAI() },
            { GhostType.Pinky, new PinkyAI() },
            { GhostType.Inky, new InkyAI() },
            { GhostType.Clyde, new ClydeAI() }
        };
    }

    public void LoadLevel(int level)
    {
        try
        {
            _isCustomLevel = false;
            _customPacmanSpawn = null;
            _ghostDoorInfo = null;
            _customFruitPoints = null;
            _customGhostEatBasePoints = null;

            _currentLevel = level;
            string fileName = "level" + level + ".txt";

            _map = _mapLoader.LoadMap(fileName);
            ComputeGhostDoorInfo();

            _collectibles = _mapLoader.GetCollectibles(fileName)
                .Select(c => new Collectible(c.Col, c.Row, c.Type))
                .ToList();

            var pacmanSpawn = _mapLoader.GetPacmanSpawn(fileName);
            _pacman = new Pacman(pacmanSpawn.Col, pacmanSpawn.Row, _loggerFactory.CreateLogger<Pacman>());

            if (_pacman != null && _map[_pacman.Y, _pacman.X] == TileType.Wall)
            {
                // Log error if needed, but removed for production cleanup
            }

            var ghostSpawns = _mapLoader.GetGhostSpawns(fileName);
            _ghosts = new List<Ghost>();

            GhostType[] ghostTypes = { GhostType.Blinky, GhostType.Pinky, GhostType.Inky, GhostType.Clyde };
            for (int i = 0; i < ghostSpawns.Count && i < ghostTypes.Length; i++)
            {
                var spawn = ghostSpawns[i];
                Ghost ghost = new Ghost(spawn.Col, spawn.Row, ghostTypes[i]);
                ghost.ExactX = spawn.Col;
                ghost.ExactY = spawn.Row;
                ghost.State = GhostState.InHouse;
                ghost.ReleaseTimer = 0.5f + i * Constants.GhostReleaseInterval;
                _ghosts.Add(ghost);
            }

            // Must be applied after ghosts are created so their speed multipliers are updated correctly.
            ApplyDifficultySettings(level);

            _ghostsEatenThisRound = 0;
            _modeTimer = 0f;
            _isChaseMode = false;

            _spriteManager.Initialize();
            _audioManager.Initialize();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading level");
            throw;
        }
    }

    public void LoadCustomLevel(string mapFilePath)
    {
        try
        {
            _isCustomLevel = true;
            _customPacmanSpawn = null;
            _ghostDoorInfo = null;
            _customFruitPoints = null;
            _customGhostEatBasePoints = null;

            if (string.IsNullOrWhiteSpace(mapFilePath))
            {
                throw new ArgumentException("Custom map path is required.", nameof(mapFilePath));
            }

            if (!File.Exists(mapFilePath))
            {
                throw new FileNotFoundException("Custom map file not found.", mapFilePath);
            }

            _currentLevel = 1;
            var lines = File.ReadAllLines(mapFilePath);

            if (lines.Length != Constants.MapHeight)
            {
                throw new InvalidOperationException(
                    $"Map height mismatch. Expected {Constants.MapHeight}, got {lines.Length}");
            }

            _map = new TileType[Constants.MapHeight, Constants.MapWidth];
            for (int row = 0; row < lines.Length; row++)
            {
                var line = lines[row];
                if (line.Length > Constants.MapWidth)
                {
                    throw new InvalidOperationException(
                        $"Map width mismatch on line {row + 1}. Expected max {Constants.MapWidth}, got {line.Length}");
                }

                for (int col = 0; col < Constants.MapWidth; col++)
                {
                    char c = col < line.Length ? line[col] : ' ';
                    _map[row, col] = CharToTileType(c);
                }
            }

            ComputeGhostDoorInfo();

            _collectibles = new List<Collectible>();
            for (int row = 0; row < lines.Length; row++)
            {
                for (int col = 0; col < lines[row].Length; col++)
                {
                    var type = CharToCollectibleType(lines[row][col]);
                    if (type.HasValue)
                    {
                        _collectibles.Add(new Collectible(col, row, type.Value));
                    }
                }
            }

            var pacmanSpawn = FindSpawn(lines, Constants.PacmanChar);
            _customPacmanSpawn = pacmanSpawn;
            _pacman = new Pacman(pacmanSpawn.Col, pacmanSpawn.Row, _loggerFactory.CreateLogger<Pacman>());

            var ghostSpawns = FindAllSpawns(lines, Constants.GhostChar);
            _ghosts = new List<Ghost>();

            GhostType[] ghostTypes = { GhostType.Blinky, GhostType.Pinky, GhostType.Inky, GhostType.Clyde };
            for (int i = 0; i < ghostSpawns.Count && i < ghostTypes.Length; i++)
            {
                var spawn = ghostSpawns[i];
                Ghost ghost = new Ghost(spawn.Col, spawn.Row, ghostTypes[i]);
                ghost.ExactX = spawn.Col;
                ghost.ExactY = spawn.Row;
                ghost.State = GhostState.InHouse;
                ghost.ReleaseTimer = 0.5f + i * Constants.GhostReleaseInterval;
                _ghosts.Add(ghost);
            }

            // Must be applied after ghosts are created so their speed multipliers are updated correctly.
            ApplyDifficultySettings(_currentLevel);

            _ghostsEatenThisRound = 0;
            _modeTimer = 0f;
            _isChaseMode = false;

            _spriteManager.Initialize();
            _audioManager.Initialize();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading custom level from {Path}", mapFilePath);
            throw;
        }
    }

    public void ApplyCustomLevelSettings(LevelConfig levelConfig)
    {
        if (levelConfig == null) throw new ArgumentNullException(nameof(levelConfig));

        if (_pacman != null)
        {
            _pacman.PowerPelletDuration = Math.Max(1, levelConfig.FrightenedDuration);
            _pacman.Speed = 3.0f * (float)Math.Clamp(levelConfig.PacmanSpeedMultiplier, LevelConfig.MinSpeedMultiplier, LevelConfig.MaxSpeedMultiplier);
        }

        var ghostMultiplier = (float)Math.Clamp(levelConfig.GhostSpeedMultiplier, LevelConfig.MinSpeedMultiplier, LevelConfig.MaxSpeedMultiplier);
        foreach (var ghost in _ghosts)
        {
            ghost.SpeedMultiplier *= ghostMultiplier;
        }

        _customFruitPoints = Math.Max(1, levelConfig.FruitPoints);
        _customGhostEatBasePoints = Math.Max(1, levelConfig.GhostEatPoints);

        // Apply fruit points to any fruit already present.
        foreach (var collectible in _collectibles)
        {
            if (collectible.Type == CollectibleType.Cherry && _customFruitPoints.HasValue)
            {
                collectible.Points = _customFruitPoints.Value;
            }
        }
    }

    private static TileType CharToTileType(char c)
    {
        return c switch
        {
            Constants.WallChar => TileType.Wall,
            Constants.GhostDoorChar => TileType.GhostDoor,
            _ => TileType.Empty
        };
    }

    private static CollectibleType? CharToCollectibleType(char c)
    {
        return c switch
        {
            Constants.SmallDotChar => CollectibleType.SmallDot,
            Constants.PowerPelletChar or 'O' => CollectibleType.PowerPellet,
            Constants.FruitChar => CollectibleType.Cherry,
            _ => null
        };
    }

    private static (int Row, int Col) FindSpawn(string[] lines, char spawnChar)
    {
        for (int row = 0; row < lines.Length; row++)
        {
            int col = lines[row].IndexOf(spawnChar);
            if (col >= 0)
            {
                return (row, col);
            }
        }

        throw new InvalidOperationException($"Spawn point '{spawnChar}' not found in custom map.");
    }

    private static List<(int Row, int Col)> FindAllSpawns(string[] lines, char spawnChar)
    {
        var spawns = new List<(int Row, int Col)>();
        for (int row = 0; row < lines.Length; row++)
        {
            for (int col = 0; col < lines[row].Length; col++)
            {
                if (lines[row][col] == spawnChar)
                {
                    spawns.Add((row, col));
                }
            }
        }

        if (spawns.Count == 0)
        {
            throw new InvalidOperationException($"No spawn points '{spawnChar}' found in custom map.");
        }

        return spawns;
    }

    private void ApplyDifficultySettings(int level)
    {
        float speedMultiplier = Constants.Level1GhostSpeedMultiplier;
        if (_pacman != null)
        {
            _pacman.PowerPelletDuration = Constants.Level1PowerPelletDuration;
            _pacman.Speed = Constants.Level1PacmanSpeed;
        }
        _chaseDuration = Constants.Level1ChaseDuration;
        _scatterDuration = Constants.Level1ScatterDuration;
        _ghostRespawnTime = Constants.Level1GhostRespawnTime;

        if (level == 2)
        {
            speedMultiplier = Constants.Level2GhostSpeedMultiplier;
            if (_pacman != null)
            {
                _pacman.PowerPelletDuration = Constants.Level2PowerPelletDuration;
                _pacman.Speed = Constants.Level2PacmanSpeed;
            }
            _chaseDuration = Constants.Level2ChaseDuration;
            _scatterDuration = Constants.Level2ScatterDuration;
            _ghostRespawnTime = Constants.Level2GhostRespawnTime;
        }
        else if (level >= 3)
        {
            speedMultiplier = Constants.Level3GhostSpeedMultiplier;
            if (_pacman != null)
            {
                _pacman.PowerPelletDuration = Constants.Level3PowerPelletDuration;
                _pacman.Speed = Constants.Level3PacmanSpeed;
            }
            _chaseDuration = Constants.Level3ChaseDuration;
            _scatterDuration = Constants.Level3ScatterDuration;
            _ghostRespawnTime = Constants.Level3GhostRespawnTime;
        }

        foreach (var ghost in _ghosts)
        {
            float baseGhostSpeed = level switch
            {
                1 => Constants.Level1GhostBaseSpeed,
                2 => Constants.Level2GhostBaseSpeed,
                _ => Constants.Level3GhostBaseSpeed
            };

            ghost.SpeedMultiplier = baseGhostSpeed / Constants.GhostNormalSpeed;
            ghost.Speed *= speedMultiplier;
        }
    }

    public void Start()
    {
        _isRunning = true;
        _isPaused = false;
    }

    public void Stop()
    {
        _isRunning = false;
        _isPaused = false;
    }

    public void Pause()
    {
        if (_isRunning && !_isPaused)
        {
            _isPaused = true;
        }
    }

    public void Resume()
    {
        if (_isRunning && _isPaused)
        {
            _isPaused = false;
        }
    }

    public void SetPacmanDirection(Direction direction)
    {
        if (Pacman == null)
        {
            return;
        }

        Pacman.NextDirection = direction;
    }

    public void Update(float deltaTime)
    {
        if (!_isRunning || _isPaused)
            return;

        if (IsMultiplayerClient)
        {
            UpdateTimers(deltaTime);
            return;
        }

        if (_pacman != null && _pacman.IsDying)
        {
            UpdateDeathAnimation(deltaTime);
            return;
        }

        UpdatePacman(deltaTime);
        UpdateGhosts(deltaTime);
        UpdateCollisions();
        UpdateTimers(deltaTime);
    }

    private void UpdateDeathAnimation(float deltaTime)
    {
        if (_pacman == null) return;

        _deathAnimationTimer += deltaTime;
        if (_deathAnimationTimer >= 0.1f)
        {
            _pacman.AnimationFrame++;
            _deathAnimationTimer = 0;
            if (_pacman.AnimationFrame >= Constants.DeathAnimationFrames)
            {
                FinishDeathSequence();
            }
        }
    }

    private void FinishDeathSequence()
    {
        if (_pacman != null)
        {
            _pacman.IsDying = false;
            _pacman.AnimationFrame = 0;
        }
        LifeLost?.Invoke();
        if (_isRunning)
        {
            ResetPositions();
        }
    }

    private void UpdatePacman(float deltaTime)
    {
        if (Pacman == null)
        {
            return;
        }

        if (Pacman.NextDirection != Direction.None)
        {
            var (newX, newY) = GetNextPosition(Pacman.X, Pacman.Y, Pacman.NextDirection);

            if (CanMoveTo(newX, newY))
            {
                Pacman.CurrentDirection = Pacman.NextDirection;
            }
        }

        if (Pacman.CurrentDirection != Direction.None)
        {
            var (moveX, moveY) = GetNextPosition(Pacman.X, Pacman.Y, Pacman.CurrentDirection);

            if (CanMoveTo(moveX, moveY))
            {
                Pacman.X = moveX;
                Pacman.Y = moveY;
                Pacman.ExactX = moveX;
                Pacman.ExactY = moveY;
            }
        }
    }

    private (int x, int y) GetNextPosition(int currentX, int currentY, Direction direction)
    {
        var result = direction switch
        {
            Direction.Up => (currentX, currentY - 1),
            Direction.Down => (currentX, currentY + 1),
            Direction.Left => (currentX - 1, currentY),
            Direction.Right => (currentX + 1, currentY),
            _ => (currentX, currentY)
        };

        return result;
    }

    private bool CanMoveTo(int x, int y)
    {
        if (x < 0 || x >= Constants.MapWidth || y < 0 || y >= Constants.MapHeight)
        {
            return false;
        }

        var tile = _map[y, x];

        // Pac-Man uses this movement check; the ghost door is not passable for Pac-Man.
        if (tile == TileType.Wall || tile == TileType.GhostDoor)
        {
            return false;
        }

        return true;
    }

    private void UpdateGhosts(float deltaTime)
    {
        _ghostReleaseTimer += deltaTime;
        foreach (var ghost in _ghosts)
        {
            UpdateGhost(ghost, deltaTime);
        }
    }

    private void UpdateGhost(Ghost ghost, float deltaTime)
    {
        if (ghost.State == GhostState.InHouse)
        {
            if (_ghostReleaseTimer >= ghost.ReleaseTimer)
            {
                ghost.State = GhostState.ExitingHouse;
                ghost.CurrentDirection = Direction.Up;
            }
            return;
        }

        const float snapThreshold = 0.03f;
        float decisionThreshold = ghost.State == GhostState.Eaten ? 0.12f : snapThreshold;
        bool atDecisionCenter =
            Math.Abs(ghost.ExactX - ghost.X) < decisionThreshold &&
            Math.Abs(ghost.ExactY - ghost.Y) < decisionThreshold;
        bool atSnapCenter =
            Math.Abs(ghost.ExactX - ghost.X) < snapThreshold &&
            Math.Abs(ghost.ExactY - ghost.Y) < snapThreshold;

        if (atSnapCenter)
        {
            ghost.ExactX = ghost.X;
            ghost.ExactY = ghost.Y;
        }

        if (atDecisionCenter)
        {
            Direction nextMove = GetNextGhostMove(ghost);
            ghost.CurrentDirection = nextMove;
        }

        if (ghost.CurrentDirection != Direction.None)
        {
            (int dx, int dy) = GetDirectionDeltas(ghost.CurrentDirection);
            float nextX = ghost.ExactX + dx * ghost.GetSpeed() * deltaTime;
            float nextY = ghost.ExactY + dy * ghost.GetSpeed() * deltaTime;

            // Validate movement to prevent passing through walls
            // Check the tile ahead based on direction
            int checkX = (int)Math.Round(nextX);
            int checkY = (int)Math.Round(nextY);

            // Special handling for ghost house gate
            bool isGate = false;
            if (checkX >= 0 && checkX < Constants.MapWidth && checkY >= 0 && checkY < Constants.MapHeight)
            {
                isGate = _map[checkY, checkX] == TileType.GhostDoor;
            }

            // Allow movement if valid tile OR if it's the gate and ghost is entering/exiting
            bool canPass = CanMoveTo(checkX, checkY) || (isGate && (ghost.State == GhostState.Eaten || ghost.State == GhostState.ExitingHouse));

            if (canPass)
            {
                ghost.ExactX = nextX;
                ghost.ExactY = nextY;

                if (ghost.ExactX < 0) ghost.ExactX = Constants.MapWidth - 1;
                else if (ghost.ExactX >= Constants.MapWidth) ghost.ExactX = 0;
                if (ghost.ExactY < 0) ghost.ExactY = Constants.MapHeight - 1;
                else if (ghost.ExactY >= Constants.MapHeight) ghost.ExactY = 0;

                ghost.X = (int)Math.Round(ghost.ExactX);
                ghost.Y = (int)Math.Round(ghost.ExactY);
            }
            else
            {
                // Hit a wall, snap to grid
                ghost.ExactX = ghost.X;
                ghost.ExactY = ghost.Y;
            }
        }

        ghost.UpdateVulnerability(deltaTime, _logger);
        if (ghost.State == GhostState.Eaten && ghost.RespawnTimer > 0f)
        {
            ghost.RespawnTimer -= deltaTime;
            if (ghost.RespawnTimer <= 0f)
            {
                ghost.Respawn(_logger);
                ghost.State = GhostState.ExitingHouse;
                ghost.ReleaseTimer = 0f;
            }
        }
    }

    private Direction GetNextGhostMove(Ghost ghost)
    {
        if (!ghost.IsAIControlled) return ghost.CurrentDirection;

        // If an eaten ghost has already reached home and is waiting on its respawn timer, hold position.
        // Without this, the global fallback logic could make it wander during the countdown.
        if (ghost.State == GhostState.Eaten && ghost.RespawnTimer > 0f)
        {
            return Direction.None;
        }

        Direction nextMove = Direction.None;

        switch (ghost.State)
        {
            case GhostState.Eaten:
                var (homeY, homeX) = GetGhostHouseHomeTile(ghost);

                // If we're close enough to home (but missed the exact integer center due to dt),
                // snap to the home tile and start the respawn countdown.
                if (ghost.RespawnTimer <= 0f &&
                    Math.Abs(ghost.ExactX - homeX) < 0.2f &&
                    Math.Abs(ghost.ExactY - homeY) < 0.2f)
                {
                    ghost.ExactX = homeX;
                    ghost.ExactY = homeY;
                    ghost.X = homeX;
                    ghost.Y = homeY;
                    ghost.CurrentDirection = Direction.None;
                    ghost.RespawnTimer = _ghostRespawnTime;
                    _logger.LogDebug("Eaten ghost {Ghost} reached home tile ({HomeX},{HomeY}); starting respawn timer {Seconds}s.", ghost.GetName(), homeX, homeY, _ghostRespawnTime);
                    return Direction.None;
                }

                nextMove = _pathfinder.FindPath(ghost.Y, ghost.X, homeY, homeX, _map, ghost, _logger);

                // If A* can't find a path (or returns None), fall back to a greedy step toward home.
                // This prevents "stuck eyes" where the ghost never starts moving after being eaten.
                if (nextMove == Direction.None && (ghost.X != homeX || ghost.Y != homeY))
                {
                    nextMove = ChooseGreedyMoveToward(ghost, homeY, homeX);
                    _logger.LogDebug(
                        "Eaten ghost {Ghost} A* returned None; greedy move {Move} toward ({HomeX},{HomeY}) from ({X},{Y})",
                        ghost.GetName(), nextMove, homeX, homeY, ghost.X, ghost.Y);
                }

                // Start respawn countdown only after returning home (ghost house base).
                if (ghost.X == homeX && ghost.Y == homeY && ghost.RespawnTimer <= 0f)
                {
                    ghost.RespawnTimer = _ghostRespawnTime;
                }
                break;

            case GhostState.Vulnerable:
            case GhostState.Warning:
                // Frightened: random movement (prefer not reversing).
                var frightenedMoves = new List<Direction> { Direction.Up, Direction.Down, Direction.Left, Direction.Right }
                    .Where(d => ghost.CanMove(d, _map))
                    .ToList();

                var nonReversing = frightenedMoves.Where(d => d != GetOppositeDirection(ghost.CurrentDirection)).ToList();
                if (nonReversing.Any())
                {
                    frightenedMoves = nonReversing;
                }

                if (frightenedMoves.Any())
                {
                    nextMove = frightenedMoves[_random.Next(frightenedMoves.Count)];
                }
                break;

            case GhostState.ExitingHouse:
                if (_ghostDoorInfo is { } info)
                {
                    // Move towards the door center, then step through the gate and become Normal outside.
                    if (ghost.Y > info.DoorY)
                    {
                        nextMove = Direction.Up;
                    }
                    else if (ghost.Y < info.DoorY)
                    {
                        nextMove = Direction.Down;
                    }
                    else
                    {
                        if (ghost.X < info.CenterX) nextMove = Direction.Right;
                        else if (ghost.X > info.CenterX) nextMove = Direction.Left;
                        else nextMove = Direction.Up;
                    }

                    if (ghost.Y <= info.ExitY && ghost.X == info.CenterX)
                    {
                        ghost.State = GhostState.Normal;
                        nextMove = Direction.None;
                    }
                }
                else
                {
                    // Legacy fallback for built-in layouts.
                    if (ghost.Y > Constants.GhostHouseExitY)
                    {
                        nextMove = Direction.Up;
                    }
                    else
                    {
                        ghost.State = GhostState.Normal;
                    }
                }
                break;

            case GhostState.Normal:
                if (_pacman != null && _ghostAIs.TryGetValue(ghost.Type, out var ai))
                {
                    nextMove = ai.GetNextMove(ghost, _pacman, _map, _ghosts, _isChaseMode, _loggerFactory.CreateLogger(ai.GetType()));
                }
                break;
        }

        if (nextMove == Direction.None || !ghost.CanMove(nextMove, _map))
        {
            if (ghost.CanMove(ghost.CurrentDirection, _map))
            {
                return ghost.CurrentDirection;
            }

            var fallbackMoves = new List<Direction> { Direction.Up, Direction.Down, Direction.Left, Direction.Right }
                .Where(d => ghost.CanMove(d, _map) && d != GetOppositeDirection(ghost.CurrentDirection))
                .ToList();

            if (fallbackMoves.Any()) return fallbackMoves[_random.Next(fallbackMoves.Count)];

            if (ghost.CanMove(GetOppositeDirection(ghost.CurrentDirection), _map))
            {
                return GetOppositeDirection(ghost.CurrentDirection);
            }
        }

        return nextMove;
    }

    private Direction ChooseGreedyMoveToward(Ghost ghost, int targetY, int targetX)
    {
        // Prefer the move that reduces Manhattan distance and is legal.
        // Avoid immediate reversal when possible to keep the eyes moving smoothly.
        var candidates = new List<Direction> { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
        var nonReversing = candidates.Where(d => d != GetOppositeDirection(ghost.CurrentDirection)).ToList();
        if (nonReversing.Any())
        {
            candidates = nonReversing;
        }

        Direction best = Direction.None;
        int bestDist = int.MaxValue;

        foreach (var dir in candidates)
        {
            if (!ghost.CanMove(dir, _map)) continue;

            var (nx, ny) = GetNextPosition(ghost.X, ghost.Y, dir);
            // Wrap tunnels like the movement layer does.
            if (nx < 0) nx = Constants.MapWidth - 1;
            else if (nx >= Constants.MapWidth) nx = 0;
            if (ny < 0) ny = Constants.MapHeight - 1;
            else if (ny >= Constants.MapHeight) ny = 0;

            var dist = Math.Abs(ny - targetY) + Math.Abs(nx - targetX);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = dir;
            }
        }

        if (best != Direction.None)
        {
            return best;
        }

        // Last resort: allow reversal.
        var opposite = GetOppositeDirection(ghost.CurrentDirection);
        if (opposite != Direction.None && ghost.CanMove(opposite, _map))
        {
            return opposite;
        }

        return Direction.None;
    }

    private (int HomeY, int HomeX) GetGhostHouseHomeTile(Ghost ghost)
    {
        // Prefer the computed ghost-door center; return to the tile "inside" the house (opposite the exit side).
        if (_ghostDoorInfo is { } info)
        {
            // exitY is the outside tile relative to doorY; inside is the opposite direction.
            var insideY = info.DoorY + (info.DoorY - info.ExitY);
            insideY = Math.Clamp(insideY, 0, Constants.MapHeight - 1);
            var x = Math.Clamp(info.CenterX, 0, Constants.MapWidth - 1);

            // If the computed inside tile is a wall (unexpected), fall back to the door tile.
            if (_map[insideY, x] == TileType.Wall)
            {
                insideY = info.DoorY;
            }

            return (insideY, x);
        }

        // Fallback: use the ghost's spawn tile (typically inside the house on built-in maps).
        return (ghost.SpawnY, ghost.SpawnX);
    }

    private void UpdateCollisions()
    {
        if (_pacman == null) return;

        var collected = _collisionDetector.CheckPacmanCollectibleCollision(_pacman, _collectibles);
        if (collected != null)
        {
            collected.IsActive = false;
            if (collected.Type == CollectibleType.PowerPellet)
            {
                _pacman.ActivatePowerPellet();
                _ghostsEatenThisRound = 0;
                foreach (var ghost in _ghosts.Where(g => g.State == GhostState.Normal))
                {
                    ghost.MakeVulnerable(_pacman.PowerPelletDuration, _logger);
                }
                _audioManager.PlaySoundEffect("eat-power-pellet");
            }
            else
            {
                _audioManager.PlaySoundEffect("chomp");
            }
            ScoreChanged?.Invoke(collected.Points);

            if (_collectibles.All(c => !c.IsActive))
            {
                if (_currentLevel == 3)
                {
                    Victory?.Invoke();
                }
                else
                {
                    LevelComplete?.Invoke();
                }
            }
        }

        var hitGhost = _collisionDetector.CheckPacmanGhostCollision(_pacman, _ghosts);
        if (hitGhost != null)
        {
            if (hitGhost.State == GhostState.Vulnerable || hitGhost.State == GhostState.Warning)
            {
                hitGhost.GetEaten();
                _audioManager.PlaySoundEffect("eat-ghost");
                _ghostsEatenThisRound++;
                int basePoints = _customGhostEatBasePoints ?? Constants.GhostPoints;
                int points = basePoints * (1 << (_ghostsEatenThisRound - 1));
                ScoreChanged?.Invoke(points);
            }
            else if (hitGhost.State == GhostState.Normal)
            {
                _audioManager.PlaySoundEffect("death");
                _pacman.IsDying = true;
            }
        }
    }

    private void ResetPositions()
    {
        var pacmanSpawn = _isCustomLevel && _customPacmanSpawn.HasValue
            ? _customPacmanSpawn.Value
            : _mapLoader.GetPacmanSpawn($"level{_currentLevel}.txt");
        if (_pacman != null)
        {
            _pacman.X = pacmanSpawn.Col;
            _pacman.Y = pacmanSpawn.Row;
            _pacman.ExactX = pacmanSpawn.Col;
            _pacman.ExactY = pacmanSpawn.Row;
            _pacman.CurrentDirection = Direction.None;
            _pacman.NextDirection = Direction.None;
            _pacman.IsInvulnerable = false;
            _pacman.InvulnerabilityTime = 0f;
        }

        foreach (var ghost in _ghosts)
        {
            ghost.X = ghost.SpawnX;
            ghost.Y = ghost.SpawnY;
            ghost.ExactX = ghost.SpawnX;
            ghost.ExactY = ghost.SpawnY;
            ghost.CurrentDirection = Direction.None;
            ghost.State = GhostState.InHouse;
            ghost.VulnerableTime = 0f;
            ghost.RespawnTimer = 0f;
            ghost.ReleaseTimer = 0.5f + (int)ghost.Type * Constants.GhostReleaseInterval;
        }

        _ghostsEatenThisRound = 0;
        _modeTimer = 0f;
        _isChaseMode = false;
    }

    private void ComputeGhostDoorInfo()
    {
        // Find the most prominent ghost door row. Built-in maps use '-' (GhostDoorChar) for the gate.
        var doorsByRow = new Dictionary<int, List<int>>();
        for (int y = 0; y < Constants.MapHeight; y++)
        {
            for (int x = 0; x < Constants.MapWidth; x++)
            {
                if (_map[y, x] != TileType.GhostDoor) continue;
                if (!doorsByRow.TryGetValue(y, out var xs))
                {
                    xs = new List<int>();
                    doorsByRow[y] = xs;
                }
                xs.Add(x);
            }
        }

        if (doorsByRow.Count == 0)
        {
            _ghostDoorInfo = null;
            return;
        }

        var chosen = doorsByRow
            .OrderByDescending(kvp => kvp.Value.Count)
            .ThenBy(kvp => kvp.Key)
            .First();

        var doorY = chosen.Key;
        var xsSorted = chosen.Value.OrderBy(x => x).ToList();
        var centerX = xsSorted[xsSorted.Count / 2];

        var exitY = doorY - 1;
        if (exitY < 0)
        {
            exitY = doorY + 1;
        }

        _ghostDoorInfo = (doorY, centerX, exitY);
    }

    private void UpdateTimers(float deltaTime)
    {
        _animationAccumulator += deltaTime;
        if (_animationAccumulator >= Constants.AnimationSpeed)
        {
            if (_pacman != null)
            {
                _pacman.AnimationFrame = (_pacman.AnimationFrame + 1) % Constants.PacmanAnimationFrames;
            }
            foreach (var ghost in _ghosts)
            {
                ghost.AnimationFrame = (ghost.AnimationFrame + 1) % Constants.GhostAnimationFrames;
            }
            _animationAccumulator = 0f;
        }

        _modeTimer += deltaTime;
        float currentDuration = _isChaseMode ? _chaseDuration : _scatterDuration;
        if (_modeTimer >= currentDuration)
        {
            _isChaseMode = !_isChaseMode;
            _modeTimer = 0f;
        }
    }

    private static (int dx, int dy) GetDirectionDeltas(Direction direction)
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

    private static Direction GetOppositeDirection(Direction direction)
    {
        return direction switch
        {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            _ => Direction.None
        };
    }

    public void TriggerGameOver()
    {
        GameOver?.Invoke();
    }

    public void Render(Canvas canvas)
    {
        if (_spriteManager == null || _map.Length == 0) return;

        // Draw tiles
        for (int row = 0; row < Constants.MapHeight; row++)
        for (int col = 0; col < Constants.MapWidth; col++)
        {
            if (_map[row, col] == TileType.Wall)
            {
                string spriteName = GetWallSpriteName(row, col, _map);
                var sprite = _spriteManager.GetTileSprite(spriteName);
                if (sprite != null)
                    DrawImage(canvas, sprite, col * Constants.TileSize, row * Constants.TileSize, 0);
            }
        }

        // Draw collectibles
        foreach (var collectible in Collectibles.Where(c => c.IsActive))
        {
            string itemTypeKey = collectible.Type switch
            {
                CollectibleType.SmallDot => "dot",
                CollectibleType.PowerPellet => "power_pellet",
                CollectibleType.Cherry => "cherry",
                CollectibleType.Strawberry => "strawberry",
                _ => collectible.Type.ToString().ToLower()
            };
            var sprite = _spriteManager.GetItemSprite(itemTypeKey, Pacman != null ? Pacman.AnimationFrame % 2 : 0);
            if (sprite != null)
                DrawImage(canvas, sprite, collectible.X * Constants.TileSize, collectible.Y * Constants.TileSize, 1);
        }

        // Draw Pac-Man
        if (Pacman != null)
        {
            if (Pacman.IsDying)
            {
                var sprite = _spriteManager.GetDeathSprite(Pacman.AnimationFrame);
                if (sprite != null)
                    DrawImage(canvas, sprite, (int)(Pacman.ExactX * Constants.TileSize), (int)(Pacman.ExactY * Constants.TileSize), 2);
            }
            else
            {
                string direction = Pacman.CurrentDirection switch
                {
                    Direction.Up => "down",
                    Direction.Down => "up",
                    Direction.Left => "left",
                    Direction.Right => "right",
                    _ => "right"
                };
                var sprite = _spriteManager.GetPacmanSprite(direction, Pacman.AnimationFrame);
                if (sprite != null)
                    DrawImage(canvas, sprite, (int)(Pacman.ExactX * Constants.TileSize), (int)(Pacman.ExactY * Constants.TileSize), 2);
            }
        }

        // Draw ghosts
        foreach (var ghost in Ghosts)
        {
            CroppedBitmap? sprite = GetGhostSprite(ghost);
            if (sprite != null)
                DrawImage(canvas, sprite, (int)(ghost.ExactX * Constants.TileSize), (int)(ghost.ExactY * Constants.TileSize), 3);
        }
    }

    private CroppedBitmap? GetGhostSprite(Ghost ghost)
    {
        if (_spriteManager == null) return null;

        string direction = ghost.CurrentDirection.ToString().ToLower();
        if (ghost.CurrentDirection == Direction.None)
        {
            direction = "down";
        }

        return ghost.State switch
        {
            GhostState.Eaten => _spriteManager.GetGhostEyesSprite(direction),
            GhostState.Vulnerable => _spriteManager.GetVulnerableGhostSprite(0),
            GhostState.Warning => (ghost.AnimationFrame % 2 == 0) ? _spriteManager.GetVulnerableGhostSprite(1) : _spriteManager.GetVulnerableGhostSprite(0),
            _ => _spriteManager.GetGhostSprite(ghost.Type.ToString().ToLower(), direction, ghost.AnimationFrame)
        };
    }

    private void DrawImage(Canvas canvas, CroppedBitmap sprite, int x, int y, int zIndex)
    {
        var image = new Image
        {
            Source = sprite,
            Width = Constants.TileSize,
            Height = Constants.TileSize,
            ZIndex = zIndex
        };
        Canvas.SetLeft(image, x);
        Canvas.SetTop(image, y);
        canvas.Children.Add(image);
    }

    private string GetWallSpriteName(int row, int col, TileType[,] map)
    {
        bool hasUp = row > 0 && map[row - 1, col] == TileType.Wall;
        bool hasDown = row < Constants.MapHeight - 1 && map[row + 1, col] == TileType.Wall;
        bool hasLeft = col > 0 && map[row, col - 1] == TileType.Wall;
        bool hasRight = col < Constants.MapWidth - 1 && map[row, col + 1] == TileType.Wall;

        int neighbors = (hasUp ? 1 : 0) + (hasDown ? 1 : 0) + (hasLeft ? 1 : 0) + (hasRight ? 1 : 0);

        if (neighbors == 4) return "walls_cross";

        if (neighbors == 3)
        {
            if (!hasRight) return "walls_t_left";
            if (!hasLeft) return "walls_t_right";
            if (!hasDown) return "walls_t_up";
            if (!hasUp) return "walls_t_down";
        }

        if (neighbors == 2)
        {
            if (hasUp && hasDown) return "walls_vertical";
            if (hasLeft && hasRight) return "walls_horizontal";
            if (hasDown && hasRight) return "walls_corner_br";
            if (hasDown && hasLeft) return "walls_corner_bl";
            if (hasUp && hasRight) return "walls_corner_tr";
            if (hasUp && hasLeft) return "walls_corner_tl";
        }

        if (neighbors == 1)
        {
            if (hasUp) return "walls_end_down";
            if (hasDown) return "walls_end_up";
            if (hasLeft) return "walls_end_left";
            if (hasRight) return "walls_end_right";
        }

        return "special_empty";
    }
}

public interface IGameEngineInternal
{
    ISpriteManager SpriteManager { get; }
}
