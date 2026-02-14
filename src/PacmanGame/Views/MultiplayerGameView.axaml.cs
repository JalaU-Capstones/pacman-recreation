using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using PacmanGame.Helpers;
using PacmanGame.ViewModels;
using System;

namespace PacmanGame.Views;

public partial class MultiplayerGameView : UserControl
{
    private DispatcherTimer? _renderTimer;

    public MultiplayerGameView()
    {
        InitializeComponent();
        // Replicate the working single-player formula:
        this.Focusable = true;
        this.Loaded += (s, e) => this.Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (DataContext is not MultiplayerGameViewModel vm) return;

        // Map key to direction
        var direction = e.Key switch
        {
            Key.Up => PacmanGame.Models.Enums.Direction.Up,
            Key.Down => PacmanGame.Models.Enums.Direction.Down,
            Key.Left => PacmanGame.Models.Enums.Direction.Left,
            Key.Right => PacmanGame.Models.Enums.Direction.Right,
            Key.Escape => PacmanGame.Models.Enums.Direction.None, // For pause/menu
            _ => PacmanGame.Models.Enums.Direction.None
        };

        if (direction != PacmanGame.Models.Enums.Direction.None)
        {
            // Execute the ReactiveCommand with the direction
            vm.SetDirectionCommand.Execute(direction);
            e.Handled = true;

            Console.WriteLine($"[CLIENT-VIEW] Key pressed: {e.Key} -> Direction: {direction}");
        }
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (DataContext is MultiplayerGameViewModel vm)
        {
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
    }

    private void RenderFrame(MultiplayerGameViewModel vm)
    {
        vm.Engine.Update(Constants.FixedDeltaTime);
        GameCanvas.Children.Clear();
        vm.Render(GameCanvas);
    }
}
