using PacmanGame.Models.Enums;
using PacmanGame.Helpers;
using System;
using Microsoft.Extensions.Logging;

namespace PacmanGame.Models.Entities;

public class Pacman : Entity
{
    public int AnimationFrame { get; set; }
    public bool IsInvulnerable { get; set; }
    public float InvulnerabilityTime { get; set; }
    public bool IsDying { get; set; }
    public float PowerPelletDuration { get; set; }
    private readonly ILogger<Pacman> _logger;

    public Pacman(int x, int y, ILogger<Pacman> logger) : base(x, y)
    {
        _logger = logger;
        Speed = Constants.PacmanSpeed;
        AnimationFrame = 0;
        IsInvulnerable = false;
        InvulnerabilityTime = 0f;
        IsDying = false;
        PowerPelletDuration = Constants.Level1PowerPelletDuration;
    }

    public void Update(float deltaTime, TileType[,] map)
    {
        if (NextDirection != Direction.None && CanMove(NextDirection, map))
        {
            CurrentDirection = NextDirection;
        }

        if (CurrentDirection != Direction.None && CanMove(CurrentDirection, map))
        {
            (int dx, int dy) = GetDirectionDeltas(CurrentDirection);
            ExactX += dx * Speed * deltaTime;
            ExactY += dy * Speed * deltaTime;

            if (ExactX < 0) ExactX = Constants.MapWidth - 0.01f;
            else if (ExactX >= Constants.MapWidth) ExactX = 0;
            if (ExactY < 0) ExactY = Constants.MapHeight - 0.01f;
            else if (ExactY >= Constants.MapHeight) ExactY = 0;

            X = (int)Math.Round(ExactX);
            Y = (int)Math.Round(ExactY);

            IsMoving = true;
        }
        else
        {
            IsMoving = false;
        }

        UpdateInvulnerability(deltaTime);
    }

    public void ActivatePowerPellet()
    {
        IsInvulnerable = true;
        InvulnerabilityTime = PowerPelletDuration;
    }

    public void UpdateInvulnerability(float deltaTime)
    {
        if (IsInvulnerable)
        {
            InvulnerabilityTime -= deltaTime;
            if (InvulnerabilityTime <= 0)
            {
                IsInvulnerable = false;
                InvulnerabilityTime = 0f;
            }
        }
    }

    public override bool CanMove(Direction direction, TileType[,] map)
    {
        float nextX = ExactX;
        float nextY = ExactY;
        const float checkOffset = 0.5f;

        switch (direction)
        {
            case Direction.Up:
                nextY -= checkOffset;
                break;
            case Direction.Down:
                nextY += checkOffset;
                break;
            case Direction.Left:
                nextX -= checkOffset;
                break;
            case Direction.Right:
                nextX += checkOffset;
                break;
            default:
                return false;
        }

        int gridX = (int)nextX;
        int gridY = (int)nextY;

        if (gridY < 0 || gridY >= map.GetLength(0) || gridX < 0 || gridX >= map.GetLength(1))
        {
            return true; // Allow movement into tunnels
        }

        TileType tile = map[gridY, gridX];
        bool isWall = tile == TileType.Wall || tile == TileType.GhostDoor;
        if (isWall)
        {
            _logger.LogDebug($"[Input] Pacman blocked at ({X},{Y}) by wall at ({gridX},{gridY})");
        }
        return !isWall;
    }

    private static (int dx, int dy) GetDirectionDeltas(Direction direction)
    {
        return direction switch
        {
            Direction.Up => (0, -1),
            Direction.Down => (0, 1),
            Direction.Left => (-1, 0),
            Direction.Right => (1, 0),
            _ => (0, 0)
        };
    }
}
