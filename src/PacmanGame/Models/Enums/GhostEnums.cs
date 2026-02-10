namespace PacmanGame.Models.Enums;

/// <summary>
/// The type of ghost, which determines its AI behavior.
/// </summary>
public enum GhostType
{
    Blinky, // Red
    Pinky,  // Pink
    Inky,   // Cyan
    Clyde   // Orange
}

/// <summary>
/// The current state of a ghost, which affects its behavior and appearance.
/// </summary>
public enum GhostState
{
    /// <summary>
    /// The ghost is in the ghost house, waiting to exit.
    /// </summary>
    InHouse,
    /// <summary>
    /// The ghost is actively leaving the ghost house.
    /// </summary>
    ExitingHouse,
    /// <summary>
    /// The ghost is actively chasing or scattering.
    /// </summary>
    Normal,
    /// <summary>
    /// The ghost is vulnerable after Pac-Man ate a power pellet.
    /// </summary>
    Vulnerable,
    /// <summary>
    /// The ghost is flashing, indicating vulnerability is about to end.
    /// </summary>
    Warning,
    /// <summary>
    /// The ghost has been eaten and is returning to the ghost house.
    /// </summary>
    Eaten
}
