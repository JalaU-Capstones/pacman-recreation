using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using PacmanGame.Helpers;
using PacmanGame.Services.Interfaces;
using PacmanGame.ViewModels;
using System;
using LocalDirection = PacmanGame.Models.Enums.Direction;
using SharedDirection = PacmanGame.Shared.Direction;

namespace PacmanGame.Views;

public partial class GameView : UserControl
{
    private DispatcherTimer? _gameLoopTimer;

    public GameView()
    {
        InitializeComponent();
        Focusable = true;
        this.KeyDown += OnKeyDown;
        this.Loaded += (s, e) => Dispatcher.UIThread.InvokeAsync(() => this.Focus(), DispatcherPriority.Render);
        this.PointerPressed += OnPointerPressed;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() => this.Focus());
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (DataContext is not (GameViewModel or MultiplayerGameViewModel)) return;

        // We need to handle different view models separately or use a common interface if available
        // For now, let's just check the type
        IGameEngine? engine = null;
        if (DataContext is GameViewModel gvm)
        {
            engine = gvm.Engine;
            gvm.StartGame();
        }
        else if (DataContext is MultiplayerGameViewModel mgvm)
        {
            engine = mgvm.Engine;
        }

        if (engine == null) return;

        _gameLoopTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / Constants.TargetFps)
        };

        // Capture the current DataContext to avoid closure issues if it changes (though unlikely in this lifecycle)
        var currentVm = DataContext;
        _gameLoopTimer.Tick += (s, args) =>
        {
             if (currentVm is GameViewModel g && g.IsGameRunning && !g.IsPaused)
             {
                 engine.Update(Constants.FixedDeltaTime);
             }
             else if (currentVm is MultiplayerGameViewModel m && m.IsGameRunning)
             {
                 engine.Update(Constants.FixedDeltaTime);
             }

             GameCanvas.Children.Clear();
             engine.Render(GameCanvas);
        };
        _gameLoopTimer.Start();

        // Give focus to this UserControl so KeyDown events are captured
        Dispatcher.UIThread.InvokeAsync(() => this.Focus(), DispatcherPriority.Render);
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _gameLoopTimer?.Stop();
        _gameLoopTimer = null;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is GameViewModel gvm)
        {
            var direction = e.Key switch
            {
                Key.Up => LocalDirection.Up,
                Key.Down => LocalDirection.Down,
                Key.Left => LocalDirection.Left,
                Key.Right => LocalDirection.Right,
                _ => LocalDirection.None
            };

            if (direction != LocalDirection.None)
            {
                gvm.SetDirectionCommand.Execute(direction).Subscribe();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                if (gvm.IsPaused)
                    gvm.ResumeGameCommand.Execute(null);
                else
                    gvm.PauseGameCommand.Execute(null);
                e.Handled = true;
            }
        }
        else if (DataContext is MultiplayerGameViewModel mgvm)
        {
            // Map Avalonia Key to LocalDirection (which is what the ViewModel expects)
            // The ViewModel expects PacmanGame.Models.Enums.Direction
            var direction = e.Key switch
            {
                Key.Up => LocalDirection.Up,
                Key.Down => LocalDirection.Down,
                Key.Left => LocalDirection.Left,
                Key.Right => LocalDirection.Right,
                _ => LocalDirection.None
            };

            if (direction != LocalDirection.None)
            {
                mgvm.SetDirectionCommand.Execute(direction).Subscribe();
                e.Handled = true;
            }
        }
    }
}
