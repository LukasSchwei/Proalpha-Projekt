using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using ClassLibrary.Coordinates;
using ClassLibrary.Responses;

namespace ClassLibrary.Algorithm.AStar;

public static class AStarPathfinding
{
    private const int DIAGONAL_COST = 14;
    private const int STRAIGHT_COST = 10;
    private static List<Node> openList = new List<Node>();
    private static List<Node> closedList = new List<Node>();
    public static List<(int x, int y)> FindPath(Dictionary<(int x, int y), AbsoluteObject> objects, string? type, CurrentPosition currentPosition, int globalMapMinX, int globalMapMinY, int globalMapMaxX, int globalMapMaxY)
    {
        // Initialize/reset the lists
        openList = new List<Node>();
        closedList = new List<Node>();

        if (FindEndNode(objects, type, new Node() { x = currentPosition.AbsoluteX, y = currentPosition.AbsoluteY }, globalMapMinX, globalMapMinY, globalMapMaxX, globalMapMaxY) == null)
        {
            return null;
        }
        Node endNode = FindEndNode(objects, type, new Node() { x = currentPosition.AbsoluteX, y = currentPosition.AbsoluteY }, globalMapMinX, globalMapMinY, globalMapMaxX, globalMapMaxY);
        Node startNode = new Node()
        {
            x = currentPosition.AbsoluteX,
            y = currentPosition.AbsoluteY,
            g = 0,
            h = CalculateDistanceCost(new Node() { x = currentPosition.AbsoluteX, y = currentPosition.AbsoluteY }, endNode),
            parent = null
        };
        startNode.CalculateFCost();
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            Node currentNode = GetLowestFCostNode(openList);
            if (currentNode.x == endNode.x && currentNode.y == endNode.y)
            {
                return ReconstructPath(startNode, currentNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            foreach (Node neighbour in GetNeighbours(currentNode, endNode, globalMapMinX, globalMapMinY, globalMapMaxX, globalMapMaxY))
            {
                if (closedList.Any(n => n.x == neighbour.x && n.y == neighbour.y)) continue;
                if (!IsWalkable(objects, neighbour, globalMapMinX, globalMapMinY, globalMapMaxX, globalMapMaxY, false))
                {
                    closedList.Add(neighbour);
                    continue;
                }

                int tentativeGCost = currentNode.g + CalculateDistanceCost(currentNode, neighbour);
                var existingNode = openList.FirstOrDefault(n => n.x == neighbour.x && n.y == neighbour.y);

                if (existingNode == null || tentativeGCost < existingNode.g)
                {
                    if (existingNode == null)
                    {
                        neighbour.g = tentativeGCost;
                        neighbour.h = CalculateDistanceCost(neighbour, endNode);
                        neighbour.parent = currentNode;
                        neighbour.CalculateFCost();
                        openList.Add(neighbour);
                    }
                    else
                    {
                        existingNode.g = tentativeGCost;
                        existingNode.h = CalculateDistanceCost(existingNode, endNode);
                        existingNode.parent = currentNode;
                        existingNode.CalculateFCost();
                    }
                }
            }
        }
        return null;
    }

    public static List<(int x, int y)> FindPathToUnknown(Dictionary<(int x, int y), AbsoluteObject> objects, CurrentPosition currentPosition, int globalMapMinX, int globalMapMinY, int globalMapMaxX, int globalMapMaxY)
    {
        // Initialize/reset the lists
        openList = new List<Node>();
        closedList = new List<Node>();

        if (FindEndNode(objects, null, new Node() { x = currentPosition.AbsoluteX, y = currentPosition.AbsoluteY }, globalMapMinX, globalMapMinY, globalMapMaxX, globalMapMaxY) == null)
        {
            return null;
        }

        Node endNode = FindEndNode(objects, null, new Node() { x = currentPosition.AbsoluteX, y = currentPosition.AbsoluteY }, globalMapMinX, globalMapMinY, globalMapMaxX, globalMapMaxY);
        Node startNode = new Node()
        {
            x = currentPosition.AbsoluteX,
            y = currentPosition.AbsoluteY,
            g = 0,
            h = CalculateDistanceCost(new Node() { x = currentPosition.AbsoluteX, y = currentPosition.AbsoluteY }, endNode),
            parent = null
        };
        startNode.CalculateFCost();
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            Node currentNode = GetLowestFCostNode(openList);
            if (currentNode.x == endNode.x && currentNode.y == endNode.y)
            {
                return ReconstructPath(startNode, currentNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            foreach (Node neighbour in GetNeighbours(currentNode, endNode, globalMapMinX, globalMapMinY, globalMapMaxX, globalMapMaxY))
            {
                if (neighbour.x == endNode.x && neighbour.y == endNode.y)
                {
                    var actualEndNode = new Node() { x = endNode.x, y = endNode.y, parent = currentNode };
                    return ReconstructPath(startNode, actualEndNode);
                }

                if (closedList.Any(n => n.x == neighbour.x && n.y == neighbour.y)) continue;

                if (!IsWalkable(objects, neighbour, globalMapMinX, globalMapMinY, globalMapMaxX, globalMapMaxY, true))
                {
                    closedList.Add(neighbour);
                    continue;
                }

                int tentativeGCost = currentNode.g + CalculateDistanceCost(currentNode, neighbour);
                var existingNode = openList.FirstOrDefault(n => n.x == neighbour.x && n.y == neighbour.y);

                if (existingNode == null || tentativeGCost < existingNode.g)
                {
                    if (existingNode == null)
                    {
                        neighbour.g = tentativeGCost;
                        neighbour.h = CalculateDistanceCost(neighbour, endNode);
                        neighbour.parent = currentNode;
                        neighbour.CalculateFCost();
                        openList.Add(neighbour);
                    }
                    else
                    {
                        existingNode.g = tentativeGCost;
                        existingNode.h = CalculateDistanceCost(existingNode, endNode);
                        existingNode.parent = currentNode;
                        existingNode.CalculateFCost();
                    }
                }
            }
        }
        return null;
    }

    private static List<Node> GetNeighbours(Node node, Node endNode, int globalMapMinX, int globalMapMinY, int globalMapMaxX, int globalMapMaxY)
    {
        List<Node> neighbours = new List<Node>();
        if (node.x - 1 >= globalMapMinX)
        {
            neighbours.Add(new Node()
            {
                x = node.x - 1,
                y = node.y,
                parent = node,
                g = CalculateDistanceCost(node, new Node() { x = node.x - 1, y = node.y }),
                h = CalculateDistanceCost(new Node() { x = node.x - 1, y = node.y }, endNode),
                f = node.g + node.h
            });
            if (node.y - 1 >= globalMapMinY)
            {
                neighbours.Add(new Node()
                {
                    x = node.x - 1,
                    y = node.y - 1,
                    parent = node,
                    g = CalculateDistanceCost(node, new Node() { x = node.x - 1, y = node.y - 1 }),
                    h = CalculateDistanceCost(new Node() { x = node.x - 1, y = node.y - 1 }, endNode),
                    f = node.g + node.h
                });
            }
            if (node.y + 1 <= globalMapMaxY)
            {
                neighbours.Add(new Node()
                {
                    x = node.x - 1,
                    y = node.y + 1,
                    parent = node,
                    g = CalculateDistanceCost(node, new Node() { x = node.x - 1, y = node.y + 1 }),
                    h = CalculateDistanceCost(new Node() { x = node.x - 1, y = node.y + 1 }, endNode),
                    f = node.g + node.h
                });
            }
        }
        if (node.x + 1 <= globalMapMaxX)
        {
            neighbours.Add(new Node()
            {
                x = node.x + 1,
                y = node.y,
                parent = node,
                g = CalculateDistanceCost(node, new Node() { x = node.x + 1, y = node.y }),
                h = CalculateDistanceCost(new Node() { x = node.x + 1, y = node.y }, endNode),
                f = node.g + node.h
            });
            if (node.y - 1 >= globalMapMinY)
            {
                neighbours.Add(new Node()
                {
                    x = node.x + 1,
                    y = node.y - 1,
                    parent = node,
                    g = CalculateDistanceCost(node, new Node() { x = node.x + 1, y = node.y - 1 }),
                    h = CalculateDistanceCost(new Node() { x = node.x + 1, y = node.y - 1 }, endNode),
                    f = node.g + node.h
                });
            }
            if (node.y + 1 <= globalMapMaxY)
            {
                neighbours.Add(new Node()
                {
                    x = node.x + 1,
                    y = node.y + 1,
                    parent = node,
                    g = CalculateDistanceCost(node, new Node() { x = node.x + 1, y = node.y + 1 }),
                    h = CalculateDistanceCost(new Node() { x = node.x + 1, y = node.y + 1 }, endNode),
                    f = node.g + node.h
                });
            }
        }
        if (node.y - 1 >= globalMapMinY)
        {
            neighbours.Add(new Node()
            {
                x = node.x,
                y = node.y - 1,
                parent = node,
                g = CalculateDistanceCost(node, new Node() { x = node.x, y = node.y - 1 }),
                h = CalculateDistanceCost(new Node() { x = node.x, y = node.y - 1 }, endNode),
                f = node.g + node.h
            });
        }
        if (node.y + 1 <= globalMapMaxY)
        {
            neighbours.Add(new Node()
            {
                x = node.x,
                y = node.y + 1,
                parent = node,
                g = CalculateDistanceCost(node, new Node() { x = node.x, y = node.y + 1 }),
                h = CalculateDistanceCost(new Node() { x = node.x, y = node.y + 1 }, endNode),
                f = node.g + node.h
            });
        }
        return neighbours;
    }

    private static List<(int x, int y)>? ReconstructPath(Node start, Node end)
    {
        if (start == null || end == null)
            return null;

        List<(int x, int y)> moves = new();
        Node current = end;

        while (current != null && current != start)
        {
            if (current.parent == null)
            {
                // Path is broken
                return null;
            }

            moves.Add((current.x - current.parent.x, current.y - current.parent.y));
            current = current.parent;
        }

        if (current == null)
        {
            // Reached the end without finding the start node
            return null;
        }

        moves.Reverse();
        return moves.Count == 0 ? null : moves;
    }

    private static bool IsWalkable(Dictionary<(int x, int y), AbsoluteObject> objects, Node node, int globalMapMinX, int globalMapMinY, int globalMapMaxX, int globalMapMaxY, bool unknown)
    {
        if (!objects.ContainsKey((node.x, node.y)))
        {
            if (unknown && node.x >= globalMapMinX && node.x <= globalMapMaxX && node.y >= globalMapMinY && node.y <= globalMapMaxY)
            {
                return true;
            }
            return false;
        }
        if (node.x < globalMapMinX || node.x > globalMapMaxX || node.y < globalMapMinY || node.y > globalMapMaxY)
        {
            return false;
        }
        if (objects[(node.x, node.y)].Type == "SOLID_ROCK" || objects[(node.x, node.y)].Type == "SOLID_WALL")
        {
            return false;
        }
        return true;
    }

    private static int CalculateDistanceCost(Node start, Node end)
    {
        int dx = Math.Abs(start.x) - Math.Abs(end.x);
        int dy = Math.Abs(start.y) - Math.Abs(end.y);
        int remaining = Math.Abs(dx) - Math.Abs(dy);
        return Math.Abs(Math.Abs(dx) - Math.Abs(remaining)) * DIAGONAL_COST + Math.Abs(remaining) * STRAIGHT_COST;
    }

    private static Node GetLowestFCostNode(List<Node> nodes)
    {
        Node lowestFCostNode = nodes[0];
        for (int i = 1; i < nodes.Count; i++)
        {
            if (nodes[i].f < lowestFCostNode.f)
            {
                lowestFCostNode = nodes[i];
            }
        }
        return lowestFCostNode;
    }

    private static Node FindEndNode(Dictionary<(int x, int y), AbsoluteObject> objects, string? type, Node startNode, int globalMapMinX, int globalMapMinY, int globalMapMaxX, int globalMapMaxY)
    {
        if (type == null)
        {
            List<Node> unknownNodes = new List<Node>();
            for (int i = globalMapMinX; i < globalMapMaxX; i++)
            {
                for (int j = globalMapMinY; j < globalMapMaxY; j++)
                {
                    if (!objects.ContainsKey((i, j)))
                    {
                        var node = new Node()
                        {
                            x = i,
                            y = j,
                            parent = null
                        };
                        node.f = Math.Abs(i - startNode.x) + Math.Abs(j - startNode.y);
                        unknownNodes.Add(node);
                    }
                }
            }
            if (unknownNodes.Count == 0)
            {
                return null;
            }
            return GetLowestFCostNode(unknownNodes);
        }

        List<Node> targetNodes = new List<Node>();
        foreach (AbsoluteObject obj in objects.Values)
        {
            if (obj.Type == type)
            {
                var node = new Node()
                {
                    x = obj.AbsoluteX,
                    y = obj.AbsoluteY,
                    parent = null
                };
                node.f = Math.Abs(obj.AbsoluteX - startNode.x) + Math.Abs(obj.AbsoluteY - startNode.y);
                targetNodes.Add(node);
            }
        }
        if (targetNodes.Count == 0)
        {
            return null;
        }
        targetNodes.Sort((a, b) => a.f.CompareTo(b.f));
        foreach (Node node in targetNodes)
        {
            node.f = 0;
        }
        return targetNodes[0];
    }
}

public class Node
{
    public int x;
    public int y;
    public int g;
    public int h;
    public int f;
    public Node parent;

    public void CalculateFCost()
    {
        f = g + h;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        Node other = (Node)obj;
        return x == other.x && y == other.y;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (x.GetHashCode() * 397) ^ y.GetHashCode();
        }
    }
}
