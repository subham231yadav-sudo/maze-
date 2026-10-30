using System;

namespace MazeGenerator
{
    /// <summary>
    /// Creates algorithm instances by type.
    /// </summary>
    public static class MazeAlgorithmFactory
    {
        public static IMazeAlgorithm Create(MazeAlgorithmType type)
        {
            switch (type)
            {
                case MazeAlgorithmType.Prim:
                    return new PrimAlgorithm();
                case MazeAlgorithmType.Eller:
                    return new EllerAlgorithm();
                case MazeAlgorithmType.RecursiveBacktracker:
                    return new RecursiveBacktrackerAlgorithm();
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
