using System;

namespace MazeGenerator
{
    /// <summary>
    /// Deterministic RNG wrapper for maze generation.
    /// </summary>
    public sealed class MazeRandom
    {
        readonly Random _random;

        public MazeRandom(int seed)
        {
            _random = new Random(seed);
        }

        public int Next(int maxExclusive) => _random.Next(maxExclusive);

        public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);

        public float NextFloat() => (float)_random.NextDouble();

        public bool Chance(float probability) => probability > 0f && NextFloat() < probability;

        public void Shuffle<T>(System.Collections.Generic.IList<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
