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
        this.KeyDown += OnKeyDown;
        this.Focusable = true;
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        this.Focus();

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
        GameCanvas.Children.Clear();
        vm.Render(GameCanvas);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MultiplayerGameViewModel vm) return;

        vm.HandleKeyPress(e.Key);
        e.Handled = true;
    }
}
