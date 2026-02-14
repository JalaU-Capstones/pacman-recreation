using System;
using System.Linq;
using System.Reactive;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using Microsoft.Extensions.Logging;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
using PacmanGame.Shared;
using ReactiveUI;
using Direction = PacmanGame.Shared.Direction;

namespace PacmanGame.ViewModels;

public class MultiplayerGameViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IGameEngine _gameEngine;
    private readonly IAudioManager _audioManager;
    private readonly ILogger<MultiplayerGameViewModel> _logger;
    private readonly NetworkService _networkService;

    private readonly int _roomId;
    private readonly PlayerRole _myRole;
    private bool _isAdmin;
    private int _myPlayerId;
    private Direction _currentDirection = Direction.None;

    private int _score;
    public int Score { get => _score; set => this.RaiseAndSetIfChanged(ref _score, value); }

    private int _lives;
    public int Lives { get => _lives; set => this.RaiseAndSetIfChanged(ref _lives, value); }

    private int _level;
    public int Level { get => _level; set => this.RaiseAndSetIfChanged(ref _level, value); }

    private bool _isGameOver;
    public bool IsGameOver { get => _isGameOver; set => this.RaiseAndSetIfChanged(ref _isGameOver, value); }

    private bool _isGameRunning;
    public bool IsGameRunning { get => _isGameRunning; set => this.RaiseAndSetIfChanged(ref _isGameRunning, value); }

    private bool _isPaused;
    public bool IsPaused { get => _isPaused; set => this.RaiseAndSetIfChanged(ref _isPaused, value); }

    private bool _isPausedByHost;
    public bool IsPausedByHost { get => _isPausedByHost; set => this.RaiseAndSetIfChanged(ref _isPausedByHost, value); }

    private bool _isSpectating;
    public bool IsSpectating { get => _isSpectating; set => this.RaiseAndSetIfChanged(ref _isSpectating, value); }

    private bool _isLevelComplete;
    public bool IsLevelComplete { get => _isLevelComplete; set => this.RaiseAndSetIfChanged(ref _isLevelComplete, value); }

    private int _finalScore;
    public int FinalScore { get => _finalScore; set => this.RaiseAndSetIfChanged(ref _finalScore, value); }

    private bool _isVictory;
    public bool IsVictory { get => _isVictory; set => this.RaiseAndSetIfChanged(ref _isVictory, value); }

    public bool IsAdmin { get => _isAdmin; set => this.RaiseAndSetIfChanged(ref _isAdmin, value); }
    public string PauseButtonText => IsPaused ? "RESUME" : "PAUSE";

    public Direction CurrentDirection => _currentDirection;

    public string MyRoleText => _myRole switch
    {
        PlayerRole.Pacman => "YOU ARE: PAC-MAN",
        PlayerRole.Blinky => "YOU ARE: BLINKY (RED GHOST)",
        PlayerRole.Pinky => "YOU ARE: PINKY (PINK GHOST)",
        PlayerRole.Inky => "YOU ARE: INKY (CYAN GHOST)",
        PlayerRole.Clyde => "YOU ARE: CLYDE (ORANGE GHOST)",
        PlayerRole.Spectator => "YOU ARE: SPECTATOR",
        _ => ""
    };

    public string ObjectiveText => _myRole switch
    {
        PlayerRole.Pacman => "Objective: Collect all dots and complete 3 levels. Avoid ghosts!",
        PlayerRole.Blinky or PlayerRole.Pinky or PlayerRole.Inky or PlayerRole.Clyde =>
            "Objective: Catch Pac-Man! Make them lose all 3 lives.",
        PlayerRole.Spectator => "You are watching the game.",
        _ => ""
    };

    public ICommand TogglePauseCommand { get; }
    public ICommand ReturnToMenuCommand { get; }
    public ICommand RestartGameCommand { get; }
    public ReactiveCommand<Direction, Unit> SetDirectionCommand { get; }

    public IGameEngine Engine => _gameEngine;

    public MultiplayerGameViewModel(
        MainWindowViewModel mainWindowViewModel,
        int roomId,
        PlayerRole myRole,
        bool isAdmin,
        IGameEngine gameEngine,
        IAudioManager audioManager,
        ILogger<MultiplayerGameViewModel> logger,
        NetworkService networkService,
        int myPlayerId)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _roomId = roomId;
        _myRole = myRole;
        _isAdmin = isAdmin;
        _gameEngine = gameEngine;
        _audioManager = audioManager;
        _logger = logger;
        _networkService = networkService;
        _myPlayerId = myPlayerId;

        TogglePauseCommand = ReactiveCommand.Create(TogglePause);
        ReturnToMenuCommand = ReactiveCommand.Create(ReturnToMenu);
        RestartGameCommand = ReactiveCommand.Create(RestartGame);
        SetDirectionCommand = ReactiveCommand.Create<Direction>(SetDirection);

        Initialize();
        StartInputPolling();
    }

    private void Initialize()
    {
        _logger.LogInformation("[MULTIPLAYER] Initializing game for Room {RoomId} as {MyRole}", _roomId, _myRole);
        _gameEngine.LoadLevel(1);
        _gameEngine.IsMultiplayerClient = true; // Enable multiplayer client mode

        // Disable local AI for all ghosts in multiplayer
        foreach (var ghost in _gameEngine.Ghosts)
        {
            ghost.IsAIControlled = false;
        }

        _gameEngine.Start();
        _audioManager.PlayMusic("background-theme.wav", loop: true);
        _networkService.OnGameStateUpdate += HandleGameStateUpdate;
        _networkService.OnGameEvent += HandleGameEvent;
        _networkService.OnGamePaused += HandleGamePaused;
        IsGameRunning = true;
        _logger.LogInformation("[MULTIPLAYER] Game initialized successfully");
    }

    private void SetDirection(Direction direction)
    {
        _currentDirection = direction;
        _logger.LogInformation($"[CLIENT-VM] Direction set to: {direction}");
    }

    private void StartInputPolling()
    {
        // Send input to server at 60 FPS
        var inputTimer = new System.Timers.Timer(1000.0 / 60.0);
        inputTimer.Elapsed += (s, e) =>
        {
            if (IsPaused || IsPausedByHost) return;

            // Always send current direction (even if None)
            var inputMessage = new PlayerInputMessage
            {
                RoomId = _roomId,
                PlayerId = _myPlayerId,
                Direction = _currentDirection,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            _networkService.SendPlayerInput(inputMessage);

            if (_currentDirection != Direction.None)
            {
                _logger.LogDebug($"[CLIENT-VM] Sending input: {_currentDirection}");
            }
        };
        inputTimer.Start();

        _logger.LogInformation($"[CLIENT-VM] Input polling started for {_myRole}");
    }

    public void Render(Canvas canvas)
    {
        if (IsPausedByHost) return;
        _gameEngine.Render(canvas);
    }

    private void TogglePause()
    {
        if (IsAdmin)
        {
            _networkService.SendPauseGameRequest();
        }
    }

    private void RestartGame()
    {
        if (IsAdmin)
        {
            _networkService.SendRestartGameRequest();
            // Reset local UI state
            IsGameOver = false;
            IsVictory = false;
            IsLevelComplete = false;
        }
    }

    private void ReturnToMenu()
    {
        _networkService.SendLeaveRoomRequest();
        _gameEngine.Stop();
        _audioManager.StopMusic();
        IsGameRunning = false;
        _mainWindowViewModel.NavigateTo<MainMenuViewModel>();
    }

    private void HandleGameStateUpdate(GameStateMessage state)
    {
        // Auto-navigate to game view if we are in lobby but game has started
        if (_mainWindowViewModel.CurrentViewModel is not MultiplayerGameViewModel)
        {
            _mainWindowViewModel.NavigateTo(this);
        }

        // Update Pac-Man (if it exists in the game)
        if (state.PacmanPosition != null)
        {
            if (_gameEngine.Pacman != null)
            {
                // Snap to server position.
                _gameEngine.Pacman.X = (int)state.PacmanPosition.X;
                _gameEngine.Pacman.Y = (int)state.PacmanPosition.Y;
                _gameEngine.Pacman.ExactX = state.PacmanPosition.X;
                _gameEngine.Pacman.ExactY = state.PacmanPosition.Y;
                _gameEngine.Pacman.CurrentDirection = (Models.Enums.Direction)state.PacmanPosition.Direction;
            }
        }
        else if (_gameEngine.Pacman != null)
        {
            _gameEngine.Pacman = null;
        }

        // Update Ghosts
        foreach (var ghostState in state.Ghosts)
        {
            var ghost = _gameEngine.Ghosts.FirstOrDefault(g => g.Type.ToString() == ghostState.Type);
            if (ghost != null)
            {
                ghost.X = (int)ghostState.Position.X;
                ghost.Y = (int)ghostState.Position.Y;
                ghost.ExactX = ghostState.Position.X;
                ghost.ExactY = ghostState.Position.Y;
                ghost.CurrentDirection = (Models.Enums.Direction)ghostState.Position.Direction;

                // Map server GhostStateEnum to client GhostState
                ghost.State = ghostState.State switch
                {
                    GhostStateEnum.Normal => Models.Enums.GhostState.Normal,
                    GhostStateEnum.Vulnerable => Models.Enums.GhostState.Vulnerable,
                    GhostStateEnum.Eaten => Models.Enums.GhostState.Eaten,
                    _ => Models.Enums.GhostState.Normal
                };
            }
        }

        // Remove ghosts that are no longer in the server state
        var serverGhostTypes = state.Ghosts.Select(g => g.Type).ToHashSet();
        var ghostsToRemove = _gameEngine.Ghosts
            .Where(g => !serverGhostTypes.Contains(g.Type.ToString()))
            .ToList();

        foreach (var ghost in ghostsToRemove)
        {
            _gameEngine.Ghosts.Remove(ghost);
            _logger.LogWarning("[MULTIPLAYER] Removed ghost {Type} (not in server state)", ghost.Type);
        }

        foreach (var collectibleId in state.CollectedItems)
        {
            var collectible = _gameEngine.Collectibles.FirstOrDefault(c => c.Id == collectibleId);
            if (collectible != null)
            {
                collectible.IsActive = false;
            }
        }

        Score = state.Score;
        Lives = state.Lives;

        if (state.CurrentLevel != _gameEngine.CurrentLevel)
        {
            _gameEngine.LoadLevel(state.CurrentLevel);
        }
    }

    private void HandleGameEvent(GameEventMessage evt)
    {
        switch (evt.EventType)
        {
            case GameEventType.DotCollected:
                _audioManager.PlaySoundEffect("chomp.wav");
                break;
            case GameEventType.PowerPelletCollected:
                _audioManager.PlaySoundEffect("eat-power-pellet.wav");
                break;
            case GameEventType.GhostEaten:
                _audioManager.PlaySoundEffect("eat-ghost.wav");
                break;
            case GameEventType.FruitCollected:
                _audioManager.PlaySoundEffect("eat-fruit.wav");
                break;
            case GameEventType.PacmanDied:
                _audioManager.PlaySoundEffect("death.wav");
                break;
            case GameEventType.LevelComplete:
                _audioManager.PlaySoundEffect("level-complete.wav");
                // For multiplayer level 1 only, this is victory for Pacman
                if (_myRole == PlayerRole.Pacman)
                {
                    IsVictory = true;
                    FinalScore = Score;
                }
                else
                {
                    IsGameOver = true; // Ghosts lost
                }
                break;
            case GameEventType.GameOver:
                _audioManager.PlayMusic("game-over-theme.wav");
                if (_myRole == PlayerRole.Pacman)
                {
                    IsGameOver = true; // Pacman lost
                }
                else
                {
                    IsVictory = true; // Ghosts won
                    FinalScore = Score; // Show score anyway
                }
                break;
            case GameEventType.Victory:
                // Explicit victory event if server sends it
                if (_myRole == PlayerRole.Pacman) IsVictory = true;
                else IsGameOver = true;
                break;
        }
        _logger.LogInformation("[MULTIPLAYER] Game event: {EventType}", evt.EventType);
    }

    private void HandleGamePaused(bool isPaused)
    {
        IsPausedByHost = isPaused;
        IsPaused = isPaused;
        this.RaisePropertyChanged(nameof(PauseButtonText));
        if (isPaused)
        {
            _audioManager.PauseMusic();
        }
        else
        {
            _audioManager.ResumeMusic();
        }
    }
}
