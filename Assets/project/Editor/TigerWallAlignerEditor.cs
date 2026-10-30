using Project.Traps;
using UnityEditor;
using UnityEngine;

namespace Project.Editor
{
    [CustomEditor(typeof(TigerWallAligner))]
    [CanEditMultipleObjects]
    public class TigerWallAlignerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Tiger Wall Auto-Aligner:\nWhen you move or place this Tiger near a wall, it automatically turns to face OPPOSITE the wall face (into the corridor) and mounts flush to the wall surface.",
                MessageType.Info
            );

            GUILayout.Space(6);
            DrawDefaultInspector();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            GUI.backgroundColor = new Color(0.3f, 0.85f, 0.4f);
            if (GUILayout.Button("Snap & Face Opposite Wall Now", GUILayout.Height(32)))
            {
                foreach (var t in targets)
                {
                    if (t is TigerWallAligner aligner)
                    {
                        aligner.AlignToNearestWall(true);
                    }
                }
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(4);
            if (GUILayout.Button("Flip Facing Direction (180°)", GUILayout.Height(26)))
            {
                foreach (var t in targets)
                {
                    if (t is TigerWallAligner aligner)
                    {
                        aligner.FlipFacingDirection();
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("Tools/Traps & Player/Align All Tigers to Wall Faces", false, 19)]
        public static void AlignAllTigersInScene()
        {
            var tigers = Object.FindObjectsByType<TigerWallAligner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int count = 0;
            foreach (var tiger in tigers)
            {
                if (tiger.AlignToNearestWall(true))
                {
                    count++;
                }
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.isLoaded)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            }

            EditorUtility.DisplayDialog(
                "Tigers Aligned",
                $"Successfully aligned {count} Tiger fire trap(s) to face opposite their walls!",
                "Awesome!"
            );
        }
    }
}
