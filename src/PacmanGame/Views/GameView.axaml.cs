using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using PacmanGame.Helpers;
using PacmanGame.Services.Interfaces;
using PacmanGame.ViewModels;
using System;
using PacmanGame.Services.KeyBindings;
using LocalDirection = PacmanGame.Models.Enums.Direction;
using SharedDirection = PacmanGame.Shared.Direction;

namespace PacmanGame.Views;

public partial class GameView : UserControl
{
    private DispatcherTimer? _gameLoopTimer;
    private FpsCounter? _fpsCounter;

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

        _fpsCounter = new FpsCounter();

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

             // FPS overlay uses the actual render loop cadence.
             if (_fpsCounter != null)
             {
                 var fps = _fpsCounter.OnFrame();
                 if (fps.HasValue)
                 {
                     if (currentVm is GameViewModel g2)
                     {
                         g2.Fps = fps.Value;
                     }
                     else if (currentVm is MultiplayerGameViewModel m2)
                     {
                         m2.Fps = fps.Value;
                     }
                 }
             }
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
        _fpsCounter = null;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is GameViewModel gvm)
        {
            if (gvm.IsLevelComplete || gvm.IsGameOver || gvm.IsVictory)
            {
                e.Handled = true;
                return;
            }

            if (gvm.IsActionTriggered(KeyBindingActions.ShowFps, e.Key, e.KeyModifiers) || e.Key == Key.F1)
            {
                gvm.ToggleFpsCommand.Execute(null);
                e.Handled = true;
                return;
            }

            var direction =
                gvm.IsActionTriggered(KeyBindingActions.MoveUp, e.Key, e.KeyModifiers) ? LocalDirection.Up :
                gvm.IsActionTriggered(KeyBindingActions.MoveDown, e.Key, e.KeyModifiers) ? LocalDirection.Down :
                gvm.IsActionTriggered(KeyBindingActions.MoveLeft, e.Key, e.KeyModifiers) ? LocalDirection.Left :
                gvm.IsActionTriggered(KeyBindingActions.MoveRight, e.Key, e.KeyModifiers) ? LocalDirection.Right :
                LocalDirection.None;

            if (direction != LocalDirection.None)
            {
                gvm.SetDirectionCommand.Execute(direction).Subscribe();
                e.Handled = true;
            }
            else if (gvm.IsActionTriggered(KeyBindingActions.PauseGame, e.Key, e.KeyModifiers) || e.Key == Key.Escape)
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
            var keyBindings = App.GetService<IKeyBindingService>();

            if ((keyBindings != null && keyBindings.IsActionTriggered(KeyBindingActions.ShowFps, e.Key, e.KeyModifiers)) || e.Key == Key.F1)
            {
                mgvm.ToggleFpsCommand.Execute(null);
                e.Handled = true;
                return;
            }

            var direction =
                keyBindings != null && keyBindings.IsActionTriggered(KeyBindingActions.MoveUp, e.Key, e.KeyModifiers) ? SharedDirection.Up :
                keyBindings != null && keyBindings.IsActionTriggered(KeyBindingActions.MoveDown, e.Key, e.KeyModifiers) ? SharedDirection.Down :
                keyBindings != null && keyBindings.IsActionTriggered(KeyBindingActions.MoveLeft, e.Key, e.KeyModifiers) ? SharedDirection.Left :
                keyBindings != null && keyBindings.IsActionTriggered(KeyBindingActions.MoveRight, e.Key, e.KeyModifiers) ? SharedDirection.Right :
                e.Key switch
                {
                    Key.Up => SharedDirection.Up,
                    Key.Down => SharedDirection.Down,
                    Key.Left => SharedDirection.Left,
                    Key.Right => SharedDirection.Right,
                    _ => SharedDirection.None
                };

            if (direction != SharedDirection.None)
            {
                mgvm.SetDirectionCommand.Execute(direction).Subscribe();
                e.Handled = true;
                return;
            }

            if ((keyBindings != null && keyBindings.IsActionTriggered(KeyBindingActions.PauseGame, e.Key, e.KeyModifiers)) || e.Key == Key.Escape)
            {
                mgvm.TogglePauseCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
