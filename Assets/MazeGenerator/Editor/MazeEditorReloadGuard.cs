using UnityEditor;
using UnityEngine;

namespace MazeGenerator.Editor
{
    /// <summary>
    /// Stops Edit Mode solve animation and sanitizes Selection before domain reload,
    /// preventing GameObjectInspector / TransformInspector null-target exceptions.
    /// </summary>
    [InitializeOnLoad]
    static class MazeEditorReloadGuard
    {
        static MazeEditorReloadGuard()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        static void OnBeforeAssemblyReload()
        {
            SanitizeSelection();
        }

        public static void SanitizeSelection()
        {
            var objects = Selection.objects;
            if (objects == null || objects.Length == 0)
            {
                return;
            }

            var hasNull = false;
            for (var i = 0; i < objects.Length; i++)
            {
                if (objects[i] == null)
                {
                    hasNull = true;
                    break;
                }
            }

            if (!hasNull)
            {
                return;
            }

            var valid = new Object[objects.Length];
            var count = 0;
            for (var i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                {
                    valid[count++] = objects[i];
                }
            }

            if (count == 0)
            {
                Selection.activeObject = null;
                return;
            }

            var trimmed = new Object[count];
            for (var i = 0; i < count; i++)
            {
                trimmed[i] = valid[i];
            }

            Selection.objects = trimmed;
        }

        public static void FocusBehaviour(MazeGeneratorBehaviour behaviour)
        {
            if (behaviour != null)
            {
                Selection.activeGameObject = behaviour.gameObject;
            }
        }
    }
}
