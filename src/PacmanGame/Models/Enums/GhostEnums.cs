namespace PacmanGame.Models.Enums;

/// <summary>
/// Types of ghosts with different AI behaviors
/// </summary>
public enum GhostType
{
    Blinky = 0,  // Red - Direct chase
    Pinky = 1,   // Pink - Ambush (targets ahead of Pac-Man)
    Inky = 2,    // Cyan - Flanking (complex behavior)
    Clyde = 3    // Orange - Random/scatter
}

/// <summary>
/// States that a ghost can be in
/// </summary>
public enum GhostState
{
    Normal = 0,      // Regular chase/scatter mode
    Vulnerable = 1,  // Blue, can be eaten (after power pellet)
    Warning = 2,     // Flashing blue/white (vulnerability ending)
    Eaten = 3        // Eyes only, returning to base
}
