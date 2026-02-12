using PacmanGame.Shared;
using System.Collections.Generic;
using System.Linq;

namespace PacmanGame.Server.Models;

public class Room
{
    public int Id { get; }
    public string Name { get; }
    public string? Password { get; }
    public RoomVisibility Visibility { get; }
    public RoomState State { get; set; }
    public GameSimulation? Game { get; set; }

    private readonly List<Player> _players = new();
    public IReadOnlyCollection<Player> Players => _players.AsReadOnly();

    public Room(int id, string name, string? password, RoomVisibility visibility)
    {
        Id = id;
        Name = name;
        Password = password;
        Visibility = visibility;
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

    public List<PlayerState> GetPlayerStates()
    {
        return _players.Select(p => new PlayerState
        {
            PlayerId = p.Id,
            Name = p.Name,
            Role = p.Role,
            IsAdmin = p.IsAdmin
        }).ToList();
    }
}
