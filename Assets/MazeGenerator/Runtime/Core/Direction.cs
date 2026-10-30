namespace MazeGenerator
{
    /// <summary>
    /// Cardinal direction helpers for maze carving.
    /// </summary>
    public enum Direction
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public static class DirectionUtil
    {
        public static readonly Direction[] All =
        {
            Direction.North,
            Direction.East,
            Direction.South,
            Direction.West
        };

        public static WallFlags ToWall(Direction direction)
        {
            switch (direction)
            {
                case Direction.North: return WallFlags.North;
                case Direction.East: return WallFlags.East;
                case Direction.South: return WallFlags.South;
                case Direction.West: return WallFlags.West;
                default: return WallFlags.None;
            }
        }

        public static Direction Opposite(Direction direction)
        {
            switch (direction)
            {
                case Direction.North: return Direction.South;
                case Direction.East: return Direction.West;
                case Direction.South: return Direction.North;
                case Direction.West: return Direction.East;
                default: return direction;
            }
        }

        public static void Offset(Direction direction, out int dx, out int dy)
        {
            dx = 0;
            dy = 0;
            switch (direction)
            {
                case Direction.North: dy = 1; break;
                case Direction.East: dx = 1; break;
                case Direction.South: dy = -1; break;
                case Direction.West: dx = -1; break;
            }
        }
    }
}
