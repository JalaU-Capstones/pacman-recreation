using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using PacmanGame.Helpers;
using PacmanGame.Models.Enums;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;

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

    private int _score;
    public int Score
    {
        get => _score;
        set => this.RaiseAndSetIfChanged(ref _score, value);
    }

    private int _lives;
    public int Lives
    {
        get => _lives;
        set => this.RaiseAndSetIfChanged(ref _lives, value);
    }

    private int _level;
    public int Level
    {
        get => _level;
        set => this.RaiseAndSetIfChanged(ref _level, value);
    }

    private bool _isGameRunning;
    public bool IsGameRunning
    {
        get => _isGameRunning;
        set => this.RaiseAndSetIfChanged(ref _isGameRunning, value);
    }

    private bool _isPaused;
    public bool IsPaused
    {
        get => _isPaused;
        set => this.RaiseAndSetIfChanged(ref _isPaused, value);
    }

    private bool _isGameOver;
    public bool IsGameOver
    {
        get => _isGameOver;
        set => this.RaiseAndSetIfChanged(ref _isGameOver, value);
    }

    private int _finalScore;
    public int FinalScore
    {
        get => _finalScore;
        set => this.RaiseAndSetIfChanged(ref _finalScore, value);
    }

    private bool _isLevelComplete;
    public bool IsLevelComplete
    {
        get => _isLevelComplete;
        set => this.RaiseAndSetIfChanged(ref _isLevelComplete, value);
    }

    private bool _isVictory;
    public bool IsVictory
    {
        get => _isVictory;
        set => this.RaiseAndSetIfChanged(ref _isVictory, value);
    }

    public IGameEngine Engine => _engine;

    public ICommand PauseGameCommand { get; }
    public ICommand ResumeGameCommand { get; }
    public ICommand ReturnToMenuCommand { get; }
    public ICommand RestartGameCommand { get; }
    public ReactiveCommand<Direction, Unit> SetDirectionCommand { get; }

    public GameViewModel(MainWindowViewModel mainWindowViewModel, IProfileManager profileManager, IAudioManager audioManager, ILogger logger)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _profileManager = profileManager;
        _audioManager = audioManager;
        _logger = logger;
        _frameCount = 0;

        _score = 0;
        _lives = Constants.StartingLives;
        _level = 1;
        _isGameRunning = false;
        _isPaused = false;
        _isGameOver = false;
        _isLevelComplete = false;
        _isVictory = false;
        _extraLifeThreshold = Constants.ExtraLifeScore;

        _engine = new GameEngine(
            _logger,
            new MapLoader(_logger),
            new SpriteManager(_logger),
            _audioManager,
            new CollisionDetector());

        _engine.ScoreChanged += HandleScoreChanged;
        _engine.LifeLost += OnLifeLost;
        _engine.LevelComplete += OnLevelComplete;
        _engine.GameOver += OnGameOver;
        _engine.Victory += OnVictory;

        PauseGameCommand = ReactiveCommand.Create(PauseGame);
        ResumeGameCommand = ReactiveCommand.Create(ResumeGame);
        ReturnToMenuCommand = ReactiveCommand.Create(ReturnToMenu);
        RestartGameCommand = ReactiveCommand.Create(RestartGame);
        SetDirectionCommand = ReactiveCommand.Create<Direction>(SetPacmanDirection);
    }

    public void StartGame()
    {
        try
        {
            _logger.Info($"Starting game at level {Level}");
            _engine.LoadLevel(Level);
            _engine.Start();
            _audioManager.PlayMusic(Constants.BackgroundMusic);
            _audioManager.PlaySoundEffect("game-start");

            IsGameRunning = true;
            IsPaused = false;
            IsGameOver = false;
            IsVictory = false;
        }
        catch (Exception ex)
        {
            _logger.Error("Error starting game", ex);
            throw;
        }
    }

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
            if (_frameCount % 60 == 0)
            {
                _logger.Debug($"Game loop running - Frame {_frameCount}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Game loop error: {ex.Message}", ex);
        }
    }

    private void PauseGame()
    {
        if (IsGameRunning && !IsPaused)
        {
            _engine.Pause();
            IsPaused = true;
            _audioManager.PauseMusic();
        }
    }

    private void ResumeGame()
    {
        if (IsGameRunning && IsPaused)
        {
            _engine.Resume();
            IsPaused = false;
            _audioManager.ResumeMusic();
        }
    }

    private void ReturnToMenu()
    {
        _engine.Stop();
        _audioManager.StopMusic();
        IsGameRunning = false;
        _mainWindowViewModel.NavigateTo(new MainMenuViewModel(_mainWindowViewModel, _profileManager, _audioManager, _logger));
    }

    private void RestartGame()
    {
        IsGameOver = false;
        IsVictory = false;
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

    public void GameOver()
    {
        IsGameRunning = false;
        _engine.Stop();
        _audioManager.StopMusic();
        _audioManager.PlayMusic(Constants.GameOverMusic, loop: false);
        _logger.Info($"Game Over! Final Score: {Score}");

        FinalScore = Score;
        IsGameOver = true;

        var activeProfile = _profileManager.GetActiveProfile();
        if (activeProfile != null)
        {
            _profileManager.SaveScore(activeProfile.Id, Score, Level);
        }
    }

    private void HandleScoreChanged(int points)
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

    private void OnLifeLost()
    {
        Lives--;
        if (Lives <= 0)
        {
            GameOver();
        }
    }

    private async void OnLevelComplete()
    {
        _audioManager.PlaySoundEffect("level-complete");
        _logger.Info($"Level {Level} complete! Starting level {Level + 1}");

        IsLevelComplete = true;
        await Task.Delay(3000);
        IsLevelComplete = false;

        Level++;
        _engine.LoadLevel(Level);
    }

    private void OnGameOver()
    {
        GameOver();
    }

    private void OnVictory()
    {
        IsGameRunning = false;
        _engine.Stop();
        _audioManager.StopMusic();
        _audioManager.PlaySoundEffect("level-complete");
        _logger.Info($"Victory! Final Score: {Score}");

        FinalScore = Score;
        IsVictory = true;

        var activeProfile = _profileManager.GetActiveProfile();
        if (activeProfile != null)
        {
            _profileManager.SaveScore(activeProfile.Id, Score, Level);
        }
    }
}
