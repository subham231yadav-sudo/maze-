using System.Collections.Generic;

namespace MazeGenerator
{
    /// <summary>
    /// Recursive backtracker (DFS) maze algorithm.
    /// </summary>
    public sealed class RecursiveBacktrackerAlgorithm : IMazeAlgorithm
    {
        public string Name => "Recursive Backtracker";

        public void Generate(MazeGrid grid, MazeRandom random, int startX, int startY)
        {
            if (!grid.IsActive(startX, startY))
            {
                return;
            }

            var visited = new bool[grid.Width, grid.Height];
            var stack = new Stack<(int x, int y)>();
            stack.Push((startX, startY));
            visited[startX, startY] = true;

            while (stack.Count > 0)
            {
                var (x, y) = stack.Peek();
                var neighbors = new List<Direction>(4);

                foreach (var dir in DirectionUtil.All)
                {
                    DirectionUtil.Offset(dir, out var dx, out var dy);
                    var nx = x + dx;
                    var ny = y + dy;
                    if (grid.IsActive(nx, ny) && !visited[nx, ny])
                    {
                        neighbors.Add(dir);
                    }
                }

                if (neighbors.Count == 0)
                {
                    stack.Pop();
                    continue;
                }

                random.Shuffle(neighbors);
                var chosen = neighbors[0];
                DirectionUtil.Offset(chosen, out var cdx, out var cdy);
                var cx = x + cdx;
                var cy = y + cdy;
                grid.Carve(x, y, chosen);
                visited[cx, cy] = true;
                stack.Push((cx, cy));
            }
        }
    }
}
