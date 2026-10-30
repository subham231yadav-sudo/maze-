using System.Collections.Generic;
using UnityEngine;

namespace MazeGenerator
{
    /// <summary>
    /// Result of one A* run: reconstructed path between two cells.
    /// </summary>
    public sealed class MazePathResult
    {
        public Vector2Int Start;
        public Vector2Int Goal;
        public readonly List<Vector2Int> Path = new List<Vector2Int>();
        public bool Success => Path.Count > 0;
    }

    /// <summary>
    /// A* pathfinding on a carved maze grid (4-connected, no wall between cells).
    /// </summary>
    public static class MazeAStarSolver
    {
        public static MazePathResult Solve(MazeGrid grid, Vector2Int start, Vector2Int goal)
        {
            var result = new MazePathResult { Start = start, Goal = goal };
            if (grid == null || !grid.IsActive(start.x, start.y) || !grid.IsActive(goal.x, goal.y))
            {
                return result;
            }

            if (start == goal)
            {
                result.Path.Add(start);
                return result;
            }

            var open = new List<Vector2Int> { start };
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float> { [start] = 0f };
            var fScore = new Dictionary<Vector2Int, float> { [start] = Heuristic(start, goal) };
            var closed = new HashSet<Vector2Int>();

            while (open.Count > 0)
            {
                var current = PopLowestF(open, fScore);

                if (current == goal)
                {
                    Reconstruct(cameFrom, current, result.Path);
                    return result;
                }

                closed.Add(current);
                foreach (var neighbor in GetPassableNeighbors(grid, current))
                {
                    if (closed.Contains(neighbor))
                    {
                        continue;
                    }

                    var tentative = gScore[current] + 1f;
                    if (!gScore.TryGetValue(neighbor, out var existing) || tentative < existing)
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentative;
                        fScore[neighbor] = tentative + Heuristic(neighbor, goal);
                        if (!open.Contains(neighbor))
                        {
                            open.Add(neighbor);
                        }
                    }
                }
            }

            return result;
        }

        static float Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        static Vector2Int PopLowestF(List<Vector2Int> open, Dictionary<Vector2Int, float> fScore)
        {
            var bestIndex = 0;
            var bestF = float.PositiveInfinity;
            for (var i = 0; i < open.Count; i++)
            {
                var f = fScore.TryGetValue(open[i], out var value) ? value : float.PositiveInfinity;
                if (f < bestF)
                {
                    bestF = f;
                    bestIndex = i;
                }
            }

            var best = open[bestIndex];
            open.RemoveAt(bestIndex);
            return best;
        }

        static IEnumerable<Vector2Int> GetPassableNeighbors(MazeGrid grid, Vector2Int cell)
        {
            foreach (var dir in DirectionUtil.All)
            {
                if (!grid.TryGetNeighbor(cell.x, cell.y, dir, out var neighbor))
                {
                    continue;
                }

                if (grid[cell.x, cell.y].HasWall(DirectionUtil.ToWall(dir)))
                {
                    continue;
                }

                yield return new Vector2Int(neighbor.X, neighbor.Y);
            }
        }

        static void Reconstruct(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current, List<Vector2Int> path)
        {
            path.Clear();
            path.Add(current);
            while (cameFrom.TryGetValue(current, out var prev))
            {
                current = prev;
                path.Add(current);
            }

            path.Reverse();
        }
    }
}
