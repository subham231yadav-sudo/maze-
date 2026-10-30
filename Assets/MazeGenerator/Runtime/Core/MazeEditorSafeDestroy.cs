using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MazeGenerator
{
    /// <summary>
    /// Destroys child objects without leaving the Inspector pointing at deleted targets
    /// (avoids SerializedObjectNotCreatableException / MissingReferenceException in Edit Mode).
    /// </summary>
    public static class MazeEditorSafeDestroy
    {
        public static void DestroyChildren(Transform root)
        {
            if (root == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                AvoidInspectingHierarchy(root);
            }
#endif

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Object.DestroyImmediate(child);
                    continue;
                }
#endif
                Object.Destroy(child);
            }
        }

        public static void DestroyGameObject(GameObject go, Transform hierarchyRoot)
        {
            if (go == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying && hierarchyRoot != null)
            {
                AvoidInspectingHierarchy(hierarchyRoot);
            }
#endif

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(go);
                return;
            }
#endif
            Object.Destroy(go);
        }

#if UNITY_EDITOR
        static void AvoidInspectingHierarchy(Transform root)
        {
            // Always pin selection to the generator (or root) before DestroyImmediate.
            var keep = root.parent != null ? root.parent.gameObject : root.gameObject;
            Selection.activeGameObject = keep;
        }
#endif
    }
}
