using System;
using System.Reactive;
using System.Windows.Input;
using PacmanGame.Services;
using PacmanGame.Shared;
using ReactiveUI;

namespace PacmanGame.ViewModels;

public class MultiplayerGameViewModel : ViewModelBase
{
    private readonly NetworkService _networkService;

    private int _score;
    public int Score
    {
        get => _score;
        set => this.RaiseAndSetIfChanged(ref _score, value);
    }

    private int _level;
    public int Level
    {
        get => _level;
        set => this.RaiseAndSetIfChanged(ref _level, value);
    }

    private int _lives;
    public int Lives
    {
        get => _lives;
        set => this.RaiseAndSetIfChanged(ref _lives, value);
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

    private bool _isPausedByHost;
    public bool IsPausedByHost
    {
        get => _isPausedByHost;
        set => this.RaiseAndSetIfChanged(ref _isPausedByHost, value);
    }

    private bool _isSpectating;
    public bool IsSpectating
    {
        get => _isSpectating;
        set => this.RaiseAndSetIfChanged(ref _isSpectating, value);
    }

    private bool _isLevelComplete;
    public bool IsLevelComplete
    {
        get => _isLevelComplete;
        set => this.RaiseAndSetIfChanged(ref _isLevelComplete, value);
    }

    private bool _isGameOver;
    public bool IsGameOver
    {
        get => _isGameOver;
        set => this.RaiseAndSetIfChanged(ref _isGameOver, value);
    }

    private bool _isVictory;
    public bool IsVictory
    {
        get => _isVictory;
        set => this.RaiseAndSetIfChanged(ref _isVictory, value);
    }

    private string _finalScore = string.Empty;
    public string FinalScore
    {
        get => _finalScore;
        set => this.RaiseAndSetIfChanged(ref _finalScore, value);
    }

    public ReactiveCommand<Unit, Unit> PauseGameCommand { get; }
    public ReactiveCommand<Unit, Unit> ResumeGameCommand { get; }
    public ReactiveCommand<Unit, Unit> ReturnToMenuCommand { get; }
    public ReactiveCommand<Unit, Unit> RestartGameCommand { get; }
    public ReactiveCommand<Direction, Unit> SetDirectionCommand { get; }

    public MultiplayerGameViewModel(NetworkService networkService)
    {
        _networkService = networkService;
        _networkService.OnGameStateUpdate += OnGameStateUpdate;

        PauseGameCommand = ReactiveCommand.Create(() => { /* Send pause request */ });
        ResumeGameCommand = ReactiveCommand.Create(() => { /* Send resume request */ });
        ReturnToMenuCommand = ReactiveCommand.Create(() => { /* Navigate to main menu */ });
        RestartGameCommand = ReactiveCommand.Create(() => { /* Send restart request */ });
        SetDirectionCommand = ReactiveCommand.Create<Direction>(SetDirection);
    }

    private void SetDirection(Direction direction)
    {
        var input = new PlayerInputMessage
        {
            Direction = direction,
            Timestamp = DateTime.UtcNow.Ticks
        };
        _networkService.SendPlayerInput(input);
    }

    private void OnGameStateUpdate(GameStateMessage message)
    {
        Score = message.Score;
        Level = message.Level;
        Lives = message.Lives;
        // Update player positions and other state from message.PlayerStates
    }
}
