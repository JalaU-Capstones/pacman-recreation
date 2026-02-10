using System;
using System.Collections.Generic;
using PacmanGame.Models.Entities;
using PacmanGame.Models.Enums;
using PacmanGame.Helpers;
using PacmanGame.Services.Pathfinding;

namespace PacmanGame.Services.AI;

/// <summary>
/// Implements Clyde's AI behavior.
/// </summary>
public class ClydeAI : IGhostAI
{
    private readonly AStarPathfinder _pathfinder = new AStarPathfinder();

    /// <summary>
    /// Clyde's behavior is to chase Pac-Man when far away, but scatter when close.
    /// </summary>
    public Direction GetNextMove(Ghost ghost, Pacman pacman, TileType[,] map, List<Ghost> allGhosts, bool isChaseMode)
    {
        int targetY, targetX;

        if (isChaseMode)
        {
            // Chase Mode: Target depends on distance to Pac-Man.
            double distance = Math.Sqrt(Math.Pow(ghost.X - pacman.X, 2) + Math.Pow(ghost.Y - pacman.Y, 2));

            if (distance > Constants.ClydeShyDistance)
            {
                // If far from Pac-Man, chase directly (like Blinky).
                targetY = pacman.Y;
                targetX = pacman.X;
            }
            else
            {
                // If close to Pac-Man, retreat to scatter target.
                targetY = Constants.ClydeScatterY;
                targetX = Constants.ClydeScatterX;
            }
        }
        else
        {
            // Scatter Mode: Target is the bottom-left corner.
            targetY = Constants.ClydeScatterY;
            targetX = Constants.ClydeScatterX;
        }

        return _pathfinder.FindPath(ghost.Y, ghost.X, targetY, targetX, map, ghost);
    }
}
