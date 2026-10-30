using System;

namespace MazeGenerator
{
    /// <summary>
    /// Cardinal wall presence flags for a maze cell.
    /// </summary>
    [Flags]
    public enum WallFlags
    {
        None = 0,
        North = 1 << 0,
        East = 1 << 1,
        South = 1 << 2,
        West = 1 << 3,
        All = North | East | South | West
    }
}
