using MazeGenerator;
using UnityEditor;
using UnityEngine;

namespace MazeGenerator.Editor
{
    [CustomEditor(typeof(MazeGeneratorBehaviour))]
    public sealed class MazeGeneratorBehaviourEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (target == null)
            {
                return;
            }

            DrawDefaultInspector();

            var behaviour = target as MazeGeneratorBehaviour;
            if (behaviour == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            var prefabError = behaviour.Prefabs?.ValidateRequired();
            using (new EditorGUI.DisabledScope(prefabError != null))
            {
                if (GUILayout.Button("Generate Maze"))
                {
                    RunSafe(behaviour, () => behaviour.Generate());
                }

                if (GUILayout.Button("Reseed & Generate"))
                {
                    RunSafe(behaviour, () => behaviour.ReseedAndGenerate());
                }

                if (GUILayout.Button("Clear Maze"))
                {
                    RunSafe(behaviour, () => behaviour.Clear());
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug Solve (A*)", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(
                       prefabError != null ||
                       behaviour.Prefabs?.DebugSolutionPrefab == null ||
                       behaviour.CurrentGrid == null))
            {
                if (GUILayout.Button("Solve / Debug Maze"))
                {
                    MazeEditorReloadGuard.FocusBehaviour(behaviour);
                    MazeSolveDebugVisualizer.Start(behaviour);
                }
            }

            if (GUILayout.Button("Clear Debug Markers"))
            {
                RunSafe(behaviour, () => MazeSolveDebugVisualizer.Clear(behaviour));
            }

            if (!string.IsNullOrEmpty(behaviour.SolveDebugStatus))
            {
                EditorGUILayout.HelpBox(behaviour.SolveDebugStatus, MessageType.Info);
            }

            var error = behaviour.Parameters?.Validate();
            if (!string.IsNullOrEmpty(error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }
            else if (prefabError != null)
            {
                EditorGUILayout.HelpBox(prefabError, MessageType.Warning);
            }
            else if (behaviour.Prefabs.DebugSolutionPrefab == null)
            {
                EditorGUILayout.HelpBox("Assign Debug Solution Prefab for Solve / Debug.", MessageType.Warning);
            }
        }

        static void RunSafe(MazeGeneratorBehaviour behaviour, System.Action action)
        {
            MazeSolveDebugVisualizer.Stop(behaviour);
            MazeEditorReloadGuard.FocusBehaviour(behaviour);
            action();
            MazeEditorReloadGuard.FocusBehaviour(behaviour);
            if (behaviour != null)
            {
                EditorUtility.SetDirty(behaviour);
            }
        }
    }
}
