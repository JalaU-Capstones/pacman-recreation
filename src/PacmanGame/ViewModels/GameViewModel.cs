using ReactiveUI;
using PacmanGame.Helpers;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
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
    private readonly IProfileManager _profileManager;
    private readonly IGameEngine _engine;
    private readonly IAudioManager _audioManager;
    private int _extraLifeThreshold;

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

    /// <summary>
    /// Get the game engine
    /// </summary>
    public IGameEngine Engine => _engine;

    // Commands
    public ReactiveCommand<Unit, Unit> PauseGameCommand { get; }
    public ReactiveCommand<Unit, Unit> ResumeGameCommand { get; }
    public ReactiveCommand<Unit, Unit> ReturnToMenuCommand { get; }

    public GameViewModel(MainWindowViewModel mainWindowViewModel, IProfileManager profileManager, IAudioManager? audioManager = null)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _profileManager = profileManager;

        // Initialize game state
        _score = 0;
        _lives = 3;
        _level = 1;
        _isGameRunning = false;
        _isPaused = false;
        _extraLifeThreshold = Constants.ExtraLifeScore;

        // Create audio manager first (used in event handlers)
        _audioManager = audioManager ?? new AudioManager();
        if (audioManager == null)
        {
            _audioManager.Initialize();
        }

        // Create engine with required services
        // TODO: Inject these properly via DI container in production
        _engine = new GameEngine(
            new MapLoader(),
            new SpriteManager(),
            _audioManager,
            new CollisionDetector());

        // Subscribe to engine events
        _engine.ScoreChanged += OnScoreChanged;
        _engine.LifeLost += OnLifeLost;
        _engine.LevelComplete += OnLevelComplete;
        _engine.GameOver += OnGameOver;

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
        try
        {
            Console.WriteLine($"📝 StartGame called, Level={_level}");
            _engine.LoadLevel(_level);
            Console.WriteLine($"✅ Level {_level} loaded");
            _engine.Start();
            Console.WriteLine("✅ Engine started");
            _audioManager.PlayMusic(Constants.BackgroundMusic);
            Console.WriteLine("✅ Background music playing");
            _audioManager.PlaySoundEffect("game-start");
            Console.WriteLine("✅ Game start sound played");

            IsGameRunning = true;
            IsPaused = false;
            Console.WriteLine("✅ Game state updated");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in StartGame: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Pause the game
    /// </summary>
    private void PauseGame()
    {
        if (IsGameRunning && !IsPaused)
        {
            _engine.Pause();
            IsPaused = true;
            _audioManager.PauseMusic();
        }
    }

    /// <summary>
    /// Resume the game
    /// </summary>
    private void ResumeGame()
    {
        if (IsGameRunning && IsPaused)
        {
            _engine.Resume();
            IsPaused = false;
            _audioManager.ResumeMusic();
        }
    }

    /// <summary>
    /// Return to main menu
    /// </summary>
    private void ReturnToMenu()
    {
        _engine.Stop();
        _audioManager.StopMusic();
        IsGameRunning = false;
        _mainWindowViewModel.NavigateTo(new MainMenuViewModel(_mainWindowViewModel, _profileManager, _audioManager));
    }

    /// <summary>
    /// Handle game over
    /// </summary>
    public void GameOver()
    {
        IsGameRunning = false;
        _engine.Stop();
        _audioManager.StopMusic();
        _audioManager.PlayMusic(Constants.GameOverMusic, loop: false);
        Console.WriteLine($"Game Over! Final Score: {Score}");

        // Save score to profile
        var activeProfile = _profileManager.GetActiveProfile();
        if (activeProfile != null)
        {
            _profileManager.SaveScore(activeProfile.Id, Score, Level);
        }
    }

    /// <summary>
    /// Add points to the score and check for extra life
    /// </summary>
    private void OnScoreChanged(int points)
    {
        Score += points;
        if (Score >= _extraLifeThreshold && Lives < Constants.MaxLives)
        {
            Lives++;
            _extraLifeThreshold += Constants.ExtraLifeScore;
            _audioManager.PlaySoundEffect("extra-life");
        }
    }

    /// <summary>
    /// Lose a life
    /// </summary>
    private void OnLifeLost()
    {
        Lives--;
        if (Lives <= 0)
        {
            GameOver();
        }
    }

    /// <summary>
    /// Complete the current level
    /// </summary>
    private void OnLevelComplete()
    {
        _audioManager.PlaySoundEffect("level-complete");
        Level++;
        Console.WriteLine($"Level {Level - 1} complete! Starting level {Level}");

        // TODO: Add delay before loading next level
        _engine.LoadLevel(Level);
    }

    /// <summary>
    /// Handle game over event
    /// </summary>
    private void OnGameOver()
    {
        GameOver();
    }
}
