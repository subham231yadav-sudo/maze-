using System.Collections.Generic;

namespace MazeGenerator
{
    /// <summary>
    /// Eller's algorithm. On non-rectangular masks, only active cells in each row participate.
    /// </summary>
    public sealed class EllerAlgorithm : IMazeAlgorithm
    {
        public string Name => "Eller";

        public void Generate(MazeGrid grid, MazeRandom random, int startX, int startY)
        {
            // start cell is unused by Eller but kept for interface consistency
            _ = startX;
            _ = startY;

            var sets = new int[grid.Width];
            var nextSetId = 1;

            for (var y = 0; y < grid.Height; y++)
            {
                var isLastRow = y == grid.Height - 1;
                AssignSetsForRow(grid, sets, ref nextSetId, y);

                JoinAdjacentInRow(grid, random, sets, y, forceJoin: isLastRow);
                if (!isLastRow)
                {
                    CarveVerticalConnections(grid, random, sets, y);
                }
            }
        }

        static void AssignSetsForRow(MazeGrid grid, int[] sets, ref int nextSetId, int y)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                if (!grid.IsActive(x, y))
                {
                    sets[x] = 0;
                    continue;
                }

                if (sets[x] == 0)
                {
                    sets[x] = nextSetId++;
                }
            }
        }

        static void JoinAdjacentInRow(MazeGrid grid, MazeRandom random, int[] sets, int y, bool forceJoin)
        {
            for (var x = 0; x < grid.Width - 1; x++)
            {
                if (!grid.IsActive(x, y) || !grid.IsActive(x + 1, y))
                {
                    continue;
                }

                if (sets[x] == sets[x + 1])
                {
                    continue;
                }

                if (forceJoin || random.Chance(0.5f))
                {
                    grid.Carve(x, y, Direction.East);
                    MergeSets(sets, sets[x + 1], sets[x]);
                }
            }
        }

        static void CarveVerticalConnections(MazeGrid grid, MazeRandom random, int[] sets, int y)
        {
            var cellsBySet = new Dictionary<int, List<int>>();
            for (var x = 0; x < grid.Width; x++)
            {
                if (!grid.IsActive(x, y) || sets[x] == 0)
                {
                    continue;
                }

                if (!cellsBySet.TryGetValue(sets[x], out var list))
                {
                    list = new List<int>();
                    cellsBySet[sets[x]] = list;
                }

                list.Add(x);
            }

            var nextSets = new int[grid.Width];
            foreach (var pair in cellsBySet)
            {
                var members = pair.Value;
                random.Shuffle(members);

                var connections = random.Next(1, members.Count + 1);
                for (var i = 0; i < connections; i++)
                {
                    var x = members[i];
                    if (!grid.IsActive(x, y + 1))
                    {
                        // try another member if possible
                        continue;
                    }

                    grid.Carve(x, y, Direction.North);
                    nextSets[x] = pair.Key;
                }

                // Guarantee at least one vertical link when possible
                var linked = false;
                for (var i = 0; i < members.Count; i++)
                {
                    if (nextSets[members[i]] == pair.Key)
                    {
                        linked = true;
                        break;
                    }
                }

                if (!linked)
                {
                    foreach (var x in members)
                    {
                        if (grid.IsActive(x, y + 1))
                        {
                            grid.Carve(x, y, Direction.North);
                            nextSets[x] = pair.Key;
                            break;
                        }
                    }
                }
            }

            for (var x = 0; x < grid.Width; x++)
            {
                sets[x] = nextSets[x];
            }
        }

        static void MergeSets(int[] sets, int from, int to)
        {
            for (var i = 0; i < sets.Length; i++)
            {
                if (sets[i] == from)
                {
                    sets[i] = to;
                }
            }
        }
    }
}
