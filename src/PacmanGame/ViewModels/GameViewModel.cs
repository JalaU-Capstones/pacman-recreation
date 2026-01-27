using ReactiveUI;
using System;
using System.Reactive;

namespace PacmanGame.ViewModels;

/// <summary>
/// ViewModel for the main game screen.
/// Manages game state, score, lives, and coordinates between game services.
/// </summary>
public class GameViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    // Game state properties
    private int _score;
    private int _lives;
    private int _level;
    private bool _isGameRunning;
    private bool _isPaused;

    /// <summary>
    /// Current player score
    /// </summary>
    public int Score
    {
        get => _score;
        set => this.RaiseAndSetIfChanged(ref _score, value);
    }

    /// <summary>
    /// Remaining lives
    /// </summary>
    public int Lives
    {
        get => _lives;
        set => this.RaiseAndSetIfChanged(ref _lives, value);
    }

    /// <summary>
    /// Current level number
    /// </summary>
    public int Level
    {
        get => _level;
        set => this.RaiseAndSetIfChanged(ref _level, value);
    }

    /// <summary>
    /// Is the game currently running
    /// </summary>
    public bool IsGameRunning
    {
        get => _isGameRunning;
        set => this.RaiseAndSetIfChanged(ref _isGameRunning, value);
    }

    /// <summary>
    /// Is the game paused
    /// </summary>
    public bool IsPaused
    {
        get => _isPaused;
        set => this.RaiseAndSetIfChanged(ref _isPaused, value);
    }

    // Commands
    public ReactiveCommand<Unit, Unit> PauseGameCommand { get; }
    public ReactiveCommand<Unit, Unit> ResumeGameCommand { get; }
    public ReactiveCommand<Unit, Unit> ReturnToMenuCommand { get; }

    public GameViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;

        // Initialize game state
        _score = 0;
        _lives = 3;
        _level = 1;
        _isGameRunning = false;
        _isPaused = false;

        // Initialize commands
        PauseGameCommand = ReactiveCommand.Create(PauseGame);
        ResumeGameCommand = ReactiveCommand.Create(ResumeGame);
        ReturnToMenuCommand = ReactiveCommand.Create(ReturnToMenu);
    }

    /// <summary>
    /// Start the game
    /// </summary>
    public void StartGame()
    {
        // TODO: Initialize game engine
        // TODO: Load level
        // TODO: Start game loop
        // TODO: Play game start sound

        IsGameRunning = true;
        IsPaused = false;

        Console.WriteLine($"Game started - Level {Level}");
    }

    /// <summary>
    /// Pause the game
    /// </summary>
    private void PauseGame()
    {
        if (IsGameRunning && !IsPaused)
        {
            IsPaused = true;
            // TODO: Stop game loop
            // TODO: Play pause sound
            Console.WriteLine("Game paused");
        }
    }

    /// <summary>
    /// Resume the game
    /// </summary>
    private void ResumeGame()
    {
        if (IsGameRunning && IsPaused)
        {
            IsPaused = false;
            // TODO: Resume game loop
            // TODO: Play resume sound
            Console.WriteLine("Game resumed");
        }
    }

    /// <summary>
    /// Return to main menu
    /// </summary>
    private void ReturnToMenu()
    {
        // TODO: Stop game loop
        // TODO: Play menu select sound
        IsGameRunning = false;
        _mainWindowViewModel.NavigateTo(new MainMenuViewModel(_mainWindowViewModel));
    }

    /// <summary>
    /// Handle game over
    /// </summary>
    public void GameOver()
    {
        IsGameRunning = false;
        // TODO: Save score
        // TODO: Play game over sound
        // TODO: Show game over screen
        Console.WriteLine($"Game Over! Final Score: {Score}");
    }

    /// <summary>
    /// Add points to the score
    /// </summary>
    public void AddScore(int points)
    {
        Score += points;
        // TODO: Check for extra life milestone
    }

    /// <summary>
    /// Lose a life
    /// </summary>
    public void LoseLife()
    {
        Lives--;
        // TODO: Play death sound
        // TODO: Reset positions
        
        if (Lives <= 0)
        {
            GameOver();
        }
    }

    /// <summary>
    /// Complete the current level
    /// </summary>
    public void CompleteLevel()
    {
        // TODO: Play level complete sound
        // TODO: Show level complete animation
        Level++;
        // TODO: Load next level
        Console.WriteLine($"Level {Level - 1} complete! Starting level {Level}");
    }
}
