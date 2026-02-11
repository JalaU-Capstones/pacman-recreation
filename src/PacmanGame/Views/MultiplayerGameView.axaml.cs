using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PacmanGame.Helpers;
using PacmanGame.Models.Enums;
using PacmanGame.Services.Interfaces;
using PacmanGame.ViewModels;
using System;

namespace PacmanGame.Views;

public partial class MultiplayerGameView : UserControl
{
    private DispatcherTimer? _gameLoopTimer;

    public MultiplayerGameView()
    {
        InitializeComponent();
        this.KeyDown += OnKeyDown;
        this.Focusable = true;
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        this.Focus();

        if (DataContext is MultiplayerGameViewModel vm)
        {
            // vm.StartGame(); // This will be handled by the server
            // _spriteManager = (vm.Engine as IGameEngineInternal)?.SpriteManager;

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

    private void GameLoop_Tick(MultiplayerGameViewModel vm)
    {
        // vm.UpdateGame(Constants.FixedDeltaTime);
        Render(vm);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MultiplayerGameViewModel vm) return;

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
            // vm.SetDirectionCommand.Execute(direction).Subscribe();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            // if (vm.IsPaused)
            //     vm.ResumeGameCommand.Execute().Subscribe();
            // else
            //     vm.PauseGameCommand.Execute().Subscribe();
            e.Handled = true;
        }
    }

    private void Render(MultiplayerGameViewModel vm)
    {
        // Rendering logic will be based on the GameStateMessage from the server
        GameCanvas.Children.Clear();
    }
}
