using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using PacmanGame.Models.Entities;
using PacmanGame.Models.Enums;

namespace PacmanGame.Services.AI;

/// <summary>
/// Defines the contract for a ghost's artificial intelligence.
/// </summary>
public interface IGhostAI
{
    /// <summary>
    /// Calculates the next move for a ghost based on game state.
    /// </summary>
    /// <param name="ghost">The ghost for which to calculate the move.</param>
    /// <param name="pacman">Pac-Man's current state.</param>
    /// <param name="map">The current game map.</param>
    /// <param name="allGhosts">A list of all ghosts in the game (needed for Inky's AI).</param>
    /// <param name="isChaseMode">True if the ghosts are in chase mode, false for scatter mode.</param>
    /// <param name="logger">The logger instance for logging.</param>
    /// <returns>The recommended direction for the ghost to move.</returns>
    Direction GetNextMove(Ghost ghost, Pacman pacman, TileType[,] map, List<Ghost> allGhosts, bool isChaseMode, ILogger logger);
}
