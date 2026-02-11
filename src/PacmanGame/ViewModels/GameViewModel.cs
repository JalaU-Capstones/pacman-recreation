using ReactiveUI;
using PacmanGame.Helpers;
using PacmanGame.Models.Enums;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
using System;
using System.Reactive;
using System.Threading.Tasks;

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
    private readonly ILogger _logger;
    private int _extraLifeThreshold;
    private int _frameCount; // For logging game loop frames

    // Game state properties
    private int _score;
    private int _lives;
    private int _level;
    private bool _isGameRunning;
    private bool _isPaused;
    private bool _isGameOver;
    private int _finalScore;
    private bool _isLevelComplete;

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
    /// Is the game over
    /// </summary>
    public bool IsGameOver
    {
        get => _isGameOver;
        set => this.RaiseAndSetIfChanged(ref _isGameOver, value);
    }

    /// <summary>
    /// Final score when the game is over
    /// </summary>
    public int FinalScore
    {
        get => _finalScore;
        set => this.RaiseAndSetIfChanged(ref _finalScore, value);
    }

    /// <summary>
    /// Is the level complete message showing
    /// </summary>
    public bool IsLevelComplete
    {
        get => _isLevelComplete;
        set => this.RaiseAndSetIfChanged(ref _isLevelComplete, value);
    }

    /// <summary>
    /// Get the game engine
    /// </summary>
    public IGameEngine Engine => _engine;

    // Commands
    public ReactiveCommand<Unit, Unit> PauseGameCommand { get; }
    public ReactiveCommand<Unit, Unit> ResumeGameCommand { get; }
    public ReactiveCommand<Unit, Unit> ReturnToMenuCommand { get; }
    public ReactiveCommand<Unit, Unit> RestartGameCommand { get; }
    public ReactiveCommand<Direction, Unit> SetDirectionCommand { get; }

    public GameViewModel(MainWindowViewModel mainWindowViewModel, IProfileManager profileManager, IAudioManager audioManager, ILogger logger)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _profileManager = profileManager;
        _audioManager = audioManager;
        _logger = logger;
        _frameCount = 0;

        // Initialize game state
        _score = 0;
        _lives = Constants.StartingLives;
        _level = 1;
        _isGameRunning = false;
        _isPaused = false;
        _isGameOver = false;
        _isLevelComplete = false;
        _extraLifeThreshold = Constants.ExtraLifeScore;

        // Create engine with required services
        _engine = new GameEngine(
            _logger,
            new MapLoader(_logger),
            new SpriteManager(_logger),
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
        RestartGameCommand = ReactiveCommand.Create(RestartGame);
        SetDirectionCommand = ReactiveCommand.Create<Direction>(SetPacmanDirection);
    }
    /// <summary>
    /// Start the game
    /// </summary>
    public void StartGame()
    {
        try
        {
            _logger.Info($"Starting game at level {_level}");
            _engine.LoadLevel(_level);
            _engine.Start();
            _audioManager.PlayMusic(Constants.BackgroundMusic);
            _audioManager.PlaySoundEffect("game-start");

            IsGameRunning = true;
            IsPaused = false;
            IsGameOver = false;
        }
        catch (Exception ex)
        {
            _logger.Error("Error starting game", ex);
            throw;
        }
    }

    /// <summary>
    /// Updates the game logic for one frame. Called by the View's DispatcherTimer.
    /// </summary>
    public void UpdateGame(float deltaTime)
    {
        try
        {
            if (Engine == null)
            {
                _logger.Error("Game engine is null during update.");
                return;
            }

            Engine.Update(deltaTime);
            _frameCount++;
            if (_frameCount % 60 == 0) // Log every second at 60 FPS
            {
                _logger.Debug($"Game loop running - Frame {_frameCount}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Game loop error: {ex.Message}", ex);
            // Optionally stop the game loop here or trigger game over
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
        _mainWindowViewModel.NavigateTo(new MainMenuViewModel(_mainWindowViewModel, _profileManager, _audioManager, _logger));
    }

    /// <summary>
    /// Restart the game after a game over
    /// </summary>
    private void RestartGame()
    {
        IsGameOver = false;
        Score = 0;
        Lives = Constants.StartingLives;
        Level = 1;
        _extraLifeThreshold = Constants.ExtraLifeScore;
        StartGame();
    }

    private void SetPacmanDirection(Direction direction)
    {
        _engine.SetPacmanDirection(direction);
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
        _logger.Info($"Game Over! Final Score: {Score}");

        FinalScore = Score;
        IsGameOver = true;

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
            _logger.Info($"Extra life awarded at {Score} score. Lives remaining: {Lives}");
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
    private async void OnLevelComplete()
    {
        _audioManager.PlaySoundEffect("level-complete");
        _logger.Info($"Level {Level} complete! Starting level {Level + 1}");

        IsLevelComplete = true;
        await Task.Delay(3000); // 3 second delay
        IsLevelComplete = false;

        Level++;
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
