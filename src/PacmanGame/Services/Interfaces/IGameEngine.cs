using PacmanGame.Models.Entities;
using PacmanGame.Models.Enums;
using System;
using System.Collections.Generic;

namespace PacmanGame.Services.Interfaces;

/// <summary>
/// Interface for the game engine that manages all game logic and updates
/// </summary>
public interface IGameEngine
{
    /// <summary>
    /// Event raised when the score changes
    /// </summary>
    event Action<int>? ScoreChanged;

    /// <summary>
    /// Event raised when a life is lost
    /// </summary>
    event Action? LifeLost;

    /// <summary>
    /// Event raised when a level is completed
    /// </summary>
    event Action? LevelComplete;

    /// <summary>
    /// Event raised when the game is over
    /// </summary>
    event Action? GameOver;

    /// <summary>
    /// Event raised when the player achieves victory
    /// </summary>
    event Action? Victory;

    /// <summary>
    /// Get the current game map
    /// </summary>
    TileType[,] Map { get; }

    /// <summary>
    /// Get Pac-Man instance
    /// </summary>
    Pacman Pacman { get; }

    /// <summary>
    /// Get the list of ghosts
    /// </summary>
    List<Ghost> Ghosts { get; }

    /// <summary>
    /// Get the list of collectibles
    /// </summary>
    List<Collectible> Collectibles { get; }

    /// <summary>
    /// Load a specific level
    /// </summary>
    /// <param name="level">Level number to load</param>
    void LoadLevel(int level);

    /// <summary>
    /// Start the game
    /// </summary>
    void Start();

    /// <summary>
    /// Stop the game
    /// </summary>
    void Stop();

    /// <summary>
    /// Pause the game
    /// </summary>
    void Pause();

    /// <summary>
    /// Resume a paused game
    /// </summary>
    void Resume();

    /// <summary>
    /// Set the desired direction for Pac-Man
    /// </summary>
    /// <param name="direction">Direction to move</param>
    void SetPacmanDirection(Direction direction);

    /// <summary>
    /// Update game logic for a single frame
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame in seconds</param>
    void Update(float deltaTime);
}
