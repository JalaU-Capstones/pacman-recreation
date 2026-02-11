using PacmanGame.Helpers;
using PacmanGame.Models.Entities;
using PacmanGame.Models.Enums;
using PacmanGame.Services.Interfaces;
using PacmanGame.Services.AI;
using PacmanGame.Services.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;
using PacmanGame.Views;

namespace PacmanGame.Services;

/// <summary>
/// Main game engine managing game loop and logic
/// </summary>
public class GameEngine : IGameEngine, IGameEngineInternal
{
    private readonly ILogger _logger;
    private readonly IMapLoader _mapLoader;
    private readonly ISpriteManager _spriteManager;
    private readonly IAudioManager _audioManager;
    private readonly ICollisionDetector _collisionDetector;
    private readonly Random _random;

    private bool _isRunning;
    private bool _isPaused;
    private TileType[,] _map;
    private Pacman _pacman;
    private List<Ghost> _ghosts;
    private List<Collectible> _collectibles;
    private float _animationAccumulator;
    private int _ghostsEatenThisRound;
    private int _currentLevel;
    private float _ghostRespawnTime;

    // AI specific fields
    private readonly Dictionary<GhostType, IGhostAI> _ghostAIs;
    private readonly AStarPathfinder _pathfinder;
    private bool _isChaseMode = false;
    private float _modeTimer = 0f;
    private float _chaseDuration;
    private float _scatterDuration;

    // Ghost house release logic
    private float _ghostReleaseTimer = 0f;
    private int _nextGhostToRelease = 0;

    // Death animation
    private float _deathAnimationTimer = 0f;

    public event Action<int>? ScoreChanged;
    public event Action? LifeLost;
    public event Action? LevelComplete;
    public event Action? GameOver;
    public event Action? Victory;

    public TileType[,] Map => _map;
    public Pacman Pacman => _pacman;
    public List<Ghost> Ghosts => _ghosts;
    public List<Collectible> Collectibles => _collectibles;
    public ISpriteManager SpriteManager => _spriteManager;

    /// <summary>
    /// Create a new GameEngine instance
    /// </summary>
    public GameEngine(
        ILogger logger,
        IMapLoader mapLoader,
        ISpriteManager spriteManager,
        IAudioManager audioManager,
        ICollisionDetector collisionDetector)
    {
        _logger = logger;
        _mapLoader = mapLoader;
        _spriteManager = spriteManager;
        _audioManager = audioManager;
        _collisionDetector = collisionDetector;
        _random = new Random();

        _isRunning = false;
        _isPaused = false;
        _map = new TileType[0, 0];
        _pacman = new Pacman(0, 0);
        _ghosts = new List<Ghost>();
        _collectibles = new List<Collectible>();
        _animationAccumulator = 0f;
        _ghostsEatenThisRound = 0;
        _currentLevel = 1;
        _ghostRespawnTime = Constants.Level1GhostRespawnTime;

        // Initialize AI
        _pathfinder = new AStarPathfinder();
        _ghostAIs = new Dictionary<GhostType, IGhostAI>
        {
            { GhostType.Blinky, new BlinkyAI() },
            { GhostType.Pinky, new PinkyAI() },
            { GhostType.Inky, new InkyAI() },
            { GhostType.Clyde, new ClydeAI() }
        };
    }

    /// <summary>
    /// Load a level from the map files
    /// </summary>
    public void LoadLevel(int level)
    {
        try
        {
            _currentLevel = level;
            _logger.Info($"Loading level {level}...");
            string fileName = "level" + level + ".txt";

            _map = _mapLoader.LoadMap(fileName);
            _logger.Info($"Map loaded: {_map.GetLength(0)} rows × {_map.GetLength(1)} cols");

            _collectibles = _mapLoader.GetCollectibles(fileName)
                .Select(c => new Collectible(c.Col, c.Row, c.Type))
                .ToList();
            _logger.Info($"{_collectibles.Count} collectibles loaded");

            var pacmanSpawn = _mapLoader.GetPacmanSpawn(fileName);
            _pacman = new Pacman(pacmanSpawn.Col, pacmanSpawn.Row);
            _logger.Info($"Pac-Man spawned at ({pacmanSpawn.Col}, {pacmanSpawn.Row})");

            var ghostSpawns = _mapLoader.GetGhostSpawns(fileName);
            _ghosts = new List<Ghost>();

            ApplyDifficultySettings(level);

            GhostType[] ghostTypes = { GhostType.Blinky, GhostType.Pinky, GhostType.Inky, GhostType.Clyde };
            for (int i = 0; i < ghostSpawns.Count && i < ghostTypes.Length; i++)
            {
                var spawn = ghostSpawns[i];
                Ghost ghost = new Ghost(spawn.Col, spawn.Row, ghostTypes[i]);
                ghost.ExactX = spawn.Col;
                ghost.ExactY = spawn.Row;
                ghost.State = GhostState.InHouse; // Start all ghosts in house
                // Stagger release timers so ghosts leave sequentially
                ghost.ReleaseTimer = 0.5f + i * Constants.GhostReleaseInterval;
                _ghosts.Add(ghost);
            }
            _logger.Info($"{_ghosts.Count} ghosts spawned (in house)");

            _ghostsEatenThisRound = 0;
            _modeTimer = 0f;
            _isChaseMode = false; // Start in Scatter mode

            _spriteManager.Initialize();
            _audioManager.Initialize();
        }
        catch (Exception ex)
        {
            _logger.Error("Error loading level", ex);
            throw;
        }
    }

    private void ApplyDifficultySettings(int level)
    {
        float speedMultiplier = Constants.Level1GhostSpeedMultiplier;
        _pacman.PowerPelletDuration = Constants.Level1PowerPelletDuration;
        _chaseDuration = Constants.Level1ChaseDuration;
        _scatterDuration = Constants.Level1ScatterDuration;
        _ghostRespawnTime = Constants.Level1GhostRespawnTime;
        _pacman.Speed = Constants.PacmanSpeed;

        if (level == 2)
        {
            speedMultiplier = Constants.Level2GhostSpeedMultiplier;
            _pacman.PowerPelletDuration = Constants.Level2PowerPelletDuration;
            _chaseDuration = Constants.Level2ChaseDuration;
            _scatterDuration = Constants.Level2ScatterDuration;
            _ghostRespawnTime = Constants.Level2GhostRespawnTime;
        }
        else if (level >= 3)
        {
            speedMultiplier = Constants.Level3GhostSpeedMultiplier;
            _pacman.PowerPelletDuration = Constants.Level3PowerPelletDuration;
            _chaseDuration = Constants.Level3ChaseDuration;
            _scatterDuration = Constants.Level3ScatterDuration;
            _ghostRespawnTime = Constants.Level3GhostRespawnTime;
            _pacman.Speed *= Constants.Level3PacmanSpeedMultiplier;
        }

        foreach (var ghost in _ghosts)
        {
            ghost.SpeedMultiplier = speedMultiplier;
        }
    }

    /// <summary>
    /// Start the game
    /// </summary>
    public void Start()
    {
        _isRunning = true;
        _isPaused = false;
        _logger.Info("Game started");
    }

    /// <summary>
    /// Stop the game
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        _isPaused = false;
        _logger.Info("Game stopped");
    }

    /// <summary>
    /// Pause the game
    /// </summary>
    public void Pause()
    {
        if (_isRunning && !_isPaused)
        {
            _isPaused = true;
            _logger.Info("Game paused");
        }
    }

    /// <summary>
    /// Resume a paused game
    /// </summary>
    public void Resume()
    {
        if (_isRunning && _isPaused)
        {
            _isPaused = false;
            _logger.Info("Game resumed");
        }
    }

    /// <summary>
    /// Set the desired direction for Pac-Man
    /// </summary>
    public void SetPacmanDirection(Direction direction)
    {
        if (_isRunning && !_isPaused && !_pacman.IsDying)
        {
            _pacman.NextDirection = direction;
        }
    }

    /// <summary>
    /// Update game logic for one frame
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!_isRunning || _isPaused)
            return;

        if (_pacman.IsDying)
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
        _deathAnimationTimer += deltaTime;
        if (_deathAnimationTimer >= 0.1f) // 100ms per frame
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
        _pacman.IsDying = false;
        _pacman.AnimationFrame = 0;
        LifeLost?.Invoke();
        if (_isRunning) // Check if game is still running (not game over)
        {
            ResetPositions();
        }
    }

    /// <summary>
    /// Update Pac-Man movement and animation
    /// </summary>
    private void UpdatePacman(float deltaTime)
    {
        // Try to turn into NextDirection first
        if (_pacman.NextDirection != Direction.None && _pacman.CanMove(_pacman.NextDirection, _map))
        {
            _pacman.CurrentDirection = _pacman.NextDirection;
            _pacman.NextDirection = Direction.None;
        }

        // Now try to advance in CurrentDirection
        if (_pacman.CurrentDirection != Direction.None && _pacman.CanMove(_pacman.CurrentDirection, _map))
        {
            (int dx, int dy) = GetDirectionDeltas(_pacman.CurrentDirection);
            _pacman.ExactX += dx * _pacman.Speed * deltaTime;
            _pacman.ExactY += dy * _pacman.Speed * deltaTime;

            // Tunnel wrapping
            if (_pacman.ExactX < 0) _pacman.ExactX = Constants.MapWidth - 1;
            else if (_pacman.ExactX >= Constants.MapWidth) _pacman.ExactX = 0;
            if (_pacman.ExactY < 0) _pacman.ExactY = Constants.MapHeight - 1;
            else if (_pacman.ExactY >= Constants.MapHeight) _pacman.ExactY = 0;

            // Update integer grid position
            int pX = (int)Math.Round(_pacman.ExactX);
            int pY = (int)Math.Round(_pacman.ExactY);

            // Ensure integer coordinates are within bounds
            if (pX >= Constants.MapWidth) pX = 0;
            else if (pX < 0) pX = Constants.MapWidth - 1;

            if (pY >= Constants.MapHeight) pY = 0;
            else if (pY < 0) pY = Constants.MapHeight - 1;

            _pacman.X = pX;
            _pacman.Y = pY;

            _pacman.IsMoving = true;
        }
        else
        {
            _pacman.IsMoving = false;
        }

        _pacman.UpdateInvulnerability(deltaTime);
    }

    /// <summary>
    /// Update all ghosts with advanced AI
    /// </summary>
    private void UpdateGhosts(float deltaTime)
    {
        foreach (var ghost in _ghosts)
        {
            UpdateGhost(ghost, deltaTime);
        }
    }

    /// <summary>
    /// Update a single ghost's movement
    /// </summary>
    private void UpdateGhost(Ghost ghost, float deltaTime)
    {
        // Handle ghost house states
        if (ghost.State == GhostState.InHouse)
        {
            ghost.ReleaseTimer -= deltaTime;
            if (ghost.ReleaseTimer <= 0f)
            {
                ghost.State = GhostState.ExitingHouse;
                ghost.CurrentDirection = Direction.Up; // Start moving up to exit
            }
            return; // Don't move yet if in house
        }

        // Check if ghost is centered on a tile to make a new decision.
        const float centeringThreshold = 0.03f;
        bool isCentered = Math.Abs(ghost.ExactX - ghost.X) < centeringThreshold && Math.Abs(ghost.ExactY - ghost.Y) < centeringThreshold;

        if (isCentered)
        {
            // Snap to grid center
            ghost.ExactX = ghost.X;
            ghost.ExactY = ghost.Y;

            Direction nextMove = GetNextGhostMove(ghost);
            ghost.CurrentDirection = nextMove;
        }

        // Apply movement based on current direction and speed
        if (ghost.CurrentDirection != Direction.None)
        {
            (int dx, int dy) = GetDirectionDeltas(ghost.CurrentDirection);
            ghost.ExactX += dx * ghost.GetSpeed() * deltaTime;
            ghost.ExactY += dy * ghost.GetSpeed() * deltaTime;

            // Tunnel wrapping for ghosts
            if (ghost.ExactX < 0) ghost.ExactX = Constants.MapWidth - 1;
            else if (ghost.ExactX >= Constants.MapWidth) ghost.ExactX = 0;
            if (ghost.ExactY < 0) ghost.ExactY = Constants.MapHeight - 1;
            else if (ghost.ExactY >= Constants.MapHeight) ghost.ExactY = 0;

            // Update integer grid position
            ghost.X = (int)Math.Round(ghost.ExactX);
            ghost.Y = (int)Math.Round(ghost.ExactY);
        }

        // Update timers for vulnerability and respawn
        ghost.UpdateVulnerability(deltaTime, _logger);
        if (ghost.State == GhostState.Eaten && ghost.RespawnTimer > 0f)
        {
            ghost.RespawnTimer -= deltaTime;
            if (ghost.RespawnTimer <= 0f)
            {
                ghost.Respawn(_logger);
                ghost.State = GhostState.ExitingHouse; // Make ghost exit the house after respawning
                ghost.ReleaseTimer = 0f; // No delay
            }
        }
    }

    private Direction GetNextGhostMove(Ghost ghost)
    {
        Direction nextMove = Direction.None;

        switch (ghost.State)
        {
            case GhostState.Eaten:
                nextMove = _pathfinder.FindPath(ghost.Y, ghost.X, ghost.SpawnY, ghost.SpawnX, _map, ghost, _logger);
                if (ghost.X == ghost.SpawnX && ghost.Y == ghost.SpawnY && ghost.RespawnTimer <= 0f)
                {
                    ghost.RespawnTimer = _ghostRespawnTime;
                }
                break;

            case GhostState.Vulnerable:
            case GhostState.Warning:
                var fleeMoves = new List<Direction> { Direction.Up, Direction.Down, Direction.Left, Direction.Right }
                    .Where(d => ghost.CanMove(d, _map))
                    .ToList();

                var nonReversingFleeMoves = fleeMoves.Where(d => d != GetOppositeDirection(ghost.CurrentDirection)).ToList();
                if (nonReversingFleeMoves.Any())
                {
                    fleeMoves = nonReversingFleeMoves;
                }

                if (fleeMoves.Any())
                {
                    Direction bestDirection = Direction.None;
                    float maxDistance = -1;

                    foreach (var direction in fleeMoves)
                    {
                        (int dx, int dy) = GetDirectionDeltas(direction);
                        int newX = ghost.X + dx;
                        int newY = ghost.Y + dy;
                        float distance = (newX - _pacman.X) * (newX - _pacman.X) + (newY - _pacman.Y) * (newY - _pacman.Y);

                        if (distance > maxDistance)
                        {
                            maxDistance = distance;
                            bestDirection = direction;
                        }
                    }
                    nextMove = bestDirection;
                }
                break;

            case GhostState.ExitingHouse:
                if (ghost.Y > Constants.GhostHouseExitY)
                {
                    nextMove = Direction.Up;
                }
                else
                {
                    ghost.State = GhostState.Normal;
                }
                break;

            case GhostState.Normal:
                if (_ghostAIs.TryGetValue(ghost.Type, out var ai))
                {
                    nextMove = ai.GetNextMove(ghost, _pacman, _map, _ghosts, _isChaseMode, _logger);
                }
                break;
        }

        if (nextMove == Direction.None || !ghost.CanMove(nextMove, _map))
        {
            _logger.Warning($"Ghost pathfinding failed for {ghost.Type} - using fallback random move");
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

    private void UpdateCollisions()
    {
        var collected = _collisionDetector.CheckPacmanCollectibleCollision(_pacman, _collectibles);
        if (collected != null)
        {
            collected.IsActive = false;
            if (collected.Type == CollectibleType.PowerPellet)
            {
                _logger.Info($"Power pellet collected - Ghosts vulnerable for {_pacman.PowerPelletDuration} seconds");
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
                int points = Constants.GhostPoints * (1 << (_ghostsEatenThisRound - 1));
                ScoreChanged?.Invoke(points);
                _logger.Info($"Eaten ghost {hitGhost.Type} for {points} points");
            }
            else if (hitGhost.State == GhostState.Normal)
            {
                _logger.Info("Ghost collision detected - Life lost");
                _audioManager.PlaySoundEffect("death");
                _pacman.IsDying = true;
            }
        }
    }

    private void ResetPositions()
    {
        _logger.Info("Resetting entity positions after life lost");
        var pacmanSpawn = _mapLoader.GetPacmanSpawn($"level{_currentLevel}.txt");
        _pacman.X = pacmanSpawn.Col;
        _pacman.Y = pacmanSpawn.Row;
        _pacman.ExactX = pacmanSpawn.Col;
        _pacman.ExactY = pacmanSpawn.Row;
        _pacman.CurrentDirection = Direction.None;
        _pacman.NextDirection = Direction.None;
        _pacman.IsInvulnerable = false;
        _pacman.InvulnerabilityTime = 0f;

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

    private void UpdateTimers(float deltaTime)
    {
        _animationAccumulator += deltaTime;
        if (_animationAccumulator >= Constants.AnimationSpeed)
        {
            _pacman.AnimationFrame = (_pacman.AnimationFrame + 1) % Constants.PacmanAnimationFrames;
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
            _logger.Info($"Ghost mode switched to: {(_isChaseMode ? "Chase" : "Scatter")}");
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
}
