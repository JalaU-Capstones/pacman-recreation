using Avalonia.Controls;
using Avalonia.Media.Imaging;
using PacmanGame.Helpers;
using PacmanGame.Models.Entities;
using PacmanGame.Models.Enums;
using PacmanGame.Services.Interfaces;
using PacmanGame.Services.AI;
using PacmanGame.Services.Pathfinding;
using System;
using System.Collections.Generic;
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
            _currentLevel = level;
            _logger.LogInformation($"Loading level {level}...");
            string fileName = "level" + level + ".txt";

            _map = _mapLoader.LoadMap(fileName);
            _logger.LogInformation($"Map loaded: {_map.GetLength(0)} rows × {_map.GetLength(1)} cols");

            _collectibles = _mapLoader.GetCollectibles(fileName)
                .Select(c => new Collectible(c.Col, c.Row, c.Type))
                .ToList();
            _logger.LogInformation($"{_collectibles.Count} collectibles loaded");

            var pacmanSpawn = _mapLoader.GetPacmanSpawn(fileName);
            _pacman = new Pacman(pacmanSpawn.Col, pacmanSpawn.Row, _loggerFactory.CreateLogger<Pacman>());
            _logger.LogInformation($"Pac-Man spawned at ({pacmanSpawn.Col}, {pacmanSpawn.Row})");

            if (_pacman != null && _map[Pacman.Y, Pacman.X] == TileType.Wall)
            {
                _logger.LogError("[GAMEENGINE] CRITICAL: Pacman spawned inside a WALL!");
            }

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
                ghost.State = GhostState.InHouse;
                ghost.ReleaseTimer = 0.5f + i * Constants.GhostReleaseInterval;
                _ghosts.Add(ghost);
            }
            _logger.LogInformation($"{_ghosts.Count} ghosts spawned (in house)");

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

    private void ApplyDifficultySettings(int level)
    {
        float speedMultiplier = Constants.Level1GhostSpeedMultiplier;
        if (_pacman != null)
        {
            _pacman.PowerPelletDuration = Constants.Level1PowerPelletDuration;
            _pacman.Speed = Constants.PacmanSpeed;
        }
        _chaseDuration = Constants.Level1ChaseDuration;
        _scatterDuration = Constants.Level1ScatterDuration;
        _ghostRespawnTime = Constants.Level1GhostRespawnTime;

        if (level == 2)
        {
            speedMultiplier = Constants.Level2GhostSpeedMultiplier;
            if (_pacman != null) _pacman.PowerPelletDuration = Constants.Level2PowerPelletDuration;
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
                _pacman.Speed *= Constants.Level3PacmanSpeedMultiplier;
            }
            _chaseDuration = Constants.Level3ChaseDuration;
            _scatterDuration = Constants.Level3ScatterDuration;
            _ghostRespawnTime = Constants.Level3GhostRespawnTime;
        }

        foreach (var ghost in _ghosts)
        {
            ghost.SpeedMultiplier = speedMultiplier;
        }
    }

    public void Start()
    {
        _isRunning = true;
        _isPaused = false;
        _logger.LogInformation("Game started");
    }

    public void Stop()
    {
        _isRunning = false;
        _isPaused = false;
        _logger.LogInformation("Game stopped");
    }

    public void Pause()
    {
        if (_isRunning && !_isPaused)
        {
            _isPaused = true;
            _logger.LogInformation("Game paused");
        }
    }

    public void Resume()
    {
        if (_isRunning && _isPaused)
        {
            _isPaused = false;
            _logger.LogInformation("Game resumed");
        }
    }

    public void SetPacmanDirection(Direction direction)
    {
        if (Pacman == null)
        {
            // _logger.LogError("[GAMEENGINE] Pacman is NULL!");
            return;
        }

        Pacman.NextDirection = direction;
    }

    public void Update(float deltaTime)
    {
        if (!_isRunning || _isPaused)
            return;

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
            // _logger.LogError("[GAMEENGINE] Pacman is NULL in UpdatePacman!");
            return;
        }

        // Try to change direction
        if (Pacman.NextDirection != Direction.None)
        {
            var (newX, newY) = GetNextPosition(Pacman.X, Pacman.Y, Pacman.NextDirection);

            if (CanMoveTo(newX, newY))
            {
                Pacman.CurrentDirection = Pacman.NextDirection;
            }
        }

        // Move in current direction
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
        // Check bounds
        if (x < 0 || x >= Constants.MapWidth || y < 0 || y >= Constants.MapHeight)
        {
            return false;
        }

        // CRITICAL: Map is indexed as [row, col] = [y, x], NOT [x, y]
        var tile = _map[y, x];

        if (tile == TileType.Wall)
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

        const float centeringThreshold = 0.03f;
        bool isCentered = Math.Abs(ghost.ExactX - ghost.X) < centeringThreshold && Math.Abs(ghost.ExactY - ghost.Y) < centeringThreshold;

        if (isCentered)
        {
            ghost.ExactX = ghost.X;
            ghost.ExactY = ghost.Y;

            Direction nextMove = GetNextGhostMove(ghost);
            ghost.CurrentDirection = nextMove;
        }

        if (ghost.CurrentDirection != Direction.None)
        {
            (int dx, int dy) = GetDirectionDeltas(ghost.CurrentDirection);
            ghost.ExactX += dx * ghost.GetSpeed() * deltaTime;
            ghost.ExactY += dy * ghost.GetSpeed() * deltaTime;

            if (ghost.ExactX < 0) ghost.ExactX = Constants.MapWidth - 1;
            else if (ghost.ExactX >= Constants.MapWidth) ghost.ExactX = 0;
            if (ghost.ExactY < 0) ghost.ExactY = Constants.MapHeight - 1;
            else if (ghost.ExactY >= Constants.MapHeight) ghost.ExactY = 0;

            ghost.X = (int)Math.Round(ghost.ExactX);
            ghost.Y = (int)Math.Round(ghost.ExactY);
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
                if (_pacman == null) break;
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
                if (_pacman != null && _ghostAIs.TryGetValue(ghost.Type, out var ai))
                {
                    nextMove = ai.GetNextMove(ghost, _pacman, _map, _ghosts, _isChaseMode, _loggerFactory.CreateLogger(ai.GetType()));
                }
                break;
        }

        if (nextMove == Direction.None || !ghost.CanMove(nextMove, _map))
        {
            _logger.LogWarning($"Ghost pathfinding failed for {ghost.Type} - using fallback random move");
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
        if (_pacman == null) return;

        var collected = _collisionDetector.CheckPacmanCollectibleCollision(_pacman, _collectibles);
        if (collected != null)
        {
            collected.IsActive = false;
            if (collected.Type == CollectibleType.PowerPellet)
            {
                _logger.LogInformation($"Power pellet collected - Ghosts vulnerable for {_pacman.PowerPelletDuration} seconds");
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
                _logger.LogInformation($"Eaten ghost {hitGhost.Type} for {points} points");
            }
            else if (hitGhost.State == GhostState.Normal)
            {
                _logger.LogInformation("Ghost collision detected - Life lost");
                _audioManager.PlaySoundEffect("death");
                _pacman.IsDying = true;
            }
        }
    }

    private void ResetPositions()
    {
        _logger.LogInformation("Resetting entity positions after life lost");
        var pacmanSpawn = _mapLoader.GetPacmanSpawn($"level{_currentLevel}.txt");
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
            _logger.LogInformation($"Ghost mode switched to: {(_isChaseMode ? "Chase" : "Scatter")}");
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
