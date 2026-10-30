using UnityEngine;

namespace MazeGenerator
{
    /// <summary>
    /// Applies rectangle / circle / custom texture masks onto a grid.
    /// </summary>
    public static class ShapeMaskBuilder
    {
        public static void Apply(MazeGrid grid, MazeGenerationParams parameters)
        {
            switch (parameters.Shape)
            {
                case MazeShape.Circle:
                    ApplyCircle(grid);
                    break;
                case MazeShape.CustomMask:
                    ApplyCustomMask(grid, parameters.CustomMaskTexture);
                    break;
                default:
                    ApplyRectangle(grid);
                    break;
            }
        }

        public static void ApplyRectangle(MazeGrid grid)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                for (var y = 0; y < grid.Height; y++)
                {
                    grid[x, y].IsActive = true;
                }
            }
        }

        public static void ApplyCircle(MazeGrid grid)
        {
            var cx = (grid.Width - 1) * 0.5f;
            var cy = (grid.Height - 1) * 0.5f;
            var radius = Mathf.Min(grid.Width, grid.Height) * 0.5f;

            for (var x = 0; x < grid.Width; x++)
            {
                for (var y = 0; y < grid.Height; y++)
                {
                    var dx = x - cx;
                    var dy = y - cy;
                    grid[x, y].IsActive = dx * dx + dy * dy <= radius * radius;
                }
            }
        }

        public static void ApplyCustomMask(MazeGrid grid, Texture2D texture)
        {
            if (texture == null)
            {
                ApplyRectangle(grid);
                return;
            }

            for (var x = 0; x < grid.Width; x++)
            {
                for (var y = 0; y < grid.Height; y++)
                {
                    var u = (x + 0.5f) / grid.Width;
                    var v = (y + 0.5f) / grid.Height;
                    var pixel = texture.GetPixelBilinear(u, v);
                    grid[x, y].IsActive = pixel.grayscale > 0.5f;
                }
            }
        }
    }
}
