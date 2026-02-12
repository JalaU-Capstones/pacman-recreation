using PacmanGame.Server.Models;
using System.Collections.Concurrent;
using System.Threading;
using PacmanGame.Shared;
using System.Collections.Generic;
using System.Linq;

namespace PacmanGame.Server;

public class RoomManager
{
    private readonly ConcurrentDictionary<string, Room> _rooms = new();
    private int _nextRoomId = 0;

    public Room? CreateRoom(string name, string? password, RoomVisibility visibility)
    {
        var roomId = Interlocked.Increment(ref _nextRoomId);
        var room = new Room(roomId, name, password, visibility);
        if (_rooms.TryAdd(name, room))
        {
            return room;
        }
        return null;
    }

    public Room? GetRoom(string name)
    {
        _rooms.TryGetValue(name, out var room);
        return room;
    }

    public IEnumerable<Room> GetPublicRooms()
    {
        return _rooms.Values.Where(r => r.Visibility == RoomVisibility.Public);
    }

    public Room? GetRoomForPlayer(Player player)
    {
        return _rooms.Values.FirstOrDefault(r => r.Players.Any(p => p.Id == player.Id));
    }
}
