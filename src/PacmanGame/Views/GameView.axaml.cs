using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using PacmanGame.Helpers;
using PacmanGame.ViewModels;
using PacmanGame.Models.Enums;
using System;

namespace PacmanGame.Views;

public partial class GameView : UserControl
{
    private DispatcherTimer? _gameLoopTimer;

    public GameView()
    {
        InitializeComponent();

        // Subscribe to keyboard input
        this.KeyDown += OnKeyDown;

        // Make sure the control can receive focus
        this.Focusable = true;
        this.Focus();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Ensure focus when view loads
        this.Focus();

        // Start the game if ViewModel is available
        if (DataContext is GameViewModel gameViewModel)
        {
            gameViewModel.StartGame();

            // Start the game loop timer (60 FPS)
            _gameLoopTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000.0 / Constants.TargetFps)
            };

            _gameLoopTimer.Tick += (_, _) =>
            {
                gameViewModel.UpdateGame(Constants.FixedDeltaTime);
                gameViewModel.Engine.Render(GameCanvas);
            };
            _gameLoopTimer.Start();
        }
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _gameLoopTimer?.Stop();
        _gameLoopTimer = null;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not GameViewModel gameViewModel)
            return;

        // Handle game controls
        switch (e.Key)
        {
            case Key.Up:
                gameViewModel.Engine.SetPacmanDirection(Direction.Up);
                e.Handled = true;
                break;

            case Key.Down:
                gameViewModel.Engine.SetPacmanDirection(Direction.Down);
                e.Handled = true;
                break;

            case Key.Left:
                gameViewModel.Engine.SetPacmanDirection(Direction.Left);
                e.Handled = true;
                break;

            case Key.Right:
                gameViewModel.Engine.SetPacmanDirection(Direction.Right);
                e.Handled = true;
                break;

            case Key.Escape:
                // Pause/Resume game
                if (gameViewModel.IsPaused)
                    gameViewModel.ResumeGameCommand.Execute().Subscribe();
                else
                    gameViewModel.PauseGameCommand.Execute().Subscribe();
                e.Handled = true;
                break;

            case Key.F1:
                // TODO: Toggle FPS counter (debug)
                e.Handled = true;
                break;
        }
    }
}
