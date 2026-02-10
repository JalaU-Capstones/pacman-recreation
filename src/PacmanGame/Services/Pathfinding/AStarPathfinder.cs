using System;
using System.Collections.Generic;
using System.Linq;
using PacmanGame.Models.Entities;
using PacmanGame.Models.Enums;
using PacmanGame.Helpers;

namespace PacmanGame.Services.Pathfinding;

/// <summary>
/// Implements the A* pathfinding algorithm for ghosts to navigate the maze.
/// </summary>
public class AStarPathfinder
{
    /// <summary>
    /// Represents a node in the A* pathfinding grid. Changed to a class to avoid cyclic layout error.
    /// </summary>
    private class Node
    {
        public int Y;
        public int X;
        public double GCost; // Cost from start to current node
        public double HCost; // Heuristic cost from current node to target
        public double FCost => GCost + HCost; // Total cost
        public Node? Parent; // Parent node to reconstruct path
        public Direction DirectionFromParent; // Direction taken to reach this node

        public Node(int y, int x, double gCost, double hCost, Node? parent, Direction dirFromParent)
        {
            Y = y;
            X = x;
            GCost = gCost;
            HCost = hCost;
            Parent = parent;
            DirectionFromParent = dirFromParent;
        }
    }

    /// <summary>
    /// Finds the optimal path from a start point to a target point on the map using A* algorithm.
    /// </summary>
    public Direction FindPath(int startY, int startX, int targetY, int targetX, TileType[,] map, Ghost ghost)
    {
        int mapHeight = map.GetLength(0);
        int mapWidth = map.GetLength(1);

        targetY = Math.Clamp(targetY, 0, mapHeight - 1);
        targetX = Math.Clamp(targetX, 0, mapWidth - 1);

        if (map[startY, startX] == TileType.Wall || map[targetY, targetX] == TileType.Wall)
        {
            (targetY, targetX) = FindClosestNonWall(targetY, targetX, map);
            if (map[targetY, targetX] == TileType.Wall) return Direction.None;
        }

        if (startY == targetY && startX == targetX) return Direction.None;

        var openList = new List<Node>();
        var closedList = new HashSet<(int y, int x)>();
        var allNodes = new Dictionary<(int y, int x), Node>();

        Node startNode = new Node(startY, startX, 0, GetManhattanDistance(startY, startX, targetY, targetX), null, Direction.None);
        openList.Add(startNode);
        allNodes.Add((startY, startX), startNode);

        while (openList.Count > 0)
        {
            Node currentNode = openList.OrderBy(node => node.FCost).First();
            openList.Remove(currentNode);
            closedList.Add((currentNode.Y, currentNode.X));

            if (currentNode.Y == targetY && currentNode.X == targetX)
            {
                return ReconstructPath(currentNode);
            }

            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
            {
                if (direction == Direction.None || direction == GetOppositeDirection(ghost.CurrentDirection)) continue;

                int neighborY = currentNode.Y;
                int neighborX = currentNode.X;

                switch (direction)
                {
                    case Direction.Up: neighborY--; break;
                    case Direction.Down: neighborY++; break;
                    case Direction.Left: neighborX--; break;
                    case Direction.Right: neighborX++; break;
                }

                if (neighborX < 0) neighborX = mapWidth - 1;
                else if (neighborX >= mapWidth) neighborX = 0;
                if (neighborY < 0) neighborY = mapHeight - 1;
                else if (neighborY >= mapHeight) neighborY = 0;

                if (closedList.Contains((neighborY, neighborX))) continue;

                bool isWall = map[neighborY, neighborX] == TileType.Wall;
                bool isGhostHouseDoor = map[neighborY, neighborX] == TileType.GhostDoor;

                if (isWall && !isGhostHouseDoor) continue;
                if (isGhostHouseDoor && ghost.State != GhostState.Eaten) continue;

                double newGCost = currentNode.GCost + 1;

                if (allNodes.TryGetValue((neighborY, neighborX), out Node? neighborNode))
                {
                    if (newGCost < neighborNode.GCost)
                    {
                        neighborNode.GCost = newGCost;
                        neighborNode.Parent = currentNode;
                        neighborNode.DirectionFromParent = direction;
                    }
                }
                else
                {
                    neighborNode = new Node(neighborY, neighborX, newGCost, GetManhattanDistance(neighborY, neighborX, targetY, targetX), currentNode, direction);
                    openList.Add(neighborNode);
                    allNodes.Add((neighborY, neighborX), neighborNode);
                }
            }
        }

        return Direction.None;
    }

    private (int y, int x) FindClosestNonWall(int targetY, int targetX, TileType[,] map)
    {
        int mapHeight = map.GetLength(0);
        int mapWidth = map.GetLength(1);

        if (map[targetY, targetX] != TileType.Wall) return (targetY, targetX);

        Queue<(int y, int x)> queue = new Queue<(int y, int x)>();
        HashSet<(int y, int x)> visited = new HashSet<(int y, int x)>();

        queue.Enqueue((targetY, targetX));
        visited.Add((targetY, targetX));

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (map[current.y, current.x] != TileType.Wall) return current;

            foreach (Direction dir in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
            {
                int ny = current.y;
                int nx = current.x;

                switch (dir)
                {
                    case Direction.Up: ny--; break;
                    case Direction.Down: ny++; break;
                    case Direction.Left: nx--; break;
                    case Direction.Right: nx++; break;
                }

                if (nx < 0 || nx >= mapWidth || ny < 0 || ny >= mapHeight) continue;

                if (visited.Add((ny, nx)))
                {
                    queue.Enqueue((ny, nx));
                }
            }
        }
        return (targetY, targetX);
    }

    private Direction ReconstructPath(Node targetNode)
    {
        Node? currentNode = targetNode;
        while (currentNode?.Parent?.Parent != null)
        {
            currentNode = currentNode.Parent;
        }
        return currentNode?.DirectionFromParent ?? Direction.None;
    }

    private double GetManhattanDistance(int y1, int x1, int y2, int x2)
    {
        return Math.Abs(y1 - y2) + Math.Abs(x1 - x2);
    }

    private Direction GetOppositeDirection(Direction dir)
    {
        return dir switch
        {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            _ => Direction.None
        };
    }
}
