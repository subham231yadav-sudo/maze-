using UnityEngine;

namespace MazeGenerator
{
    /// <summary>
    /// Orchestrates mask → algorithm → post-process into a finished <see cref="MazeGrid"/>.
    /// </summary>
    public static class MazeGeneratorService
    {
        public static MazeGrid Generate(MazeGenerationParams parameters)
        {
            var error = parameters.Validate();
            if (error != null)
            {
                throw new System.InvalidOperationException(error);
            }

            var grid = new MazeGrid(parameters.Width, parameters.Height);
            ShapeMaskBuilder.Apply(grid, parameters);
            grid.ResetWalls();

            var random = new MazeRandom(parameters.Seed);
            var start = ResolveStartCell(grid, random);
            if (start == null)
            {
                throw new System.InvalidOperationException("No active cells available for maze generation.");
            }

            var algorithm = MazeAlgorithmFactory.Create(parameters.Algorithm);
            algorithm.Generate(grid, random, start.Value.x, start.Value.y);
            MazePostProcessor.Apply(grid, parameters, random);
            return grid;
        }

        /// <summary>
        /// Prefer (0,0). If inactive (e.g. circle/mask), pick a random active cell.
        /// </summary>
        static Vector2Int? ResolveStartCell(MazeGrid grid, MazeRandom random)
        {
            if (grid.IsActive(0, 0))
            {
                return Vector2Int.zero;
            }

            var actives = new System.Collections.Generic.List<Vector2Int>();
            foreach (var cell in grid.ActiveCells())
            {
                actives.Add(new Vector2Int(cell.X, cell.Y));
            }

            if (actives.Count == 0)
            {
                return null;
            }

            return actives[random.Next(actives.Count)];
        }
    }
}
