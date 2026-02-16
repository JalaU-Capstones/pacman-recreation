using System;
using System.Collections.Generic;
using PacmanGame.Models.Entities;
using PacmanGame.Models.Enums;
using PacmanGame.Helpers;
using PacmanGame.Services.Pathfinding;
using Microsoft.Extensions.Logging;

namespace PacmanGame.Services.AI;

/// <summary>
/// Implements Pinky's AI behavior.
/// </summary>
public class PinkyAI : IGhostAI
{
    private readonly AStarPathfinder _pathfinder = new AStarPathfinder();

    /// <summary>
    /// Pinky's behavior is to ambush Pac-Man by targeting 4 tiles ahead of Pac-Man's direction.
    /// </summary>
    public Direction GetNextMove(Ghost ghost, Pacman pacman, TileType[,] map, List<Ghost> allGhosts, bool isChaseMode, ILogger logger)
    {
        int targetY, targetX;

        if (isChaseMode)
        {
            // Chase Mode: Target 4 tiles ahead of Pac-Man's direction.
            targetY = pacman.Y;
            targetX = pacman.X;

            switch (pacman.CurrentDirection)
            {
                case Direction.Up:
                    targetY -= 4;
                    break;
                case Direction.Down:
                    targetY += 4;
                    break;
                case Direction.Left:
                    targetX -= 4;
                    break;
                case Direction.Right:
                    targetX += 4;
                    break;
                case Direction.None:
                    break;
            }

            // Clamp target to map bounds
            targetY = Math.Clamp(targetY, 0, map.GetLength(0) - 1);
            targetX = Math.Clamp(targetX, 0, map.GetLength(1) - 1);
        }
        else
        {
            // Scatter Mode: Target is the top-left corner.
            targetY = Constants.PinkyScatterY;
            targetX = Constants.PinkyScatterX;
        }

        var next = _pathfinder.FindPath(ghost.Y, ghost.X, targetY, targetX, map, ghost, logger);
        return next;
    }
}
