namespace MazeGenerator
{
    /// <summary>
    /// Single maze cell: activity, walls, and room membership.
    /// </summary>
    public sealed class MazeCell
    {
        public int X { get; }
        public int Y { get; }

        /// <summary>False when excluded by the shape mask.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Walls still present on this cell.</summary>
        public WallFlags Walls { get; set; } = WallFlags.All;

        /// <summary>True when this cell belongs to a carved empty room.</summary>
        public bool IsRoom { get; set; }

        public MazeCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool HasWall(WallFlags wall) => (Walls & wall) != 0;

        public void RemoveWall(WallFlags wall)
        {
            Walls &= ~wall;
        }

        public void AddWall(WallFlags wall)
        {
            Walls |= wall;
        }

        public void Reset()
        {
            IsActive = true;
            Walls = WallFlags.All;
            IsRoom = false;
        }
    }
}
