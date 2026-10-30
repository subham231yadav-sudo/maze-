using MazeGenerator;
using UnityEditor;
using UnityEngine;

namespace MazeGenerator.Editor
{
    /// <summary>
    /// Edit Mode A* visualization: draw the solution path for each opening pair.
    /// Status text is stored on the behaviour (no static mutable state).
    /// </summary>
    public static class MazeSolveDebugVisualizer
    {
        public static void Start(MazeGeneratorBehaviour behaviour)
        {
            Stop(behaviour);

            if (behaviour == null)
            {
                return;
            }

            if (behaviour.CurrentGrid == null)
            {
                EditorUtility.DisplayDialog("Maze Generator", "Generate a maze first.", "OK");
                return;
            }

            if (behaviour.Prefabs == null || behaviour.Prefabs.DebugSolutionPrefab == null)
            {
                EditorUtility.DisplayDialog(
                    "Maze Generator",
                    "Assign Debug Solution Prefab on the Maze Generator.",
                    "OK");
                return;
            }

            var openings = MazeOpeningFinder.FindOpenings(behaviour.CurrentGrid);
            if (openings.Count < 2)
            {
                EditorUtility.DisplayDialog(
                    "Maze Generator",
                    $"Need at least 2 openings to solve (found {openings.Count}). Increase Opening Count and regenerate.",
                    "OK");
                return;
            }

            MazeEditorReloadGuard.FocusBehaviour(behaviour);

            var markers = behaviour.CreateDebugMarkerBuilder();
            markers.Clear();

            var pairCount = 0;
            var solvedCount = 0;
            for (var i = 0; i < openings.Count; i++)
            {
                for (var j = i + 1; j < openings.Count; j++)
                {
                    var result = MazeAStarSolver.Solve(behaviour.CurrentGrid, openings[i], openings[j]);
                    pairCount++;

                    if (!result.Success)
                    {
                        continue;
                    }

                    solvedCount++;
                    markers.SpawnSolutionPath(result.Path);
                }
            }

            behaviour.SolveDebugStatus =
                $"Solve debug complete ({solvedCount}/{pairCount} pair(s), {openings.Count} openings).";
            MazeEditorReloadGuard.FocusBehaviour(behaviour);
            SceneView.RepaintAll();
        }

        public static void Stop(MazeGeneratorBehaviour behaviour)
        {
            if (behaviour != null)
            {
                behaviour.SolveDebugStatus = string.Empty;
            }
        }

        public static void Clear(MazeGeneratorBehaviour behaviour)
        {
            Stop(behaviour);
            if (behaviour != null)
            {
                MazeEditorReloadGuard.FocusBehaviour(behaviour);
                behaviour.ClearDebugMarkers();
            }

            SceneView.RepaintAll();
        }
    }
}
