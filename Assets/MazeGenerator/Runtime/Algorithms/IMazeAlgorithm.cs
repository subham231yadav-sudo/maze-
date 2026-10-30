namespace MazeGenerator
{
    /// <summary>
    /// Carves passages into an already masked grid.
    /// </summary>
    public interface IMazeAlgorithm
    {
        string Name { get; }

        /// <summary>
        /// Generates a perfect maze on active cells. Grid walls should already be reset.
        /// </summary>
        void Generate(MazeGrid grid, MazeRandom random, int startX, int startY);
    }
}
