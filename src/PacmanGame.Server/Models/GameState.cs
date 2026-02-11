using PacmanGame.Shared;
using System.Collections.Generic;

namespace PacmanGame.Server.Models;

public class GameState
{
    public Dictionary<int, PlayerState> PlayerStates { get; set; } = new();
    public int Level { get; set; }
    public int Score { get; set; }
    public int Lives { get; set; }
}
