using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using PacmanGame.Helpers;
using PacmanGame.Models.CustomLevel;
using PacmanGame.Models.Enums;
using PacmanGame.Services.Interfaces;
using PacmanGame.Services.KeyBindings;
using Avalonia.Input;

namespace PacmanGame.ViewModels;

public class GameViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IProfileManager _profileManager;
    private readonly IGameEngine _engine;
    private readonly IAudioManager _audioManager;
    private readonly IKeyBindingService _keyBindings;
    private readonly ILogger<GameViewModel> _logger;
    private int _extraLifeThreshold;

    private int _score;
    public int Score { get => _score; set => this.RaiseAndSetIfChanged(ref _score, value); }

    private int _lives;
    public int Lives { get => _lives; set => this.RaiseAndSetIfChanged(ref _lives, value); }

    private int _level;
    public int Level { get => _level; set => this.RaiseAndSetIfChanged(ref _level, value); }

    private bool _isGameRunning;
    public bool IsGameRunning { get => _isGameRunning; set => this.RaiseAndSetIfChanged(ref _isGameRunning, value); }

    private bool _isPaused;
    public bool IsPaused { get => _isPaused; set => this.RaiseAndSetIfChanged(ref _isPaused, value); }

    private bool _isGameOver;
    public bool IsGameOver { get => _isGameOver; set => this.RaiseAndSetIfChanged(ref _isGameOver, value); }

    private int _finalScore;
    public int FinalScore { get => _finalScore; set => this.RaiseAndSetIfChanged(ref _finalScore, value); }

    private bool _isLevelComplete;
    public bool IsLevelComplete { get => _isLevelComplete; set => this.RaiseAndSetIfChanged(ref _isLevelComplete, value); }

    private bool _isVictory;
    public bool IsVictory { get => _isVictory; set => this.RaiseAndSetIfChanged(ref _isVictory, value); }

    private string _victoryUnlockMessage = string.Empty;
    public string VictoryUnlockMessage { get => _victoryUnlockMessage; set => this.RaiseAndSetIfChanged(ref _victoryUnlockMessage, value); }

    private bool _showFps;
    public bool ShowFps { get => _showFps; set => this.RaiseAndSetIfChanged(ref _showFps, value); }

    private int _fps;
    public int Fps { get => _fps; set => this.RaiseAndSetIfChanged(ref _fps, value); }

    private string? _customMapPath;
    public string? CustomMapPath { get => _customMapPath; set => this.RaiseAndSetIfChanged(ref _customMapPath, value); }

    private LevelConfig? _customLevelSettings;
    public LevelConfig? CustomLevelSettings { get => _customLevelSettings; set => this.RaiseAndSetIfChanged(ref _customLevelSettings, value); }

    private IReadOnlyList<string>? _customProjectMapPaths;
    public IReadOnlyList<string>? CustomProjectMapPaths { get => _customProjectMapPaths; set => this.RaiseAndSetIfChanged(ref _customProjectMapPaths, value); }

    private IReadOnlyList<LevelConfig>? _customProjectLevelSettings;
    public IReadOnlyList<LevelConfig>? CustomProjectLevelSettings { get => _customProjectLevelSettings; set => this.RaiseAndSetIfChanged(ref _customProjectLevelSettings, value); }

    private int _customProjectWinScore;
    public int CustomProjectWinScore { get => _customProjectWinScore; set => this.RaiseAndSetIfChanged(ref _customProjectWinScore, value); }

    private int _customProjectLevelIndex;
    private int _initialLives;
    private bool _customProjectVictoryBonusAwarded;

    public IGameEngine Engine => _engine;

    public ICommand PauseGameCommand { get; }
    public ICommand ResumeGameCommand { get; }
    public ICommand ReturnToMenuCommand { get; }
    public ICommand RestartGameCommand { get; }
    public ICommand ToggleFpsCommand { get; }
    public ReactiveCommand<Direction, Unit> SetDirectionCommand { get; }

    public GameViewModel(MainWindowViewModel mainWindowViewModel, IProfileManager profileManager, IAudioManager audioManager, IGameEngine gameEngine, IKeyBindingService keyBindings, ILogger<GameViewModel> logger)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _profileManager = profileManager;
        _engine = gameEngine;
        _audioManager = audioManager;
        _keyBindings = keyBindings;
        _logger = logger;

        _score = 0;
        _lives = Constants.StartingLives;
        _level = 1;
        _isGameRunning = false;
        _isPaused = false;
        _isGameOver = false;
        _isLevelComplete = false;
        _isVictory = false;
        _extraLifeThreshold = Constants.ExtraLifeScore;
        _initialLives = _lives;

        _engine.ScoreChanged += HandleScoreChanged;
        _engine.LifeLost += OnLifeLost;
        _engine.LevelComplete += OnLevelComplete;
        _engine.GameOver += OnGameOver;
        _engine.Victory += OnVictory;

        PauseGameCommand = ReactiveCommand.Create(PauseGame);
        ResumeGameCommand = ReactiveCommand.Create(ResumeGame);
        ReturnToMenuCommand = ReactiveCommand.Create(ReturnToMenu);
        RestartGameCommand = ReactiveCommand.Create(RestartGame);
        ToggleFpsCommand = ReactiveCommand.Create(() => ShowFps = !ShowFps);
        SetDirectionCommand = ReactiveCommand.Create<Direction>(SetPacmanDirection);
    }

    public bool IsActionTriggered(string action, Key key, KeyModifiers modifiers)
    {
        return _keyBindings.IsActionTriggered(action, key, modifiers);
    }

    public void StartGame()
    {
        try
        {
            _logger.LogInformation("Starting game at level {Level}", Level);
            _initialLives = Lives;
            if (CustomProjectMapPaths != null && CustomProjectMapPaths.Count > 0)
            {
                _customProjectLevelIndex = 0;
                Level = 1;
                _engine.LoadCustomLevel(CustomProjectMapPaths[0]);
                var levelCfg = (CustomProjectLevelSettings != null && CustomProjectLevelSettings.Count > 0)
                    ? CustomProjectLevelSettings[0]
                    : null;
                if (levelCfg != null)
                {
                    _engine.ApplyCustomLevelSettings(levelCfg);
                }
            }
            else if (!string.IsNullOrWhiteSpace(CustomMapPath))
            {
                _engine.LoadCustomLevel(CustomMapPath);
                if (CustomLevelSettings != null)
                {
                    _engine.ApplyCustomLevelSettings(CustomLevelSettings);
                }
            }
            else
            {
                _engine.LoadLevel(Level);
            }
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
            _logger.LogError(ex, "Error starting game");
            throw;
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
        _mainWindowViewModel.NavigateTo<MainMenuViewModel>();
    }

    private void RestartGame()
    {
        IsGameOver = false;
        IsVictory = false;
        VictoryUnlockMessage = string.Empty;
        Score = 0;
        Lives = _initialLives;
        Level = 1;
        _customProjectLevelIndex = 0;
        _customProjectVictoryBonusAwarded = false;
        _extraLifeThreshold = Constants.ExtraLifeScore;
        StartGame();
    }

    private void SetPacmanDirection(Direction direction)
    {
        // _logger.LogDebug("ViewModel received direction: {Direction}", direction);
        _engine.SetPacmanDirection(direction);
    }

    // Renamed to avoid confusion with event handler
    private void HandleGameOver()
    {
        IsGameRunning = false;
        _engine.Stop();
        _audioManager.StopMusic();
        _audioManager.PlayMusic(Constants.GameOverMusic, loop: false);
        _logger.LogInformation("Game Over! Final Score: {Score}", Score);

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
            _logger.LogInformation("Extra life awarded at {Score} score. Lives remaining: {Lives}", Score, Lives);
        }
    }

    private void OnLifeLost()
    {
        Lives--;
        if (Lives <= 0)
        {
            HandleGameOver();
        }
    }

    private async void OnLevelComplete()
    {
        _audioManager.PlaySoundEffect("level-complete");
        _logger.LogInformation("Level {Level} complete! Starting level {NextLevel}", Level, Level + 1);

        // Engine freezes itself immediately when the last dot is eaten; keep view state in sync.
        IsPaused = true;
        IsLevelComplete = true;
        await Task.Delay(3000);
        IsLevelComplete = false;

        if (CustomProjectMapPaths != null && CustomProjectMapPaths.Count > 0)
        {
            _customProjectLevelIndex++;
            Level = _customProjectLevelIndex + 1;
            if (_customProjectLevelIndex >= CustomProjectMapPaths.Count)
            {
                OnVictory();
                return;
            }

            _engine.LoadCustomLevel(CustomProjectMapPaths[_customProjectLevelIndex]);
            if (CustomProjectLevelSettings != null && _customProjectLevelIndex < CustomProjectLevelSettings.Count)
            {
                _engine.ApplyCustomLevelSettings(CustomProjectLevelSettings[_customProjectLevelIndex]);
            }

            _engine.Resume();
            IsPaused = false;
            return;
        }

        Level++;

        if (Level > 3)
        {
            // Game completed!
            OnVictory();
        }
        else
        {
            _engine.LoadLevel(Level);
            _engine.Resume();
            IsPaused = false;
        }
    }

    private void OnGameOver()
    {
        HandleGameOver();
    }

    private void OnVictory()
    {
        if (CustomProjectMapPaths != null && CustomProjectMapPaths.Count > 0 &&
            CustomProjectWinScore > 0 && !_customProjectVictoryBonusAwarded)
        {
            Score += CustomProjectWinScore;
            _customProjectVictoryBonusAwarded = true;
            _logger.LogInformation("Custom project victory bonus awarded: {Bonus}", CustomProjectWinScore);
        }

        _ = TryMarkAllLevelsCompletedAsync();

        IsPaused = true;
        IsGameRunning = false;
        _engine.Stop();
        _audioManager.StopMusic();
        _audioManager.PlaySoundEffect("level-complete");
        _logger.LogInformation("Victory! Final Score: {Score}", Score);

        FinalScore = Score;
        IsVictory = true;

        var activeProfile = _profileManager.GetActiveProfile();
        if (activeProfile != null)
        {
            _profileManager.SaveScore(activeProfile.Id, Score, Level);
        }
    }

    private async Task TryMarkAllLevelsCompletedAsync()
    {
        try
        {
            // Only permanent-unlock after finishing the built-in campaign (levels 1-3) in single-player.
            if (_engine.IsMultiplayerClient)
            {
                return;
            }

            if (CustomProjectMapPaths != null && CustomProjectMapPaths.Count > 0)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(CustomMapPath))
            {
                return;
            }

            if (_engine.CurrentLevel != 3)
            {
                return;
            }

            var profile = await _profileManager.GetCurrentProfileAsync();
            if (profile == null)
            {
                return;
            }

            if (profile.HasCompletedAllLevels)
            {
                return;
            }

            profile.HasCompletedAllLevels = true;
            await _profileManager.UpdateProfileAsync(profile);

            VictoryUnlockMessage = "Creative Mode and Global Leaderboard unlocked!";
            _logger.LogInformation("Profile {Name} has completed all levels; unlocks granted.", profile.Name);
        }
        catch (Exception ex)
        {
            // Don't fail victory flow if persistence fails; just log.
            _logger.LogError(ex, "Failed to update HasCompletedAllLevels on victory");
        }
    }
}
