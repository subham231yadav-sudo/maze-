using System.Collections.Generic;
using UnityEngine;

namespace MazeGenerator
{
    /// <summary>
    /// Finds maze openings (cells with a missing wall toward the outside / inactive neighbor).
    /// </summary>
    public static class MazeOpeningFinder
    {
        public static List<Vector2Int> FindOpenings(MazeGrid grid)
        {
            var openings = new List<Vector2Int>();
            if (grid == null)
            {
                return openings;
            }

            foreach (var cell in grid.ActiveCells())
            {
                foreach (var dir in DirectionUtil.All)
                {
                    DirectionUtil.Offset(dir, out var dx, out var dy);
                    var nx = cell.X + dx;
                    var ny = cell.Y + dy;
                    var wall = DirectionUtil.ToWall(dir);
                    if (!cell.HasWall(wall) && !grid.IsActive(nx, ny))
                    {
                        openings.Add(new Vector2Int(cell.X, cell.Y));
                        break;
                    }
                }
            }

            return openings;
        }
    }
}
