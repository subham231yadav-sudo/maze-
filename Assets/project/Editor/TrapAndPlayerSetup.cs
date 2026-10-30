using UnityEditor;
using UnityEngine;
using Project.Player;
using Project.Traps;
using Project.UI;

namespace Project.Editor
{
    public static class TrapAndPlayerSetup
    {
        private const string TigerPrefabPath = "Assets/project/tiger.prefab";
        private const string PillowPrefabPath = "Assets/project/pillow_low.prefab";
        private const string FpcPrefabPath = "Assets/Mini First Person Controller/First Person Controller.prefab";

        [MenuItem("Tools/Traps & Player/1-Click Setup All (Player + Traps + Finish Line)", false, 10)]
        public static void SetupAll()
        {
            SetupPlayerSystem();
            SetupTigerTrap();
            SetupPillowTrap();
            SetupFinishLineSystem();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Setup Complete",
                "Successfully configured all game systems:\n\n" +
                "1. Player Health & HUD (Razor-sharp anti-aliased HUD, live stopwatch & distance tracking)\n" +
                "2. Tiger Fire Trap (Sprays fire every 2s, deals damage over time)\n" +
                "3. Pillow Spiked Trap (Continuously rotates, deals damage & knockback on contact)\n" +
                "4. Finish Line & Escape Victory UI (Towering sky beacon beam, glowing exit pad, confetti & victory card)",
                "Awesome!"
            );
        }

        [MenuItem("Tools/Traps & Player/Setup Player Health & HUD", false, 11)]
        public static void SetupPlayerSystem()
        {
            int count = 0;

            // 1. Scene Instances
            var players = Object.FindObjectsByType<FirstPersonMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p.GetComponent<PlayerHealth>() == null)
                {
                    Undo.AddComponent<PlayerHealth>(p.gameObject);
                }
                if (p.GetComponent<PlayerHealthUI>() == null)
                {
                    Undo.AddComponent<PlayerHealthUI>(p.gameObject);
                }
                if (p.GetComponent<MazeObjectiveUI>() == null)
                {
                    Undo.AddComponent<MazeObjectiveUI>(p.gameObject);
                }
                p.gameObject.tag = "Player";
                EditorUtility.SetDirty(p.gameObject);
                count++;
            }

            // 2. Prefab
            var fpcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FpcPrefabPath);
            if (fpcPrefab != null)
            {
                bool modified = false;
                if (fpcPrefab.GetComponent<PlayerHealth>() == null)
                {
                    fpcPrefab.AddComponent<PlayerHealth>();
                    modified = true;
                }
                if (fpcPrefab.GetComponent<PlayerHealthUI>() == null)
                {
                    fpcPrefab.AddComponent<PlayerHealthUI>();
                    modified = true;
                }
                if (fpcPrefab.GetComponent<MazeObjectiveUI>() == null)
                {
                    fpcPrefab.AddComponent<MazeObjectiveUI>();
                    modified = true;
                }
                fpcPrefab.tag = "Player";
                if (modified)
                {
                    PrefabUtility.SavePrefabAsset(fpcPrefab);
                    count++;
                }
            }

            Debug.Log($"[TrapAndPlayerSetup] Configured Player System on {count} object(s).");
        }

        public static void SetupTigerTraps() => SetupTigerTrap();

        [MenuItem("Tools/Traps & Player/Setup Tiger Fire Trap", false, 12)]
        public static void SetupTigerTrap()
        {
            int count = 0;

            // 1. Scene Instances
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                if (t.name.ToLower().Contains("tiger"))
                {
                    if (ConfigureTiger(t.gameObject))
                    {
                        count++;
                    }
                }
            }

            // 2. Prefab
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TigerPrefabPath);
            if (prefab != null)
            {
                if (ConfigureTiger(prefab))
                {
                    PrefabUtility.SavePrefabAsset(prefab);
                    count++;
                }
            }

            Debug.Log($"[TrapAndPlayerSetup] Configured Tiger Fire Trap on {count} object(s).");
        }

        private static bool ConfigureTiger(GameObject go)
        {
            bool modified = false;

            // Remove CFX AutoDestruct components on all children so particle is NEVER destroyed
            var autoDestructs = go.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in autoDestructs)
            {
                if (mb != null && mb.GetType().Name.Contains("AutoDestruct"))
                {
                    Object.DestroyImmediate(mb, true);
                    modified = true;
                }
            }

            // Ensure FireSprayTrap component
            var trap = go.GetComponent<FireSprayTrap>();
            if (trap == null)
            {
                trap = go.AddComponent<FireSprayTrap>();
                modified = true;
            }

            // Ensure TigerWallAligner component for automatic wall-face orientation
            var aligner = go.GetComponent<TigerWallAligner>();
            if (aligner == null)
            {
                aligner = go.AddComponent<TigerWallAligner>();
                modified = true;
            }

            EditorUtility.SetDirty(go);
            return modified;
        }

        [MenuItem("Tools/Traps & Player/Setup Rotating Pillow Trap", false, 13)]
        public static void SetupPillowTrap()
        {
            PillowFixer.FixAllPillows(false);
            Debug.Log("[TrapAndPlayerSetup] Configured Rotating Pillow Trap cleanly.");
        }

        private static bool ConfigurePillow(GameObject go)
        {
            PillowFixer.AlignAndCleanHierarchy(go);
            return true;
        }

        [MenuItem("Tools/Traps & Player/Setup Finish Line & Victory UI", false, 14)]
        public static void SetupFinishLineSystem()
        {
            // 1. Locate Finish Line in scene
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            GameObject finishLineObj = null;

            foreach (var t in allTransforms)
            {
                string lower = t.name.ToLower();
                if (lower == "finish line" || lower == "finishline" || lower.Contains("finish line") || t.CompareTag("Finish"))
                {
                    finishLineObj = t.gameObject;
                    break;
                }
            }

            if (finishLineObj == null)
            {
                finishLineObj = new GameObject("finish line");
                finishLineObj.transform.position = new Vector3(546.1905f, 0f, 467.7647f);
                Undo.RegisterCreatedObjectUndo(finishLineObj, "Create finish line");
            }

            finishLineObj.tag = "Finish";

            // 2. Configure BoxCollider as trigger
            var col = finishLineObj.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = Undo.AddComponent<BoxCollider>(finishLineObj);
            }
            col.isTrigger = true;
            Vector3 size = col.size;
            if (size.x < 3f) size.x = 5f;
            if (size.y < 2.5f) size.y = 3.5f;
            if (size.z < 2f) size.z = 3.5f;
            col.size = size;

            if (col.center.y < 0.5f)
            {
                col.center = new Vector3(col.center.x, 1.5f, col.center.z);
            }

            // 3. Attach FinishLineTrigger
            var trigger = finishLineObj.GetComponent<FinishLineTrigger>();
            if (trigger == null)
            {
                trigger = Undo.AddComponent<FinishLineTrigger>(finishLineObj);
            }
            trigger.SetupCollider();

            // 4. Ensure MazeVictoryUI exists
            var victoryUI = Object.FindFirstObjectByType<MazeVictoryUI>(FindObjectsInactive.Include);
            if (victoryUI == null)
            {
                var victoryMgr = new GameObject("MazeVictoryManager");
                victoryUI = victoryMgr.AddComponent<MazeVictoryUI>();
                Undo.RegisterCreatedObjectUndo(victoryMgr, "Create MazeVictoryManager");
            }

            // 5. Ensure high-quality CanvasScaler settings on all Canvases
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                var scaler = c.GetComponent<UnityEngine.UI.CanvasScaler>();
                if (scaler != null)
                {
                    UIThemeHelper.ApplyHighQualityCanvasSettings(scaler);
                    EditorUtility.SetDirty(scaler);
                }
            }

            EditorUtility.SetDirty(finishLineObj);
            var scene = finishLineObj.scene;
            if (scene.isLoaded)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            }

            Debug.Log($"[TrapAndPlayerSetup] Successfully configured Finish Line & Victory UI on '{finishLineObj.name}'!");
        }
    }
}
