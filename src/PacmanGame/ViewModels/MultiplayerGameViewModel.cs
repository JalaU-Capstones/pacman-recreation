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
using Direction = PacmanGame.Models.Enums.Direction;

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

    public ICommand PauseGameCommand { get; }
    public ICommand ResumeGameCommand { get; }
    public ICommand ReturnToMenuCommand { get; }
    public ICommand RestartGameCommand { get; }
    public ReactiveCommand<Direction, Unit> SetDirectionCommand { get; }

    public IGameEngine Engine => _gameEngine;

    public MultiplayerGameViewModel(
        MainWindowViewModel mainWindowViewModel,
        int roomId,
        PlayerRole myRole,
        IGameEngine gameEngine,
        IAudioManager audioManager,
        ILogger<MultiplayerGameViewModel> logger,
        NetworkService networkService)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _roomId = roomId;
        _myRole = myRole;
        _gameEngine = gameEngine;
        _audioManager = audioManager;
        _logger = logger;
        _networkService = networkService;

        PauseGameCommand = ReactiveCommand.Create(PauseGame);
        ResumeGameCommand = ReactiveCommand.Create(PauseGame); // Same command toggles
        ReturnToMenuCommand = ReactiveCommand.Create(ReturnToMenu);
        RestartGameCommand = ReactiveCommand.Create(() => { /* TODO: Implement restart for host */ });
        SetDirectionCommand = ReactiveCommand.Create<Direction>(SetPacmanDirection);

        Initialize();
    }

    private void Initialize()
    {
        _logger.LogInformation("[MULTIPLAYER] Initializing game for Room {RoomId} as {MyRole}", _roomId, _myRole);
        _gameEngine.LoadLevel(1);
        _gameEngine.Start();
        _audioManager.PlayMusic("background-theme.wav", loop: true);
        _networkService.OnGameStateUpdate += HandleGameStateUpdate;
        _networkService.OnGameEvent += HandleGameEvent;
        _networkService.OnGamePaused += HandleGamePaused;
        IsGameRunning = true;
        _logger.LogInformation("[MULTIPLAYER] Game initialized successfully");
    }

    public void Render(Canvas canvas)
    {
        _gameEngine.Render(canvas);
    }

    public void HandleKeyPress(Key key)
    {
        _logger.LogDebug("Key Pressed: {Key}", key);
        var direction = key switch
        {
            Key.Up => Direction.Up,
            Key.Down => Direction.Down,
            Key.Left => Direction.Left,
            Key.Right => Direction.Right,
            _ => Direction.None
        };

        if (direction != Direction.None)
        {
            SetDirectionCommand.Execute(direction).Subscribe();
        }
    }

    private void SetPacmanDirection(Direction direction)
    {
        _logger.LogDebug("Direction set to: {Direction}", direction);

        // Only apply client-side prediction if we are controlling the character
        // For Pac-Man, we control Pac-Man
        // For Ghosts, we control our specific ghost
        // However, GameEngine.SetPacmanDirection only controls Pac-Man
        // So we should only call it if we are Pac-Man
        if (_myRole == PlayerRole.Pacman)
        {
            _gameEngine.SetPacmanDirection(direction);
        }

        // Send input to server regardless of role (server decides what to do with it)
        var inputMessage = new PlayerInputMessage
        {
            RoomId = _roomId,
            Direction = (PacmanGame.Shared.Direction)direction,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        _networkService.SendPlayerInput(inputMessage);
    }

    private void PauseGame()
    {
        // Only admin can pause (we assume admin check is done on server or UI visibility)
        // But for now, let's just send the request
        _networkService.SendPauseGameRequest();
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
        // Update Pac-Man (if it exists in the game)
        if (state.PacmanPosition != null)
        {
            // Ensure Pac-Man exists locally if server says it exists
            if (_gameEngine.Pacman == null)
            {
                // We need to re-initialize Pac-Man if it was null but now exists
                // This might happen if we joined late or something
                // For now, let's just assume LoadLevel created it, but if we set it to null earlier, we might need to recreate
                // But since we don't have easy access to spawn point here without reloading level,
                // we'll rely on the fact that LoadLevel creates it.
                // If we set it to null below, we might need a way to restore it.
                // However, usually if Pac-Man exists on server, he exists for the whole game.
            }

            if (_gameEngine.Pacman != null)
            {
                _gameEngine.Pacman.X = (int)state.PacmanPosition.X;
                _gameEngine.Pacman.Y = (int)state.PacmanPosition.Y;
                _gameEngine.Pacman.CurrentDirection = (Direction)state.PacmanPosition.Direction;
            }
        }
        else if (_gameEngine.Pacman != null)
        {
            // Pac-Man role not assigned - remove entity
            _gameEngine.Pacman = null;
        }

        // Update Ghosts (only the ones that exist in the game)
        foreach (var ghostState in state.Ghosts)
        {
            var ghost = _gameEngine.Ghosts.FirstOrDefault(g => g.Type.ToString() == ghostState.Type);

            if (ghost != null)
            {
                // Update existing ghost
                ghost.X = (int)ghostState.Position.X;
                ghost.Y = (int)ghostState.Position.Y;
                ghost.CurrentDirection = (Direction)ghostState.Position.Direction;
                ghost.State = (Models.Enums.GhostState)ghostState.State;
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
            _logger.LogInformation("[MULTIPLAYER] Level changed to {CurrentLevel}", state.CurrentLevel);
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
                break;
            case GameEventType.GameOver:
                _audioManager.PlayMusic("game-over-theme.wav");
                IsGameOver = true;
                break;
        }
        _logger.LogInformation("[MULTIPLAYER] Game event: {EventType}", evt.EventType);
    }

    private void HandleGamePaused(bool isPaused)
    {
        IsPausedByHost = isPaused;
        if (isPaused)
        {
            _gameEngine.Pause();
            _audioManager.PauseMusic();
        }
        else
        {
            _gameEngine.Resume();
            _audioManager.ResumeMusic();
        }
    }
}
