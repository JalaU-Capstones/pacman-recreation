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

namespace PacmanGame.Services;

/// <summary>
/// Main game engine managing game loop, logic, and rendering
/// </summary>
public class GameEngine : IGameEngine
{
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

    // AI specific fields
    private readonly Dictionary<GhostType, IGhostAI> _ghostAIs;
    private readonly AStarPathfinder _pathfinder;
    private bool _isChaseMode = false;
    private float _modeTimer = 0f;

    // Ghost house release logic
    private float _ghostReleaseTimer = 0f;
    private int _nextGhostToRelease = 0;

    public event Action<int>? ScoreChanged;
    public event Action? LifeLost;
    public event Action? LevelComplete;
    public event Action? GameOver;

    public TileType[,] Map => _map;
    public Pacman Pacman => _pacman;
    public List<Ghost> Ghosts => _ghosts;
    public List<Collectible> Collectibles => _collectibles;

    /// <summary>
    /// Create a new GameEngine instance
    /// </summary>
    public GameEngine(
        IMapLoader mapLoader,
        ISpriteManager spriteManager,
        IAudioManager audioManager,
        ICollisionDetector collisionDetector)
    {
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
            Console.WriteLine($"📝 Loading level {level}...");
            string fileName = "level" + level + ".txt";
            Console.WriteLine($"📝 Map file: {fileName}");

            _map = _mapLoader.LoadMap(fileName);
            Console.WriteLine($"✅ Map loaded: {_map.GetLength(0)} rows × {_map.GetLength(1)} cols");

            _collectibles = _mapLoader.GetCollectibles(fileName)
                .Select(c => new Collectible(c.Col, c.Row, c.Type))
                .ToList();
            Console.WriteLine($"✅ {_collectibles.Count} collectibles loaded");

            var pacmanSpawn = _mapLoader.GetPacmanSpawn(fileName);
            _pacman = new Pacman(pacmanSpawn.Col, pacmanSpawn.Row);
            Console.WriteLine($"✅ Pac-Man spawned at ({pacmanSpawn.Col}, {pacmanSpawn.Row})");

            var ghostSpawns = _mapLoader.GetGhostSpawns(fileName);
            _ghosts = new List<Ghost>();

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
            Console.WriteLine($"✅ {_ghosts.Count} ghosts spawned (in house)");

            _ghostsEatenThisRound = 0;
            _modeTimer = 0f;
            _isChaseMode = false; // Start in Scatter mode

            _spriteManager.Initialize();
            Console.WriteLine("✅ Sprite manager initialized");
            _audioManager.Initialize();
            Console.WriteLine("✅ Audio manager initialized");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error loading level: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Start the game
    /// </summary>
    public void Start()
    {
        _isRunning = true;
        _isPaused = false;
    }

    /// <summary>
    /// Stop the game
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        _isPaused = false;
    }

    /// <summary>
    /// Pause the game
    /// </summary>
    public void Pause()
    {
        if (_isRunning && !_isPaused)
        {
            _isPaused = true;
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
        }
    }

    /// <summary>
    /// Set the desired direction for Pac-Man
    /// </summary>
    public void SetPacmanDirection(Direction direction)
    {
        if (_isRunning && !_isPaused)
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

        UpdatePacman(deltaTime);
        UpdateGhosts(deltaTime);
        UpdateCollisions();
        UpdateTimers(deltaTime);

        _updateFrameCounter++;
        if (_updateFrameCounter % 60 == 0)
        {
            // Console.WriteLine($"🎮 Update loop running - {_updateFrameCounter} frames processed");
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

        // Check if ghost is centered on a tile to make a new decision
        bool isCentered = Math.Abs(ghost.ExactX - ghost.X) < 0.05f && Math.Abs(ghost.ExactY - ghost.Y) < 0.05f;

        if (isCentered)
        {
            // Snap to grid center
            ghost.ExactX = ghost.X;
            ghost.ExactY = ghost.Y;

            Direction nextMove = GetNextGhostMove(ghost);
            if (ghost.State == GhostState.Vulnerable || ghost.State == GhostState.Warning)
            {
                Console.WriteLine($"[MOVE] {ghost.GetName()} vulnerable at ({ghost.X},{ghost.Y}), picked direction: {nextMove}");
            }
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
        ghost.UpdateVulnerability(deltaTime);
        if (ghost.State == GhostState.Eaten && ghost.RespawnTimer > 0f)
        {
            ghost.RespawnTimer -= deltaTime;
            if (ghost.RespawnTimer <= 0f)
            {
                ghost.Respawn();
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
                nextMove = _pathfinder.FindPath(ghost.Y, ghost.X, ghost.SpawnY, ghost.SpawnX, _map, ghost);
                if (ghost.X == ghost.SpawnX && ghost.Y == ghost.SpawnY && ghost.RespawnTimer <= 0f)
                {
                    ghost.RespawnTimer = Constants.GhostRespawnTime;
                }
                break;

            case GhostState.Vulnerable:
            case GhostState.Warning:
                var validMoves = new List<Direction> { Direction.Up, Direction.Down, Direction.Left, Direction.Right }
                    .Where(d => ghost.CanMove(d, _map))
                    .ToList();

                if (validMoves.Any())
                {
                    var nonReversingMoves = validMoves.Where(d => d != GetOppositeDirection(ghost.CurrentDirection)).ToList();
                    if (nonReversingMoves.Any())
                    {
                        nextMove = nonReversingMoves[_random.Next(nonReversingMoves.Count)];
                    }
                    else
                    {
                        // If the only way is to reverse, do it
                        nextMove = validMoves.First();
                    }
                }
                break;

            case GhostState.ExitingHouse:
                // Move up until past the exit Y-coordinate
                if (ghost.Y > Constants.GhostHouseExitY)
                {
                    nextMove = Direction.Up;
                }
                else // Reached outside the house
                {
                    ghost.State = GhostState.Normal;
                }
                break;

            case GhostState.Normal:
                if (_ghostAIs.TryGetValue(ghost.Type, out var ai))
                {
                    nextMove = ai.GetNextMove(ghost, _pacman, _map, _ghosts, _isChaseMode);
                }
                break;
        }

        // Fallback logic
        if (nextMove == Direction.None || !ghost.CanMove(nextMove, _map))
        {
            if (ghost.CanMove(ghost.CurrentDirection, _map))
            {
                return ghost.CurrentDirection;
            }

            var validMoves = new List<Direction> { Direction.Up, Direction.Down, Direction.Left, Direction.Right }
                .Where(d => ghost.CanMove(d, _map) && d != GetOppositeDirection(ghost.CurrentDirection))
                .ToList();

            if (validMoves.Any()) return validMoves[_random.Next(validMoves.Count)];

            if (ghost.CanMove(GetOppositeDirection(ghost.CurrentDirection), _map))
            {
                return GetOppositeDirection(ghost.CurrentDirection);
            }
        }

        return nextMove;
    }


    /// <summary>
    /// Handle all collision detection and reactions
    /// </summary>
    private void UpdateCollisions()
    {
        // Pac-Man vs Collectibles
        var collected = _collisionDetector.CheckPacmanCollectibleCollision(_pacman, _collectibles);
        if (collected != null)
        {
            collected.IsActive = false;
            if (collected.Type == CollectibleType.PowerPellet)
            {
                _pacman.ActivatePowerPellet();
                foreach (var ghost in _ghosts.Where(g => g.State == GhostState.Normal))
                {
                    ghost.MakeVulnerable();
                }
                _audioManager.PlaySoundEffect("eat-power-pellet");
            }
            else
            {
                _audioManager.PlaySoundEffect("chomp");
            }
            ScoreChanged?.Invoke(collected.Points);

            // Check if all collectibles are collected
            if (_collectibles.All(c => !c.IsActive))
            {
                LevelComplete?.Invoke();
            }
        }

        // Pac-Man vs Ghosts
        var hitGhost = _collisionDetector.CheckPacmanGhostCollision(_pacman, _ghosts);
        if (hitGhost != null)
        {
            if (_pacman.IsInvulnerable && (hitGhost.State == GhostState.Vulnerable || hitGhost.State == GhostState.Warning))
            {
                hitGhost.GetEaten();
                _audioManager.PlaySoundEffect("eat-ghost");
                _ghostsEatenThisRound++;
                int points = Constants.GhostPoints * (1 << (_ghostsEatenThisRound - 1));
                ScoreChanged?.Invoke(points);
            }
            else if (hitGhost.State == GhostState.Normal)
            {
                _audioManager.PlaySoundEffect("death");
                LifeLost?.Invoke();
                ResetPositions();
            }
        }
    }

    /// <summary>
    /// Reset Pac-Man and all ghosts to spawn positions
    /// </summary>
    private void ResetPositions()
    {
        // Reset Pac-Man (reload spawn position from map)
        // For now, we'll just reset to a safe location
        _pacman.X = 14;
        _pacman.Y = 24;
        _pacman.ExactX = 14;
        _pacman.ExactY = 24;
        _pacman.CurrentDirection = Direction.None;
        _pacman.NextDirection = Direction.None;
        _pacman.IsInvulnerable = false;
        _pacman.InvulnerabilityTime = 0f;

        // Reset ghosts
        foreach (var ghost in _ghosts)
        {
            ghost.X = ghost.SpawnX;
            ghost.Y = ghost.SpawnY;
            ghost.ExactX = ghost.SpawnX;
            ghost.ExactY = ghost.SpawnY;
            ghost.CurrentDirection = Direction.None;
            ghost.State = GhostState.InHouse; // Return to house
            ghost.VulnerableTime = 0f;
            ghost.RespawnTimer = 0f;
            // Re-stagger release timers
            ghost.ReleaseTimer = 0.5f + (int)ghost.Type * Constants.GhostReleaseInterval;
        }

        _ghostsEatenThisRound = 0;
        _modeTimer = 0f; // Reset mode timer
        _isChaseMode = false; // Start in Scatter mode
    }

    /// <summary>
    /// Update timers for animation and effects
    /// </summary>
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

        // Update mode timer
        _modeTimer += deltaTime;
        if (_modeTimer >= Constants.ModeToggleInterval)
        {
            _isChaseMode = !_isChaseMode;
            _modeTimer = 0f;
            Console.WriteLine($"👻 Ghost mode switched to: {(_isChaseMode ? "Chase" : "Scatter")}");
        }
    }

    /// <summary>
    /// Render the game to the canvas using sprites
    /// </summary>
    public void Render(Canvas canvas)
    {
        try
        {
            if (_map.Length == 0)
            {
                return;
            }

            canvas.Children.Clear();

            // Debug: Count rendered sprites
            int spriteCount = 0;

            // 1. Draw tiles (Z-index: 0)
            for (int row = 0; row < Constants.MapHeight; row++)
            {
                for (int col = 0; col < Constants.MapWidth; col++)
                {
                    if (_map[row, col] == TileType.Wall)
                    {
                        string spriteName = GetWallSpriteName(row, col);
                        var sprite = _spriteManager.GetTileSprite(spriteName);
                        if (sprite != null)
                        {
                            DrawImage(canvas, sprite, col * Constants.TileSize, row * Constants.TileSize, zIndex: 0);
                            spriteCount++;
                        }
                    }
                }
            }

            // 2. Draw collectibles (Z-index: 1)
            foreach (var collectible in _collectibles.Where(c => c.IsActive))
            {
                string itemType = collectible.Type switch
                {
                    CollectibleType.SmallDot => "dot",
                    CollectibleType.PowerPellet => "power_pellet",
                    CollectibleType.Cherry => "cherry",
                    CollectibleType.Strawberry => "strawberry",
                    _ => "dot"
                };
                var sprite = _spriteManager.GetItemSprite(itemType, _pacman.AnimationFrame % 2); // Use 0 or 1 for animation
                if (sprite != null)
                {
                    DrawImage(canvas, sprite, collectible.X * Constants.TileSize, collectible.Y * Constants.TileSize, zIndex: 1);
                    spriteCount++;
                }
            }

            // 3. Draw Pac-Man (Z-index: 2)
            if (!_pacman.IsDying)
            {
                string direction = _pacman.CurrentDirection switch
                {
                    Direction.Up => "down",
                    Direction.Down => "up",
                    Direction.Left => "left",
                    _ => "right"
                };
                var sprite = _spriteManager.GetPacmanSprite(direction, _pacman.AnimationFrame);
                if (sprite != null)
                {
                    DrawImage(canvas, sprite, (int)(_pacman.ExactX * Constants.TileSize), (int)(_pacman.ExactY * Constants.TileSize), zIndex: 2);
                    spriteCount++;
                }
            }

            // 4. Draw ghosts (Z-index: 3)
            foreach (var ghost in _ghosts)
            {
                CroppedBitmap? sprite = null;

                if (ghost.State == GhostState.Eaten)
                {
                    string eyeDirection = ghost.CurrentDirection switch
                    {
                        Direction.Up => "up",
                        Direction.Down => "down",
                        Direction.Left => "left",
                        _ => "right"
                    };
                    sprite = _spriteManager.GetGhostEyesSprite(eyeDirection);
                }
                else if (ghost.State == GhostState.Vulnerable || ghost.State == GhostState.Warning)
                {
                    sprite = _spriteManager.GetVulnerableGhostSprite(ghost.AnimationFrame);
                }
                else // Normal, InHouse, ExitingHouse
                {
                    string ghostTypeName = ghost.Type.ToString().ToLower();
                    string ghostDirection = ghost.CurrentDirection switch
                    {
                        Direction.Up => "up",
                        Direction.Down => "down",
                        Direction.Left => "left",
                        _ => "right"
                    };
                    sprite = _spriteManager.GetGhostSprite(ghostTypeName, ghostDirection, ghost.AnimationFrame);
                }

                if (sprite != null)
                {
                    DrawImage(canvas, sprite, (int)(ghost.ExactX * Constants.TileSize), (int)(ghost.ExactY * Constants.TileSize), zIndex: 3);
                    spriteCount++;
                }
            }

            // Output frame stats every 60 frames (every second at 60 FPS)
            _frameCounter++;
            if (_frameCounter >= 60)
            {
                // Console.WriteLine($"✅ Render frame: {spriteCount} sprites, Pacman at ({_pacman.X}, {_pacman.Y})");
                _frameCounter = 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in Render: {ex.Message}");
        }
    }

    private string GetWallSpriteName(int row, int col)
    {
        bool hasUp = row > 0 && _map[row - 1, col] == TileType.Wall;
        bool hasDown = row < Constants.MapHeight - 1 && _map[row + 1, col] == TileType.Wall;
        bool hasLeft = col > 0 && _map[row, col - 1] == TileType.Wall;
        bool hasRight = col < Constants.MapWidth - 1 && _map[row, col + 1] == TileType.Wall;

        // 4-way cross
        if (hasUp && hasDown && hasLeft && hasRight) return "walls_cross";

        // T-junctions
        if (hasUp && hasDown && hasLeft && !hasRight) return "walls_t_right";
        if (hasUp && hasDown && !hasLeft && hasRight) return "walls_t_left";
        if (hasUp && !hasDown && hasLeft && hasRight) return "walls_t_down";
        if (!hasUp && hasDown && hasLeft && hasRight) return "walls_t_up";

        // Corners
        if (!hasUp && hasDown && hasLeft && !hasRight) return "walls_corner_tl";
        if (!hasUp && hasDown && !hasLeft && hasRight) return "walls_corner_tr";
        if (hasUp && !hasDown && hasLeft && !hasRight) return "walls_corner_bl";
        if (hasUp && !hasDown && !hasLeft && hasRight) return "walls_corner_br";

        // Straights
        if (hasUp && hasDown) return "walls_vertical";
        if (hasLeft && hasRight) return "walls_horizontal";

        // End caps
        if (hasUp) return "walls_end_up";
        if (hasDown) return "walls_end_down";
        if (hasLeft) return "walls_end_left";
        if (hasRight) return "walls_end_right";

        // Default for isolated walls
        return "walls_horizontal";
    }


    private int _frameCounter = 0;
    private int _updateFrameCounter = 0;

    /// <summary>
    /// Draw a sprite image on the canvas at the specified position
    /// </summary>
    private static void DrawImage(Canvas canvas, CroppedBitmap sprite, int x, int y, int zIndex = 0)
    {
        try
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
        catch (Exception)
        {
            // Silently fail individual sprite draws
        }
    }

    /// <summary>
    /// Get the delta (dx, dy) for a direction
    /// </summary>
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

    /// <summary>
    /// Get the opposite direction
    /// </summary>
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

    // Helper method to trigger game over manually if needed
    public void TriggerGameOver()
    {
        GameOver?.Invoke();
    }
}
