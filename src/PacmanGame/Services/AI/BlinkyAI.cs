using System.Collections.Generic;
using PacmanGame.Models.Entities;
using PacmanGame.Models.Enums;
using PacmanGame.Helpers;
using PacmanGame.Services.Pathfinding;
using Microsoft.Extensions.Logging;

namespace PacmanGame.Services.AI;

/// <summary>
/// Implements Blinky's AI behavior.
/// </summary>
public class BlinkyAI : IGhostAI
{
    private readonly AStarPathfinder _pathfinder = new AStarPathfinder();
#pragma warning disable CS0414
    private Direction _cachedDirection = Direction.None;
    private float _lastPathCalculationTime = 0f;
#pragma warning restore CS0414
    private const float PathCalculationInterval = 0.5f; // Calculate path every 0.5 seconds

    /// <summary>
    /// Blinky's behavior is to directly chase Pac-Man.
    /// </summary>
    public Direction GetNextMove(Ghost ghost, Pacman pacman, TileType[,] map, List<Ghost> allGhosts, bool isChaseMode, ILogger logger)
    {
        int targetY, targetX;

        if (isChaseMode)
        {
            // Chase Mode: Target is Pac-Man's current position.
            targetY = pacman.Y;
            targetX = pacman.X;
        }
        else
        {
            // Scatter Mode: Target is the top-right corner.
            targetY = Constants.BlinkyScatterY;
            targetX = Constants.BlinkyScatterX;
        }
        var next = _pathfinder.FindPath(ghost.Y, ghost.X, targetY, targetX, map, ghost, logger);
        return next;
    }
}
