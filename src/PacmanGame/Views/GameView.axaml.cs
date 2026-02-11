using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PacmanGame.Helpers;
using PacmanGame.Models.Entities;
using PacmanGame.Models.Enums;
using PacmanGame.Services.Interfaces;
using PacmanGame.ViewModels;
using System;
using System.Linq;

namespace PacmanGame.Views;

public partial class GameView : UserControl
{
    private DispatcherTimer? _gameLoopTimer;
    private ISpriteManager? _spriteManager;

    public GameView()
    {
        InitializeComponent();
        this.KeyDown += OnKeyDown;
        this.Focusable = true;
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        this.Focus();

        if (DataContext is GameViewModel vm)
        {
            vm.StartGame();
            _spriteManager = (vm.Engine as IGameEngineInternal)?.SpriteManager;

            _gameLoopTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000.0 / Constants.TargetFps)
            };
            _gameLoopTimer.Tick += (s, e) => GameLoop_Tick(vm);
            _gameLoopTimer.Start();
        }
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _gameLoopTimer?.Stop();
        _gameLoopTimer = null;
    }

    private void GameLoop_Tick(GameViewModel vm)
    {
        vm.UpdateGame(Constants.FixedDeltaTime);
        Render(vm);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not GameViewModel vm) return;

        Direction direction = e.Key switch
        {
            Key.Up => Direction.Up,
            Key.Down => Direction.Down,
            Key.Left => Direction.Left,
            Key.Right => Direction.Right,
            _ => Direction.None
        };

        if (direction != Direction.None)
        {
            vm.SetDirectionCommand.Execute(direction).Subscribe();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            if (vm.IsPaused)
                vm.ResumeGameCommand.Execute().Subscribe();
            else
                vm.PauseGameCommand.Execute().Subscribe();
            e.Handled = true;
        }
    }

    private void Render(GameViewModel vm)
    {
        if (_spriteManager == null || vm.Engine.Map.Length == 0) return;

        GameCanvas.Children.Clear();

        var engine = vm.Engine;

        // Draw tiles
        for (int row = 0; row < Constants.MapHeight; row++)
        for (int col = 0; col < Constants.MapWidth; col++)
        {
            if (engine.Map[row, col] == TileType.Wall)
            {
                string spriteName = GetWallSpriteName(row, col, engine.Map);
                var sprite = _spriteManager.GetTileSprite(spriteName);
                if (sprite != null)
                    DrawImage(sprite, col * Constants.TileSize, row * Constants.TileSize, 0);
            }
        }

        // Draw collectibles
        foreach (var collectible in engine.Collectibles.Where(c => c.IsActive))
        {
            string itemTypeKey = collectible.Type switch
            {
                CollectibleType.SmallDot => "dot",
                CollectibleType.PowerPellet => "power_pellet",
                CollectibleType.Cherry => "cherry",
                CollectibleType.Strawberry => "strawberry",
                _ => collectible.Type.ToString().ToLower()
            };
            var sprite = _spriteManager.GetItemSprite(itemTypeKey, engine.Pacman.AnimationFrame % 2);
            if (sprite != null)
                DrawImage(sprite, collectible.X * Constants.TileSize, collectible.Y * Constants.TileSize, 1);
        }

        // Draw Pac-Man
        if (engine.Pacman.IsDying)
        {
            var sprite = _spriteManager.GetDeathSprite(engine.Pacman.AnimationFrame);
            if (sprite != null)
                DrawImage(sprite, (int)(engine.Pacman.ExactX * Constants.TileSize), (int)(engine.Pacman.ExactY * Constants.TileSize), 2);
        }
        else
        {
            string direction = engine.Pacman.CurrentDirection switch
            {
                Direction.Up => "down",
                Direction.Down => "up",
                Direction.Left => "left",
                Direction.Right => "right",
                _ => "right" // Default to right if no direction
            };
            var sprite = _spriteManager.GetPacmanSprite(direction, engine.Pacman.AnimationFrame);
            if (sprite != null)
                DrawImage(sprite, (int)(engine.Pacman.ExactX * Constants.TileSize), (int)(engine.Pacman.ExactY * Constants.TileSize), 2);
        }

        // Draw ghosts
        foreach (var ghost in engine.Ghosts)
        {
            CroppedBitmap? sprite = GetGhostSprite(ghost);
            if (sprite != null)
                DrawImage(sprite, (int)(ghost.ExactX * Constants.TileSize), (int)(ghost.ExactY * Constants.TileSize), 3);
        }
    }

    private CroppedBitmap? GetGhostSprite(Ghost ghost)
    {
        if (_spriteManager == null) return null;

        return ghost.State switch
        {
            GhostState.Eaten => _spriteManager.GetGhostEyesSprite(ghost.CurrentDirection.ToString().ToLower()),
            GhostState.Vulnerable => _spriteManager.GetVulnerableGhostSprite(0),
            GhostState.Warning => (ghost.AnimationFrame % 2 == 0) ? _spriteManager.GetVulnerableGhostSprite(1) : _spriteManager.GetVulnerableGhostSprite(0),
            _ => _spriteManager.GetGhostSprite(ghost.Type.ToString().ToLower(), ghost.CurrentDirection.ToString().ToLower(), ghost.AnimationFrame)
        };
    }

    private void DrawImage(CroppedBitmap sprite, int x, int y, int zIndex)
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
        GameCanvas.Children.Add(image);
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
