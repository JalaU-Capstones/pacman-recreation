using System;
using System.Collections.Generic;
using System.Linq;
using PacmanGame.Models.Entities;
using PacmanGame.Models.Enums;
using PacmanGame.Helpers; // For Constants

namespace PacmanGame.Services.Pathfinding;

/// <summary>
/// Implements the A* pathfinding algorithm for ghosts to navigate the maze.
/// </summary>
public class AStarPathfinder
{
    /// <summary>
    /// Represents a node in the A* pathfinding grid.
    /// </summary>
    private struct Node
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
    /// <param name="startY">Starting Y coordinate.</param>
    /// <param name="startX">Starting X coordinate.</param>
    /// <param name="targetY">Target Y coordinate.</param>
    /// <param name="targetX">Target X coordinate.</param>
    /// <param name="map">The game map (tile types).</param>
    /// <param name="ghost">The ghost entity (for specific movement rules like avoiding U-turns).</param>
    /// <returns>The first direction in the optimal path, or Direction.None if no path is found.</returns>
    public Direction FindPath(int startY, int startX, int targetY, int targetX, TileType[,] map, Ghost ghost)
    {
        int mapHeight = map.GetLength(0);
        int mapWidth = map.GetLength(1);

        // Clamp target to map bounds
        targetY = Math.Clamp(targetY, 0, mapHeight - 1);
        targetX = Math.Clamp(targetX, 0, mapWidth - 1);

        // If start or target is a wall, or if start is same as target, handle edge cases
        if (map[startY, startX] == TileType.Wall || map[targetY, targetX] == TileType.Wall)
        {
            // If target is a wall, try to find the closest non-wall tile
            (targetY, targetX) = FindClosestNonWall(targetY, targetX, map);
            if (map[targetY, targetX] == TileType.Wall) return Direction.None; // Still a wall, no valid target
        }

        if (startY == targetY && startX == targetX) return Direction.None; // Already at target

        var openList = new List<Node>();
        var closedList = new HashSet<(int y, int x)>(); // Stores visited node positions
        var allNodes = new Dictionary<(int y, int x), Node>(); // Stores all created nodes for easy lookup

        Node startNode = new Node(startY, startX, 0, GetManhattanDistance(startY, startX, targetY, targetX), null, Direction.None);
        openList.Add(startNode);
        allNodes.Add((startY, startX), startNode);

        while (openList.Count > 0)
        {
            // Get node with lowest F cost
            Node currentNode = openList.OrderBy(node => node.FCost).First();
            openList.Remove(currentNode);
            closedList.Add((currentNode.Y, currentNode.X));

            if (currentNode.Y == targetY && currentNode.X == targetX)
            {
                return ReconstructPath(currentNode, startX, startY);
            }

            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
            {
                if (direction == Direction.None) continue;

                int neighborY = currentNode.Y;
                int neighborX = currentNode.X;

                // Calculate new coordinates based on direction
                switch (direction)
                {
                    case Direction.Up: neighborY--; break;
                    case Direction.Down: neighborY++; break;
                    case Direction.Left: neighborX--; break;
                    case Direction.Right: neighborX++; break;
                }

                // Handle tunnel wrapping
                if (neighborX < 0) neighborX = mapWidth - 1;
                else if (neighborX >= mapWidth) neighborX = 0;
                // Y-axis wrapping is generally not in Pac-Man, but include for robustness if map allows
                if (neighborY < 0) neighborY = mapHeight - 1;
                else if (neighborY >= mapHeight) neighborY = 0;


                // Check if neighbor is valid
                if (neighborY < 0 || neighborY >= mapHeight || neighborX < 0 || neighborX >= mapWidth) continue; // Out of bounds
                if (closedList.Contains((neighborY, neighborX))) continue; // Already evaluated

                // Ghost-specific movement rules
                // Ghosts cannot move through walls unless they are in Eaten state and passing through ghost house door
                bool isWall = map[neighborY, neighborX] == TileType.Wall;
                bool isGhostHouseDoor = map[neighborY, neighborX] == TileType.GhostDoor;

                if (isWall && !isGhostHouseDoor) continue; // Cannot move through solid walls
                if (isGhostHouseDoor && ghost.State != GhostState.Eaten && ghost.State != GhostState.Normal) continue; // Only eaten ghosts can pass through door freely

                // Prevent immediate U-turns unless necessary (e.g., dead end)
                if (direction == GetOppositeDirection(currentNode.DirectionFromParent) && openList.Count > 1)
                {
                    // Only avoid U-turn if there are other options in the open list
                    // This is a simplification; a more robust check would be if it's not a dead end
                    // For now, if it's the only path, take it.
                }

                double newGCost = currentNode.GCost + 1; // Cost to move to neighbor is 1

                Node neighborNode;
                bool inOpenList = allNodes.TryGetValue((neighborY, neighborX), out neighborNode);

                if (!inOpenList || newGCost < neighborNode.GCost)
                {
                    neighborNode = new Node(neighborY, neighborX, newGCost, GetManhattanDistance(neighborY, neighborX, targetY, targetX), currentNode, direction);
                    if (!inOpenList)
                    {
                        openList.Add(neighborNode);
                    }
                    // Update node in allNodes (structs are copied, so need to re-add or update dictionary)
                    allNodes[(neighborY, neighborX)] = neighborNode;
                }
            }
        }

        return Direction.None; // No path found
    }

    /// <summary>
    /// Finds the closest non-wall tile to a given target.
    /// </summary>
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

                // Handle wrapping for search
                if (nx < 0) nx = mapWidth - 1;
                else if (nx >= mapWidth) nx = 0;
                if (ny < 0) ny = mapHeight - 1;
                else if (ny >= mapHeight) ny = 0;

                if (visited.Add((ny, nx)))
                {
                    queue.Enqueue((ny, nx));
                }
            }
        }
        return (targetY, targetX); // Should not happen if map has non-wall tiles
    }


    /// <summary>
    /// Reconstructs the path from the target node back to the start node.
    /// </summary>
    private Direction ReconstructPath(Node targetNode, int startX, int startY)
    {
        Node? currentNode = targetNode;
        Node? firstStepNode = null;

        while (currentNode != null && (currentNode.Value.Y != startY || currentNode.Value.X != startX))
        {
            firstStepNode = currentNode;
            currentNode = currentNode.Value.Parent;
        }

        return firstStepNode?.DirectionFromParent ?? Direction.None;
    }

    /// <summary>
    /// Calculates the Manhattan distance heuristic.
    /// </summary>
    private double GetManhattanDistance(int y1, int x1, int y2, int x2)
    {
        return Math.Abs(y1 - y2) + Math.Abs(x1 - x2);
    }

    /// <summary>
    /// Gets the opposite direction.
    /// </summary>
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
