using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using PacmanGame.Helpers;
using PacmanGame.Services.Interfaces;
using PacmanGame.Services.KeyBindings;
using PacmanGame.ViewModels;
using PacmanGame.Shared;
using System;

namespace PacmanGame.Views;

public partial class MultiplayerGameView : UserControl
{
    private DispatcherTimer? _renderTimer;
    private FpsCounter? _fpsCounter;

    public MultiplayerGameView()
    {
        InitializeComponent();
        // Replicate the working single-player formula:
        this.Focusable = true;
        this.Loaded += (s, e) => this.Focus();
        this.PointerPressed += (s, e) => this.Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (DataContext is not MultiplayerGameViewModel vm) return;

        var keyBindings = App.GetService<IKeyBindingService>();

        if ((keyBindings != null && keyBindings.IsActionTriggered(KeyBindingActions.ShowFps, e.Key, e.KeyModifiers)) || e.Key == Key.F1)
        {
            vm.ToggleFpsCommand.Execute(null);
            e.Handled = true;
            return;
        }

        var direction =
            keyBindings != null && keyBindings.IsActionTriggered(KeyBindingActions.MoveUp, e.Key, e.KeyModifiers) ? Direction.Up :
            keyBindings != null && keyBindings.IsActionTriggered(KeyBindingActions.MoveDown, e.Key, e.KeyModifiers) ? Direction.Down :
            keyBindings != null && keyBindings.IsActionTriggered(KeyBindingActions.MoveLeft, e.Key, e.KeyModifiers) ? Direction.Left :
            keyBindings != null && keyBindings.IsActionTriggered(KeyBindingActions.MoveRight, e.Key, e.KeyModifiers) ? Direction.Right :
            e.Key switch
            {
                Key.Up => Direction.Up,
                Key.Down => Direction.Down,
                Key.Left => Direction.Left,
                Key.Right => Direction.Right,
                _ => Direction.None
            };

        if (direction != Direction.None)
        {
            // Execute the ReactiveCommand with the direction
            vm.SetDirectionCommand.Execute(direction).Subscribe();
            e.Handled = true;
            return;
        }

        if ((keyBindings != null && keyBindings.IsActionTriggered(KeyBindingActions.PauseGame, e.Key, e.KeyModifiers)) || e.Key == Key.Escape)
        {
            vm.TogglePauseCommand.Execute(null);
            e.Handled = true;
        }
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (DataContext is MultiplayerGameViewModel vm)
        {
            _fpsCounter = new FpsCounter();
            _renderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000.0 / Constants.TargetFps)
            };
            _renderTimer.Tick += (s, e) => RenderFrame(vm);
            _renderTimer.Start();
        }
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _renderTimer?.Stop();
        _renderTimer = null;
        _fpsCounter = null;
    }

    private void RenderFrame(MultiplayerGameViewModel vm)
    {
        vm.Engine.Update(Constants.FixedDeltaTime);
        GameCanvas.Children.Clear();
        vm.Render(GameCanvas);

        var fps = _fpsCounter?.OnFrame();
        if (fps.HasValue)
        {
            vm.Fps = fps.Value;
        }
    }
}
