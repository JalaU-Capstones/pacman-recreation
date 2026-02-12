using System;
using System.Linq;
using System.Reactive;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using Microsoft.Extensions.Logging;
using PacmanGame.Models.Enums;
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

        PauseGameCommand = ReactiveCommand.Create(() => { /* TODO: Implement pause for host */ });
        ResumeGameCommand = ReactiveCommand.Create(() => { /* TODO: Implement resume for host */ });
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
            SetDirectionCommand.Execute(direction);
        }
    }

    private void SetPacmanDirection(Direction direction)
    {
        _logger.LogDebug("Direction set to: {Direction}", direction);
        // Client-side prediction
        _gameEngine.SetPacmanDirection(direction);

        // Send input to server
        var inputMessage = new PlayerInputMessage
        {
            RoomId = _roomId,
            Direction = (PacmanGame.Shared.Direction)direction,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        _networkService.SendPlayerInput(inputMessage);
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
        if (state.PacmanPosition != null)
        {
            _gameEngine.Pacman.X = (int)state.PacmanPosition.X;
            _gameEngine.Pacman.Y = (int)state.PacmanPosition.Y;
            _gameEngine.Pacman.CurrentDirection = (Direction)state.PacmanPosition.Direction;
        }

        foreach (var ghostState in state.Ghosts)
        {
            var ghost = _gameEngine.Ghosts.FirstOrDefault(g => g.Type.ToString() == ghostState.Type);
            if (ghost != null)
            {
                ghost.X = (int)ghostState.Position.X;
                ghost.Y = (int)ghostState.Position.Y;
                ghost.CurrentDirection = (Direction)ghostState.Position.Direction;
                ghost.State = (Models.Enums.GhostState)ghostState.State;
            }
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
}
