using System;
using System.Linq;
using System.Reactive;
using System.Windows.Input;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using PacmanGame.Services;
using PacmanGame.Services.Interfaces;
using PacmanGame.Shared;
using ReactiveUI;
using Direction = PacmanGame.Shared.Direction;
using PacmanGame.Models.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace PacmanGame.ViewModels;

public class MultiplayerGameViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IGameEngine _gameEngine;
    private readonly IAudioManager _audioManager;
    private readonly ILogger<MultiplayerGameViewModel> _logger;
    private readonly NetworkService _networkService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IProfileManager _profileManager;

    private readonly int _roomId;
    private PlayerRole _myRole;
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

    private bool _isSpectatorPromotion;
    public bool IsSpectatorPromotion { get => _isSpectatorPromotion; set => this.RaiseAndSetIfChanged(ref _isSpectatorPromotion, value); }

    private string _spectatorPromotionMessage = string.Empty;
    public string SpectatorPromotionMessage { get => _spectatorPromotionMessage; set => this.RaiseAndSetIfChanged(ref _spectatorPromotionMessage, value); }

    private int _spectatorPromotionTimer;
    public int SpectatorPromotionTimer { get => _spectatorPromotionTimer; set => this.RaiseAndSetIfChanged(ref _spectatorPromotionTimer, value); }

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

    public string GameOverTitle => _myRole switch
    {
        PlayerRole.Pacman => "GAME OVER",
        PlayerRole.Spectator => "GAME OVER",
        _ => "VICTORY!"
    };

    public string GameOverMessage => _myRole switch
    {
        PlayerRole.Pacman => "You lost all your lives!",
        PlayerRole.Spectator => "Pac-Man lost all lives.",
        _ => "You caught Pac-Man!"
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
        int myPlayerId,
        IServiceProvider serviceProvider)
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
        _serviceProvider = serviceProvider;
        _profileManager = serviceProvider.GetRequiredService<IProfileManager>();

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
        _networkService.OnSpectatorPromotion += HandleSpectatorPromotion;
        _networkService.OnGameStart += HandleGameStart; // Handle restart/new game start
        _networkService.OnRoomStateUpdate += HandleRoomStateUpdate; // Listen for admin changes
        _networkService.OnNewPlayerJoined += HandleNewPlayerJoined;
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
            IsGameOver = false;
            IsVictory = false;
            IsLevelComplete = false;
            _currentDirection = Direction.None;
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
                    ApplyScoreRewards(true);
                }
                else if (_myRole == PlayerRole.Spectator)
                {
                    IsVictory = true; // Spectator sees victory if Pacman wins
                    FinalScore = Score;
                }
                else
                {
                    IsGameOver = true; // Ghosts lost
                    ApplyScoreRewards(false);
                }
                break;
            case GameEventType.GameOver:
                _audioManager.PlayMusic("game-over-theme.wav");
                if (_myRole == PlayerRole.Pacman)
                {
                    IsGameOver = true; // Pacman lost
                    ApplyScoreRewards(false);
                }
                else if (_myRole == PlayerRole.Spectator)
                {
                    IsGameOver = true; // Spectator sees game over
                    FinalScore = Score;
                }
                else
                {
                    IsVictory = true; // Ghosts won
                    FinalScore = Score; // Show score anyway
                    ApplyScoreRewards(true);
                }
                break;
            case GameEventType.Victory:
                // Explicit victory event if server sends it
                if (_myRole == PlayerRole.Pacman)
                {
                    IsVictory = true;
                    ApplyScoreRewards(true);
                }
                else
                {
                    IsGameOver = true;
                    ApplyScoreRewards(false);
                }
                break;
        }
        _logger.LogInformation("[MULTIPLAYER] Game event: {EventType}", evt.EventType);

        // Update UI properties based on role changes or game state
        this.RaisePropertyChanged(nameof(GameOverTitle));
        this.RaisePropertyChanged(nameof(GameOverMessage));
    }

    private void ApplyScoreRewards(bool isWinner)
    {
        var profile = _profileManager.GetActiveProfile();
        if (profile == null) return;

        int scoreAdjustment = 0;

        if (_myRole == PlayerRole.Pacman)
        {
            if (isWinner)
            {
                // Pac-Man wins: Game points + 5000 bonus
                scoreAdjustment = Score + 5000;
            }
            else
            {
                // Pac-Man loses: Game points only
                scoreAdjustment = Score;
            }
        }
        else if (_myRole != PlayerRole.Spectator && _myRole != PlayerRole.None)
        {
            // Ghost roles
            if (isWinner)
            {
                // Ghosts win (Pac-Man lost): +1200 points
                scoreAdjustment = 1200;
            }
            else
            {
                // Ghosts lose (Pac-Man won): Penalty equal to what Pac-Man ate
                // Note: Score tracks Pac-Man's score, so we subtract it
                scoreAdjustment = -Score;
            }
        }

        if (scoreAdjustment != 0)
        {
            // We need to fetch the current high score to add/subtract from it
            // But SaveScore only saves if higher.
            // For multiplayer, we might want a cumulative score or just update the high score?
            // The requirement says: "Total added to their local high score"
            // This implies we treat it as experience points or we just update the high score if the new total is higher?
            // "Added to their local high score" suggests accumulation, but our system is High Score based (max score).
            // Let's assume we add it to a "Multiplayer Score" or just try to save it as a new high score entry?
            // Re-reading: "Total added to their local high score"
            // If I have 10,000 high score, and I win 15,000 points, my new high score should be 25,000?
            // Or is it just a new score submission of 15,000?
            // "Added to their local high score" usually means HighScore += NewPoints.

            // Let's implement it as: NewPotentialHighScore = CurrentHighScore + scoreAdjustment

            var currentProfile = _profileManager.GetProfileById(profile.Id);
            if (currentProfile != null)
            {
                int currentHighScore = currentProfile.HighScore; // This comes from MAX(Score) in DB
                int newScore = currentHighScore + scoreAdjustment;

                if (newScore < 0) newScore = 0; // No negative scores

                _profileManager.SaveScore(profile.Id, newScore, 1); // Level 1 for multiplayer
                _logger.LogInformation($"[MULTIPLAYER] Score adjustment: {scoreAdjustment}. New High Score: {newScore}");
            }
        }
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

    private void HandleSpectatorPromotion(SpectatorPromotionEvent evt)
    {
        _myRole = evt.NewRole;
        IsSpectating = false;
        IsSpectatorPromotion = true;
        SpectatorPromotionMessage = $"You are taking over for {evt.PreviousPlayerName} as {evt.NewRole}!";
        SpectatorPromotionTimer = evt.PreparationTimeSeconds;

        this.RaisePropertyChanged(nameof(MyRoleText));
        this.RaisePropertyChanged(nameof(ObjectiveText));

        // Start countdown timer
        var timer = new System.Timers.Timer(1000);
        timer.Elapsed += (s, e) =>
        {
            SpectatorPromotionTimer--;
            if (SpectatorPromotionTimer <= 0)
            {
                IsSpectatorPromotion = false;
                timer.Stop();
                timer.Dispose();
            }
        };
        timer.Start();
    }

    private void HandleGameStart(GameStartEvent evt)
    {
        // Reset local state for new game
        IsGameOver = false;
        IsVictory = false;
        IsLevelComplete = false;
        _currentDirection = Direction.None; // Reset direction

        // Update role from the event
        var myState = evt.PlayerStates.FirstOrDefault(p => p.PlayerId == _myPlayerId);
        if (myState != null)
        {
            _myRole = myState.Role;
            IsSpectating = _myRole == PlayerRole.Spectator;
            this.RaisePropertyChanged(nameof(MyRoleText));
            this.RaisePropertyChanged(nameof(ObjectiveText));
        }

        _logger.LogInformation($"[CLIENT-VM] Game restarted. New role: {_myRole}");
    }

    private void HandleRoomStateUpdate(System.Collections.Generic.List<PlayerState> players)
    {
        var myState = players.FirstOrDefault(p => p.PlayerId == _myPlayerId);
        if (myState != null)
        {
            IsAdmin = myState.IsAdmin;
            _logger.LogInformation($"[CLIENT-VM] Room state updated. IsAdmin: {IsAdmin}");
        }
    }

    private void HandleNewPlayerJoined(NewPlayerJoinedEvent evt)
    {
        _logger.LogInformation($"[CLIENT-VM] New player {evt.PlayerName} joined as {evt.Role}");

        if (evt.Role == PlayerRole.Pacman)
        {
            if (_gameEngine.Pacman == null)
            {
                var pacmanLogger = _serviceProvider.GetRequiredService<ILogger<Pacman>>();
                _gameEngine.Pacman = new Pacman(0, 0, pacmanLogger); // Position will be updated by GameState
                _logger.LogInformation("[CLIENT-VM] Spawned new Pacman for new player.");
            }
        }
        else if (evt.Role != PlayerRole.Spectator && evt.Role != PlayerRole.None)
        {
            var ghostType = (Models.Enums.GhostType)Enum.Parse(typeof(Models.Enums.GhostType), evt.Role.ToString());
            if (!_gameEngine.Ghosts.Any(g => g.Type == ghostType))
            {
                var newGhost = new Ghost(0, 0, ghostType) { IsAIControlled = false };
                _gameEngine.Ghosts.Add(newGhost);
                _logger.LogInformation($"[CLIENT-VM] Spawned new ghost {ghostType} for new player.");
            }
        }
    }
}
