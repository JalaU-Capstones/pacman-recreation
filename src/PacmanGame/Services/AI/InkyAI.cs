using System;
using System.Collections.Generic;
using System.Linq;
using PacmanGame.Models.Entities;
using PacmanGame.Models.Enums;
using PacmanGame.Helpers;
using PacmanGame.Services.Pathfinding;
using PacmanGame.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace PacmanGame.Services.AI;

/// <summary>
/// Implements Inky's AI behavior.
/// </summary>
public class InkyAI : IGhostAI
{
    private readonly AStarPathfinder _pathfinder = new AStarPathfinder();

    /// <summary>
    /// Inky's behavior is complex, based on Pac-Man's position and Blinky's position.
    /// </summary>
    public Direction GetNextMove(Ghost ghost, Pacman pacman, TileType[,] map, List<Ghost> allGhosts, bool isChaseMode, ILogger logger)
    {
        logger.LogInformation("Calculating next move for Inky.");

        int targetY, targetX;

        if (isChaseMode)
        {
            // Chase Mode: Target is calculated using Blinky's position and Pac-Man's position.
            Ghost? blinky = allGhosts.FirstOrDefault(g => g.Type == GhostType.Blinky);

            if (blinky == null)
            {
                // Fallback to Blinky's behavior if Blinky is not found
                logger.LogWarning("Blinky not found for Inky's AI. Falling back to direct Pac-Man chase.");
                var fallback = _pathfinder.FindPath(ghost.Y, ghost.X, pacman.Y, pacman.X, map, ghost, logger);
                logger.LogDebug($"Inky fallback NextMove={fallback}");
                return fallback;
            }

            // 1. Find "pivot point" = 2 tiles ahead of Pac-Man
            int pivotY = pacman.Y;
            int pivotX = pacman.X;

            switch (pacman.CurrentDirection)
            {
                case Direction.Up:
                    pivotY -= 2;
                    break;
                case Direction.Down:
                    pivotY += 2;
                    break;
                case Direction.Left:
                    pivotX -= 2;
                    break;
                case Direction.Right:
                    pivotX += 2;
                    break;
                case Direction.None:
                    break;
            }

            // 2. Draw vector from Blinky's position to this pivot point
            int vectorX = pivotX - blinky.X;
            int vectorY = pivotY - blinky.Y;

            targetX = pivotX + vectorX;
            targetY = pivotY + vectorY;

            // Clamp target to map bounds
            targetY = Math.Clamp(targetY, 0, map.GetLength(0) - 1);
            targetX = Math.Clamp(targetX, 0, map.GetLength(1) - 1);

            logger.LogDebug($"Inky pivot=({pivotY},{pivotX}) blinky=({blinky.Y},{blinky.X}) vector=({vectorY},{vectorX}) target=({targetY},{targetX})");
        }
        else
        {
            // Scatter Mode: Target is the bottom-right corner.
            targetY = Constants.InkyScatterY;
            targetX = Constants.InkyScatterX;
            logger.LogDebug($"Inky target=({targetY},{targetX}) mode=Scatter");
        }

        var next = _pathfinder.FindPath(ghost.Y, ghost.X, targetY, targetX, map, ghost, logger);
        logger.LogDebug($"Inky NextMove={next}");
        return next;
    }
}
