using Avalonia.Controls;
using Avalonia.Media.Imaging;
using PacmanGame.Helpers;
using PacmanGame.Models.Entities;
using PacmanGame.Models.Enums;
using PacmanGame.Services.Interfaces;
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
                _ghosts.Add(ghost);
            }
            Console.WriteLine($"✅ {_ghosts.Count} ghosts spawned");

            var collectibleData = _mapLoader.GetCollectibles(fileName);
            _collectibles = new List<Collectible>();
            foreach (var data in collectibleData)
            {
                Collectible collectible = new Collectible(data.Col, data.Row, data.Type);
                _collectibles.Add(collectible);
            }
            Console.WriteLine($"✅ {_collectibles.Count} collectibles loaded");

            _ghostsEatenThisRound = 0;

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
            Console.WriteLine($"🎮 Update loop running - {_updateFrameCounter} frames processed");
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
            int newCol = _pacman.X + dx;
            int newRow = _pacman.Y + dy;

            // Tunnel wrapping
            if (newCol < 0)
                newCol = Constants.MapWidth - 1;
            if (newCol >= Constants.MapWidth)
                newCol = 0;
            if (newRow < 0)
                newRow = Constants.MapHeight - 1;
            if (newRow >= Constants.MapHeight)
                newRow = 0;

            _pacman.X = newCol;
            _pacman.Y = newRow;
            _pacman.IsMoving = true;
        }
        else
        {
            _pacman.IsMoving = false;
        }

        _pacman.UpdateInvulnerability(deltaTime);
    }

    /// <summary>
    /// Update all ghosts with simple random AI
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
        // If eaten, move back to spawn
        if (ghost.State == GhostState.Eaten)
        {
            if (ghost.X == ghost.SpawnX && ghost.Y == ghost.SpawnY)
            {
                ghost.Respawn();
            }
            else
            {
                // Move one step closer to spawn (Manhattan distance)
                int dx = ghost.SpawnX > ghost.X ? 1 : (ghost.SpawnX < ghost.X ? -1 : 0);
                int dy = ghost.SpawnY > ghost.Y ? 1 : (ghost.SpawnY < ghost.Y ? -1 : 0);

                if (dx != 0 && ghost.CanMove((Direction)(dx == 1 ? 4 : 3), _map))
                {
                    ghost.X += dx;
                }
                else if (dy != 0 && ghost.CanMove((Direction)(dy == 1 ? 2 : 1), _map))
                {
                    ghost.Y += dy;
                }
            }
        }
        else
        {
            // Simple random AI: pick a random valid direction
            var candidates = new List<Direction> { Direction.Up, Direction.Down, Direction.Left, Direction.Right }
                .Where(d => ghost.CanMove(d, _map))
                .Where(d => d != GetOppositeDirection(ghost.CurrentDirection))
                .ToList();

            if (candidates.Count == 0)
            {
                candidates = new List<Direction> { Direction.Up, Direction.Down, Direction.Left, Direction.Right }
                    .Where(d => ghost.CanMove(d, _map))
                    .ToList();
            }

            if (candidates.Count > 0)
            {
                Direction picked = candidates[_random.Next(candidates.Count)];
                ghost.CurrentDirection = picked;

                (int dx, int dy) = GetDirectionDeltas(picked);
                ghost.X += dx;
                ghost.Y += dy;
            }
        }

        ghost.UpdateVulnerability(deltaTime);
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
        _pacman.CurrentDirection = Direction.None;
        _pacman.NextDirection = Direction.None;
        _pacman.IsInvulnerable = false;
        _pacman.InvulnerabilityTime = 0f;

        // Reset ghosts
        foreach (var ghost in _ghosts)
        {
            ghost.X = ghost.SpawnX;
            ghost.Y = ghost.SpawnY;
            ghost.CurrentDirection = Direction.None;
            ghost.State = GhostState.Normal;
            ghost.VulnerableTime = 0f;
        }

        _ghostsEatenThisRound = 0;
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

            // 1. Draw tiles
            for (int row = 0; row < Constants.MapHeight; row++)
            {
                for (int col = 0; col < Constants.MapWidth; col++)
                {
                    if (_map[row, col] == TileType.Wall)
                    {
                        var sprite = _spriteManager.GetTileSprite("walls_horizontal");
                        if (sprite != null)
                        {
                            DrawImage(canvas, sprite, col * Constants.TileSize, row * Constants.TileSize);
                            spriteCount++;
                        }
                    }
                }
            }

            // 2. Draw collectibles
            foreach (var collectible in _collectibles.Where(c => c.IsActive))
            {
                string itemType = collectible.Type == CollectibleType.SmallDot ? "dot" : "power_pellet";
                var sprite = _spriteManager.GetItemSprite(itemType, _pacman.AnimationFrame);
                if (sprite != null)
                {
                    DrawImage(canvas, sprite, collectible.X * Constants.TileSize, collectible.Y * Constants.TileSize);
                    spriteCount++;
                }
            }

            // 3. Draw Pac-Man
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
                    DrawImage(canvas, sprite, _pacman.X * Constants.TileSize, _pacman.Y * Constants.TileSize);
                    spriteCount++;
                }
            }

            // 4. Draw ghosts
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
                else
                {
                    string ghostTypeName = ghost.Type.ToString().ToLower();
                    string ghostDirection = ghost.CurrentDirection switch
                    {
                        Direction.Up => "up",
                        Direction.Down => "down",
                        Direction.Left => "left",
                        _ => "right"
                    };

                    if (ghost.State == GhostState.Normal)
                    {
                        sprite = _spriteManager.GetGhostSprite(ghostTypeName, ghostDirection, ghost.AnimationFrame);
                    }
                    else if (ghost.State == GhostState.Vulnerable)
                    {
                        sprite = _spriteManager.GetVulnerableGhostSprite(ghost.AnimationFrame);
                    }
                    else if (ghost.State == GhostState.Warning)
                    {
                        sprite = _spriteManager.GetWarningGhostSprite(ghost.AnimationFrame);
                    }
                }

                if (sprite != null)
                {
                    DrawImage(canvas, sprite, ghost.X * Constants.TileSize, ghost.Y * Constants.TileSize);
                    spriteCount++;
                }
            }

            // Output frame stats every 60 frames (every second at 60 FPS)
            _frameCounter++;
            if (_frameCounter >= 60)
            {
                Console.WriteLine($"✅ Render frame: {spriteCount} sprites, Pacman at ({_pacman.X}, {_pacman.Y})");
                _frameCounter = 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in Render: {ex.Message}");
        }
    }

    private int _frameCounter = 0;
    private int _updateFrameCounter = 0;

    /// <summary>
    /// Draw a sprite image on the canvas at the specified position
    /// </summary>
    private static void DrawImage(Canvas canvas, CroppedBitmap sprite, int x, int y)
    {
        try
        {
            var image = new Image
            {
                Source = sprite,
                Width = Constants.TileSize,
                Height = Constants.TileSize
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
}
