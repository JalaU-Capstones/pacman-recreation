using PacmanGame.Shared;

namespace PacmanGame.Server.Models;

public class Room
{
    public int Id { get; }
    public string Name { get; }
    public string? Password { get; }
    public bool IsPublic => Password == null;
    public RoomState State { get; set; }

    private readonly List<Player> _players = new();
    public IReadOnlyCollection<Player> Players => _players.AsReadOnly();

    public Room(int id, string name, string? password)
    {
        Id = id;
        Name = name;
        Password = password;
        State = RoomState.Lobby;
    }

    public bool AddPlayer(Player player)
    {
        if (_players.Count >= 10)
        {
            return false;
        }

        _players.Add(player);
        return true;
    }

    public void RemovePlayer(Player player)
    {
        _players.Remove(player);
    }
}
