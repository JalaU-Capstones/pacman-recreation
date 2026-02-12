using System.Threading;
using LiteNetLib;
using PacmanGame.Shared;

namespace PacmanGame.Server.Models;

public class Player
{
    private static int _nextId;
    public int Id { get; }
    public string Name { get; set; } // Name will be set from client request
    public NetPeer Peer { get; }
    public PlayerRole Role { get; set; }
    public Room? CurrentRoom { get; set; }
    public bool IsAdmin { get; set; }

    public Player(NetPeer peer)
    {
        Id = Interlocked.Increment(ref _nextId);
        Peer = peer;
        Role = PlayerRole.None;
        Name = string.Empty; // Initialize as empty, to be set by client request
    }
}
