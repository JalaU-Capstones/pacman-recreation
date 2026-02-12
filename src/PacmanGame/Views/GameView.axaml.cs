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

        var vm = DataContext as ViewModelBase;
        var engine = vm switch
        {
            GameViewModel gvm => gvm.Engine,
            MultiplayerGameViewModel mgvm => mgvm.Engine,
            _ => null
        };

        if (engine == null || vm == null) return;

        if (vm is GameViewModel gameVm)
        {
            gameVm.StartGame();
        }

        _gameLoopTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / Constants.TargetFps)
        };
        _gameLoopTimer.Tick += (s, e) => GameLoop_Tick(vm, engine);
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

    private void GameLoop_Tick(ViewModelBase vm, IGameEngine engine)
    {
        if (vm is GameViewModel gvm && gvm.IsGameRunning && !gvm.IsPaused)
        {
            engine.Update(Constants.FixedDeltaTime);
        }
        else if (vm is MultiplayerGameViewModel mgvm && mgvm.IsGameRunning)
        {
            engine.Update(Constants.FixedDeltaTime);
        }

        GameCanvas.Children.Clear();
        engine.Render(GameCanvas);
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
            var direction = e.Key switch
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
            }
        }
    }
}
