using PacmanGame.Server.Models;
using System.Collections.Concurrent;
using System.Threading;

namespace PacmanGame.Server;

public class RoomManager
{
    private readonly ConcurrentDictionary<string, Room> _rooms = new();
    private int _nextRoomId = 0;

    public Room? CreateRoom(string name, string? password)
    {
        var roomId = Interlocked.Increment(ref _nextRoomId);
        var room = new Room(roomId, name, password);
        Console.WriteLine($"[DEBUG] Current rooms: {string.Join(", ", _rooms.Keys)}");
        if (_rooms.TryAdd(name, room))
        {
            Console.WriteLine($"[INFO] Room '{name}' created successfully with ID {roomId}.");
            return room;
        }
        Console.WriteLine($"[WARN] Failed to create room '{name}'. Room may already exist. Current rooms: {string.Join(", ", _rooms.Keys)}");
        return null;
    }

    public Room? GetRoom(string name)
    {
        _rooms.TryGetValue(name, out var room);
        return room;
    }

    public IEnumerable<Room> GetPublicRooms()
    {
        return _rooms.Values.Where(r => r.IsPublic);
    }

    public Room? GetRoomForPlayer(Player player)
    {
        return _rooms.Values.FirstOrDefault(r => r.Players.Any(p => p.Id == player.Id));
    }
}
