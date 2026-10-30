using System.Collections.Generic;

namespace MazeGenerator
{
    /// <summary>
    /// Randomized Prim's algorithm for maze generation.
    /// </summary>
    public sealed class PrimAlgorithm : IMazeAlgorithm
    {
        public string Name => "Prim";

        public void Generate(MazeGrid grid, MazeRandom random, int startX, int startY)
        {
            if (!grid.IsActive(startX, startY))
            {
                return;
            }

            var inMaze = new bool[grid.Width, grid.Height];
            var frontier = new List<(int x, int y, Direction from)>();

            inMaze[startX, startY] = true;
            AddFrontier(grid, startX, startY, inMaze, frontier);

            while (frontier.Count > 0)
            {
                var index = random.Next(frontier.Count);
                var (x, y, from) = frontier[index];
                frontier.RemoveAt(index);

                if (inMaze[x, y])
                {
                    continue;
                }

                DirectionUtil.Offset(DirectionUtil.Opposite(from), out var pdx, out var pdy);
                var px = x + pdx;
                var py = y + pdy;
                if (!grid.IsActive(px, py) || !inMaze[px, py])
                {
                    continue;
                }

                grid.Carve(px, py, from);
                inMaze[x, y] = true;
                AddFrontier(grid, x, y, inMaze, frontier);
            }
        }

        static void AddFrontier(
            MazeGrid grid,
            int x,
            int y,
            bool[,] inMaze,
            List<(int x, int y, Direction from)> frontier)
        {
            foreach (var dir in DirectionUtil.All)
            {
                DirectionUtil.Offset(dir, out var dx, out var dy);
                var nx = x + dx;
                var ny = y + dy;
                if (grid.IsActive(nx, ny) && !inMaze[nx, ny])
                {
                    frontier.Add((nx, ny, dir));
                }
            }
        }
    }
}
