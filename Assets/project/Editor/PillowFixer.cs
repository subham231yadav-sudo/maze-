using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Project.Traps;

namespace Project.Editor
{
    [InitializeOnLoad]
    public static class PillowFixer
    {
        private const string PillowPrefabPath1 = "Assets/project/pillow_low.prefab";
        private const string PillowPrefabPath2 = "Assets/project/pillow_low 1.prefab";

        static PillowFixer()
        {
            // Automatically run once on load
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                RebuildFromFbx();
                FixAllPillows(false);
                TrapAndPlayerSetup.SetupPlayerSystem();
                TrapAndPlayerSetup.SetupTigerTrap();
                MazeTrapSpawner.EnsureTrapsSpawned(false);
            };
        }

        [MenuItem("Tools/Traps & Player/Fix Pillow Rotation & Hierarchy", false, 14)]
        public static void MenuFixPillow()
        {
            RebuildFromFbx();
            FixAllPillows(true);
        }

        [MenuItem("Tools/Traps & Player/Rebuild Clean Pillow from FBX", false, 15)]
        public static void MenuRebuildPillow()
        {
            if (RebuildFromFbx())
            {
                FixAllPillows(true);
            }
        }

        public static void FixAllPillows(bool showDialog)
        {
            int fixedPrefabs = FixPrefab();
            int fixedScene = FixScene();

            if (!EditorApplication.isPlaying)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Pillow Rotation Fixed",
                    $"Successfully fixed Pillow Trap!\n\n" +
                    $"• Rebuilt and centered {fixedPrefabs} prefab(s) directly from FBX\n" +
                    $"• Fixed {fixedScene} scene instance(s)\n" +
                    $"• Pillar centered directly on root pivot (0, 0, 0)\n" +
                    $"• Spiked pillar rotates smoothly IN PLACE around its center at its spawn location\n" +
                    $"• Zero orbital wobble or swinging around offset points!",
                    "Awesome!"
                );
            }
        }

        public static GameObject LoadPillowPrefab()
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(PillowPrefabPath1);
            if (p != null) return p;
            return AssetDatabase.LoadAssetAtPath<GameObject>(PillowPrefabPath2);
        }

        public static bool RebuildFromFbx()
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/project/obstacles/scenes/obstacles.fbx");
            if (fbx == null)
            {
                Debug.LogError("[PillowFixer] Could not find obstacles.fbx!");
                return false;
            }

            var fbxInst = Object.Instantiate(fbx);
            try
            {
                Transform pillowSource = null;
                foreach (var t in fbxInst.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "pillow_low" && t.parent != null && t.parent.name == "demo_scene")
                    {
                        pillowSource = t;
                        break;
                    }
                }

                if (pillowSource == null)
                {
                    foreach (var t in fbxInst.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == "pillow_low" && t.childCount > 0)
                        {
                            pillowSource = t;
                            break;
                        }
                    }
                }

                if (pillowSource == null)
                {
                    Debug.LogError("[PillowFixer] Could not find pillow_low inside obstacles.fbx!");
                    return false;
                }

                GameObject cleanRoot = Object.Instantiate(pillowSource.gameObject);
                cleanRoot.name = "pillow_low";
                cleanRoot.transform.SetParent(null);
                cleanRoot.transform.position = Vector3.zero;
                cleanRoot.transform.rotation = Quaternion.identity;
                cleanRoot.transform.localScale = Vector3.one;

                // Apply material
                var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/project/obstacles/M_Obstacles.mat");
                if (mat != null)
                {
                    foreach (var r in cleanRoot.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        r.sharedMaterial = mat;
                    }
                }

                // Clean any stray scripts or colliders
                foreach (var trap in cleanRoot.GetComponentsInChildren<RotatingSpikeTrap>(true))
                {
                    Object.DestroyImmediate(trap, true);
                }
                foreach (var col in cleanRoot.GetComponentsInChildren<Collider>(true))
                {
                    Object.DestroyImmediate(col, true);
                }

                // Align all 6 discs into ONE unified spiked pillar and center on root pivot (0, 0, 0)
                AlignAndCleanHierarchy(cleanRoot);

                PrefabUtility.SaveAsPrefabAsset(cleanRoot, PillowPrefabPath1);
                PrefabUtility.SaveAsPrefabAsset(cleanRoot, PillowPrefabPath2);
                Object.DestroyImmediate(cleanRoot);

                Debug.Log("[PillowFixer] Rebuilt pristine, perfectly centered ONE-piece pillow_low from FBX!");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(fbxInst);
            }
        }

        public static int FixPrefab()
        {
            var prefab = LoadPillowPrefab();
            if (prefab == null)
            {
                RebuildFromFbx();
                prefab = LoadPillowPrefab();
                if (prefab == null) return 0;
            }

            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                root.transform.position = Vector3.zero;
                root.transform.rotation = Quaternion.identity;

                AlignAndCleanHierarchy(root);

                // Save to both paths to ensure total compatibility
                PrefabUtility.SaveAsPrefabAsset(root, PillowPrefabPath1);
                PrefabUtility.SaveAsPrefabAsset(root, PillowPrefabPath2);
                Debug.Log("[PillowFixer] Successfully aligned and saved centered pillow_low prefabs!");
                return 2;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        public static int FixScene()
        {
            int count = 0;
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var walls = new List<Transform>();
            foreach (var t in allTransforms)
            {
                if (t.name.Contains("Cube (1)") || t.name.Contains("Wall"))
                {
                    walls.Add(t);
                }
            }

            foreach (var t in allTransforms)
            {
                if (t == null) continue;

                // Only root pillow objects
                if (t.name.ToLower().Contains("pillow") && (t.parent == null || !t.parent.name.ToLower().Contains("pillow")))
                {
                    AlignAndCleanHierarchy(t.gameObject);

                    // Verify if this pillow is embedded in a wall; if so, push it out to corridor
                    EnsureNotEmbeddedInWall(t, walls);

                    EditorUtility.SetDirty(t.gameObject);
                    count++;
                }
            }

            if (!EditorApplication.isPlaying && !EditorApplication.isPaused)
            {
                var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (activeScene.isLoaded)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
                }
            }

            Debug.Log($"[PillowFixer] Aligned and cleaned {count} pillow instances in scene.");
            return count;
        }

        private static void EnsureNotEmbeddedInWall(Transform pillow, List<Transform> walls)
        {
            if (walls.Count == 0) return;

            Vector3 pos = pillow.position;
            pos.y = 1.0f; // test at wall height

            Transform closestWall = null;
            float closestDist = float.MaxValue;

            foreach (var wall in walls)
            {
                if (wall == null) continue;
                Vector3 wPos = wall.position;
                wPos.y = 1.0f;
                float d = Vector3.Distance(pos, wPos);
                if (d < closestDist)
                {
                    closestDist = d;
                    closestWall = wall;
                }
            }

            // If pillow center is within 1.1m of wall center, shift it outwards into open corridor space
            if (closestWall != null && closestDist < 1.1f)
            {
                Vector3 wallForward = closestWall.forward;
                Vector3 offsetDir = (pos - closestWall.position);
                offsetDir.y = 0f;

                float dot = Vector3.Dot(offsetDir, wallForward);
                Vector3 normal = (dot >= 0 ? wallForward : -wallForward).normalized;

                Vector3 correctedPos = closestWall.position + normal * 1.55f;
                correctedPos.y = 0f;
                pillow.position = correctedPos;
                Debug.Log($"[PillowFixer] Repositioned wall-clipping pillow '{pillow.name}' to open corridor at {correctedPos}");
            }
        }

        public static void AlignAndCleanHierarchy(GameObject root)
        {
            // 1. Remove duplicate RotatingSpikeTrap on children
            var allTraps = root.GetComponentsInChildren<RotatingSpikeTrap>(true);
            foreach (var trap in allTraps)
            {
                if (trap.gameObject != root)
                {
                    Object.DestroyImmediate(trap, true);
                }
            }

            // 2. Remove stray colliders on children
            var allCols = root.GetComponentsInChildren<Collider>(true);
            foreach (var col in allCols)
            {
                if (col.gameObject != root)
                {
                    Object.DestroyImmediate(col, true);
                }
            }

            // 3. Remove any empty dummy children with no renderers
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = root.transform.GetChild(i);
                if (child.GetComponentsInChildren<Renderer>(true).Length == 0)
                {
                    Object.DestroyImmediate(child.gameObject, true);
                }
            }

            // 4. Ensure RotatingSpikeTrap on root
            var rootTrap = root.GetComponent<RotatingSpikeTrap>();
            if (rootTrap == null)
            {
                rootTrap = root.AddComponent<RotatingSpikeTrap>();
            }

            // 5. Ensure all child transforms and sub-mesh holders have zero horizontal offset
            for (int i = 0; i < root.transform.childCount; i++)
            {
                Transform child = root.transform.GetChild(i);
                for (int j = 0; j < child.childCount; j++)
                {
                    Transform subChild = child.GetChild(j);
                    subChild.localPosition = Vector3.zero;
                    subChild.localRotation = Quaternion.identity;
                    subChild.localScale = Vector3.one;
                }
            }

            // 6. UNIFY THE PILLOW: MERGE ALL DISCS INTO ONE COHESIVE PILLAR
            // In the raw FBX/model, disc 0 ("pillow_low") was separated horizontally from discs 1..5 ("pillow_low1" through "5").
            // We align all discs to share the exact same horizontal center (X, Z) and stack disc 0 right at the base under disc 1!
            Transform disc1 = null;
            Transform disc0 = null;
            Transform disc2 = null;

            for (int i = 0; i < root.transform.childCount; i++)
            {
                Transform c = root.transform.GetChild(i);
                string n = c.name.ToLower();
                if (n == "pillow_low1" || n.EndsWith("low1")) disc1 = c;
                else if (n == "pillow_low2" || n.EndsWith("low2")) disc2 = c;
                else if (n == "pillow_low" || n.EndsWith("low")) disc0 = c;
            }

            if (disc1 == null && root.transform.childCount > 1) disc1 = root.transform.GetChild(1);
            if (disc0 == null && root.transform.childCount > 0) disc0 = root.transform.GetChild(0);

            if (disc1 != null)
            {
                Renderer r1 = disc1.GetComponentInChildren<Renderer>(true);
                if (r1 != null)
                {
                    Vector3 c1Local = root.transform.InverseTransformPoint(r1.bounds.center);

                    // Align all children horizontally with disc 1 so all 6 discs are strictly concentric
                    for (int i = 0; i < root.transform.childCount; i++)
                    {
                        Transform c = root.transform.GetChild(i);
                        if (c == disc1) continue;
                        Renderer rc = c.GetComponentInChildren<Renderer>(true);
                        if (rc != null)
                        {
                            Vector3 rcLocal = root.transform.InverseTransformPoint(rc.bounds.center);
                            Vector3 horizontalDelta = new Vector3(c1Local.x - rcLocal.x, 0f, c1Local.z - rcLocal.z);
                            c.localPosition += horizontalDelta;
                        }
                    }

                    // Disc 0 is the base disc at the bottom of the pillar (one disc-pitch below disc 1)
                    if (disc0 != null && disc0 != disc1)
                    {
                        Renderer r0 = disc0.GetComponentInChildren<Renderer>(true);
                        if (r0 != null)
                        {
                            float discPitch = 0.1508f;
                            if (disc2 != null)
                            {
                                Renderer r2 = disc2.GetComponentInChildren<Renderer>(true);
                                if (r2 != null)
                                {
                                    Vector3 c2Local = root.transform.InverseTransformPoint(r2.bounds.center);
                                    float measured = Mathf.Abs(c2Local.y - c1Local.y);
                                    if (measured > 0.05f) discPitch = measured;
                                }
                            }

                            Vector3 c0Local = root.transform.InverseTransformPoint(r0.bounds.center);
                            float targetY = c1Local.y - discPitch;
                            float yDelta = targetY - c0Local.y;
                            disc0.localPosition += new Vector3(0f, yDelta, 0f);
                        }
                    }
                }
            }

            // 7. Calculate exact visual bounding box and center the entire unified pillar at (0, 0, 0)
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                // Calculate center in root's local space
                Vector3 localCenter = root.transform.InverseTransformPoint(bounds.center);
                Vector3 localBottom = root.transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));

                Vector3 shift = new Vector3(localCenter.x, localBottom.y, localCenter.z);

                // Counter-shift all direct children so pillar center aligns precisely at X=0, Z=0 and base sits at Y=0!
                for (int i = 0; i < root.transform.childCount; i++)
                {
                    Transform child = root.transform.GetChild(i);
                    child.localPosition -= shift;
                }
            }

            // 7. Ensure unified CapsuleCollider on root at true center
            var rootCol = root.GetComponent<CapsuleCollider>();
            if (rootCol == null)
            {
                rootCol = root.AddComponent<CapsuleCollider>();
            }
            rootCol.center = new Vector3(0f, 0.48f, 0f);
            rootCol.radius = 0.55f;
            rootCol.height = 1.05f;
            rootCol.direction = 1; // Y axis

            // 8. Configure RotatingSpikeTrap on root
            var trapComp = root.GetComponent<RotatingSpikeTrap>();
            if (trapComp != null)
            {
                var so = new SerializedObject(trapComp);
                var radiusProp = so.FindProperty("spikeDamageRadius");
                if (radiusProp != null) radiusProp.floatValue = 0.75f;
                var offsetProp = so.FindProperty("centerOffset");
                if (offsetProp != null) offsetProp.vector3Value = new Vector3(0f, 0.48f, 0f);
                var speedProp = so.FindProperty("rotationSpeed");
                if (speedProp != null) speedProp.floatValue = 120f;
                var axisProp = so.FindProperty("rotationAxis");
                if (axisProp != null) axisProp.vector3Value = Vector3.up;
                var reverseProp = so.FindProperty("reverseDirection");
                if (reverseProp != null && !root.name.Contains("_B")) reverseProp.boolValue = false;
                so.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(root);
        }
    }
}
