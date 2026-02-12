using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using PacmanGame.Helpers;
using PacmanGame.Models.Enums;
using PacmanGame.Services.Interfaces;
using PacmanGame.ViewModels;
using System;

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
        var direction = e.Key switch
        {
            Key.Up => Direction.Up,
            Key.Down => Direction.Down,
            Key.Left => Direction.Left,
            Key.Right => Direction.Right,
            _ => Direction.None
        };

        if (direction != Direction.None)
        {
            if (DataContext is GameViewModel gvm)
            {
                gvm.SetDirectionCommand.Execute(direction);
            }
            else if (DataContext is MultiplayerGameViewModel mgvm)
            {
                mgvm.SetDirectionCommand.Execute(direction);
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            if (DataContext is GameViewModel gvm)
            {
                if (gvm.IsPaused)
                    gvm.ResumeGameCommand.Execute(null);
                else
                    gvm.PauseGameCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
