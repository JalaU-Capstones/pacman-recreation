using Avalonia.Controls;
using Avalonia.Input;
using PacmanGame.ViewModels;
using PacmanGame.Models.Enums;
using System;

namespace PacmanGame.Views;

public partial class GameView : UserControl
{
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
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not GameViewModel gameViewModel)
            return;

        // Handle game controls
        switch (e.Key)
        {
            case Key.Up:
                // TODO: Move Pac-Man up
                Console.WriteLine("Move Up");
                e.Handled = true;
                break;
            
            case Key.Down:
                // TODO: Move Pac-Man down
                Console.WriteLine("Move Down");
                e.Handled = true;
                break;
            
            case Key.Left:
                // TODO: Move Pac-Man left
                Console.WriteLine("Move Left");
                e.Handled = true;
                break;
            
            case Key.Right:
                // TODO: Move Pac-Man right
                Console.WriteLine("Move Right");
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
                Console.WriteLine("Toggle FPS");
                e.Handled = true;
                break;
        }
    }
}
