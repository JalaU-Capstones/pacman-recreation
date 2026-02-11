using System.Threading;
using LiteNetLib;
using PacmanGame.Shared;

namespace PacmanGame.Server.Models;

public class Player
{
    private static int _nextId;
    public int Id { get; }
    public NetPeer Peer { get; }
    public PlayerRole Role { get; set; }
    public Room? CurrentRoom { get; set; }
    public bool IsAdmin { get; set; }

    public Player(NetPeer peer)
    {
        // Assign a unique, sequential ID, starting from 1.
        Id = Interlocked.Increment(ref _nextId);
        Peer = peer;
        Role = PlayerRole.None;
    }
}
