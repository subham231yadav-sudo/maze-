using System.Collections.Generic;
using UnityEngine;

namespace MazeGenerator
{
    /// <summary>
    /// Post-generation steps: openings, braiding, rooms.
    /// </summary>
    public static class MazePostProcessor
    {
        public static void Apply(MazeGrid grid, MazeGenerationParams parameters, MazeRandom random)
        {
            ApplyRooms(grid, parameters, random);
            ApplyBraiding(grid, parameters, random);
            ApplyOpenings(grid, parameters, random);
        }

        public static void ApplyBraiding(MazeGrid grid, MazeGenerationParams parameters, MazeRandom random)
        {
            if (parameters.BraidFactor <= 0f)
            {
                return;
            }

            var candidates = new List<(int x, int y, Direction dir)>();
            foreach (var cell in grid.ActiveCells())
            {
                if (cell.IsRoom)
                {
                    continue;
                }

                foreach (var dir in DirectionUtil.All)
                {
                    if (!cell.HasWall(DirectionUtil.ToWall(dir)))
                    {
                        continue;
                    }

                    if (!grid.TryGetNeighbor(cell.X, cell.Y, dir, out var neighbor))
                    {
                        continue;
                    }

                    // Do not braid into/out of rooms — keep inherited room↔corridor exits.
                    if (neighbor.IsRoom)
                    {
                        continue;
                    }

                    // Only consider one direction per edge to avoid double-counting
                    if (dir == Direction.East || dir == Direction.North)
                    {
                        candidates.Add((cell.X, cell.Y, dir));
                    }
                }
            }

            var removeCount = Mathf.RoundToInt(candidates.Count * parameters.BraidFactor);
            random.Shuffle(candidates);
            for (var i = 0; i < removeCount && i < candidates.Count; i++)
            {
                var (x, y, dir) = candidates[i];
                grid.Carve(x, y, dir);
            }
        }

        public static void ApplyOpenings(MazeGrid grid, MazeGenerationParams parameters, MazeRandom random)
        {
            if (parameters.OpeningCount <= 0)
            {
                return;
            }

            var edgeCandidates = new List<(int x, int y, Direction dir)>();
            foreach (var cell in grid.ActiveCells())
            {
                foreach (var dir in DirectionUtil.All)
                {
                    DirectionUtil.Offset(dir, out var dx, out var dy);
                    var nx = cell.X + dx;
                    var ny = cell.Y + dy;
                    // Opening = remove outer wall when neighbor is missing / inactive
                    if (!grid.IsActive(nx, ny) && cell.HasWall(DirectionUtil.ToWall(dir)))
                    {
                        edgeCandidates.Add((cell.X, cell.Y, dir));
                    }
                }
            }

            if (edgeCandidates.Count == 0)
            {
                return;
            }

            random.Shuffle(edgeCandidates);
            var count = Mathf.Min(parameters.OpeningCount, edgeCandidates.Count);
            for (var i = 0; i < count; i++)
            {
                var (x, y, dir) = edgeCandidates[i];
                grid.OpenBoundaryWall(x, y, dir);
            }
        }

        public static void ApplyRooms(MazeGrid grid, MazeGenerationParams parameters, MazeRandom random)
        {
            if (parameters.RoomCount <= 0)
            {
                return;
            }

            for (var r = 0; r < parameters.RoomCount; r++)
            {
                var sizeX = random.Next(parameters.RoomMinSize, parameters.RoomMaxSize + 1);
                var sizeY = random.Next(parameters.RoomMinSize, parameters.RoomMaxSize + 1);
                if (sizeX > grid.Width || sizeY > grid.Height)
                {
                    continue;
                }

                var placed = false;
                for (var attempt = 0; attempt < 40 && !placed; attempt++)
                {
                    var ox = random.Next(0, grid.Width - sizeX + 1);
                    var oy = random.Next(0, grid.Height - sizeY + 1);
                    if (!CanPlaceRoom(grid, ox, oy, sizeX, sizeY))
                    {
                        continue;
                    }

                    CarveRoom(grid, ox, oy, sizeX, sizeY);
                    placed = true;
                }
            }
        }

        static bool CanPlaceRoom(MazeGrid grid, int ox, int oy, int sizeX, int sizeY)
        {
            for (var x = ox; x < ox + sizeX; x++)
            {
                for (var y = oy; y < oy + sizeY; y++)
                {
                    if (!grid.IsActive(x, y) || grid[x, y].IsRoom)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Opens all internal walls of the room and keeps perimeter passages inherited
        /// from the maze algorithm, so connectivity (and solvability) is preserved.
        /// </summary>
        static void CarveRoom(MazeGrid grid, int ox, int oy, int sizeX, int sizeY)
        {
            for (var x = ox; x < ox + sizeX; x++)
            {
                for (var y = oy; y < oy + sizeY; y++)
                {
                    var cell = grid[x, y];
                    cell.IsRoom = true;
                    if (x + 1 < ox + sizeX)
                    {
                        grid.Carve(x, y, Direction.East);
                    }

                    if (y + 1 < oy + sizeY)
                    {
                        grid.Carve(x, y, Direction.North);
                    }
                }
            }
        }
    }
}
