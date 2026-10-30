using System;
using System.Collections.Generic;
using UnityEngine;

namespace MazeGenerator
{
    /// <summary>
    /// Grid of maze cells with carving helpers. Generation is deterministic when seeded.
    /// </summary>
    public sealed class MazeGrid
    {
        public int Width { get; }
        public int Height { get; }

        readonly MazeCell[,] _cells;

        public MazeGrid(int width, int height)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));

            Width = width;
            Height = height;
            _cells = new MazeCell[width, height];

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    _cells[x, y] = new MazeCell(x, y);
                }
            }
        }

        public MazeCell this[int x, int y] => _cells[x, y];

        public MazeCell GetCell(int x, int y) => _cells[x, y];

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public bool IsActive(int x, int y) => InBounds(x, y) && _cells[x, y].IsActive;

        public void ResetWalls()
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    var cell = _cells[x, y];
                    var active = cell.IsActive;
                    cell.Reset();
                    cell.IsActive = active;
                }
            }
        }

        public IEnumerable<MazeCell> ActiveCells()
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    if (_cells[x, y].IsActive)
                    {
                        yield return _cells[x, y];
                    }
                }
            }
        }

        public bool TryGetNeighbor(int x, int y, Direction direction, out MazeCell neighbor)
        {
            DirectionUtil.Offset(direction, out var dx, out var dy);
            var nx = x + dx;
            var ny = y + dy;
            if (!IsActive(nx, ny))
            {
                neighbor = null;
                return false;
            }

            neighbor = _cells[nx, ny];
            return true;
        }

        public void Carve(int x, int y, Direction direction)
        {
            if (!IsActive(x, y))
            {
                return;
            }

            if (!TryGetNeighbor(x, y, direction, out var neighbor))
            {
                return;
            }

            var wall = DirectionUtil.ToWall(direction);
            var opposite = DirectionUtil.ToWall(DirectionUtil.Opposite(direction));
            _cells[x, y].RemoveWall(wall);
            neighbor.RemoveWall(opposite);
        }

        /// <summary>
        /// Restores the shared wall between two adjacent active cells.
        /// </summary>
        public void Seal(int x, int y, Direction direction)
        {
            if (!IsActive(x, y))
            {
                return;
            }

            if (!TryGetNeighbor(x, y, direction, out var neighbor))
            {
                return;
            }

            var wall = DirectionUtil.ToWall(direction);
            var opposite = DirectionUtil.ToWall(DirectionUtil.Opposite(direction));
            _cells[x, y].AddWall(wall);
            neighbor.AddWall(opposite);
        }

        public void OpenBoundaryWall(int x, int y, Direction direction)
        {
            if (!IsActive(x, y))
            {
                return;
            }

            _cells[x, y].RemoveWall(DirectionUtil.ToWall(direction));
        }

        public List<Direction> GetActiveNeighborDirections(int x, int y)
        {
            var list = new List<Direction>(4);
            foreach (var dir in DirectionUtil.All)
            {
                if (TryGetNeighbor(x, y, dir, out _))
                {
                    list.Add(dir);
                }
            }

            return list;
        }

        public Vector2Int? FindFirstActiveCell()
        {
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    if (_cells[x, y].IsActive)
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }

            return null;
        }

        public bool HasPassageBetween(MazeCell a, MazeCell b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            if (Mathf.Abs(dx) + Mathf.Abs(dy) != 1)
            {
                return false;
            }

            if (dx == 1) return !a.HasWall(WallFlags.East);
            if (dx == -1) return !a.HasWall(WallFlags.West);
            if (dy == 1) return !a.HasWall(WallFlags.North);
            if (dy == -1) return !a.HasWall(WallFlags.South);
            return false;
        }
    }
}
