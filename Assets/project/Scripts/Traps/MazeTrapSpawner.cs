using System.Collections.Generic;
using Project.Traps;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project.Traps
{
    [ExecuteAlways]
    public class MazeTrapSpawner : MonoBehaviour
    {
        private const string TigerPrefabPath = "Assets/project/tiger.prefab";
        private const string PillowPrefabPath = "Assets/project/pillow_low.prefab";
        private const string TrapsParentName = "--- MAZE TRAPS ---";

        [Header("Trap Counts")]
        [Tooltip("Number of wall-mounted Tiger Fire Traps to place (3x)")]
        [SerializeField] private int tigerCount = 24;
        [Tooltip("Number of single Pillow Spiked Traps to place (3x)")]
        [SerializeField] private int singlePillowCount = 15;
        [Tooltip("Number of double (paired) Pillow Spiked Traps to place (3x)")]
        [SerializeField] private int doublePillowPairs = 9;

        [Header("Spawn Settings")]
        [Tooltip("Minimum distance from player spawn where no traps are allowed")]
        [SerializeField] private float playerSafetyRadius = 7.0f;
        [Tooltip("Minimum distance between any two spawned traps")]
        [SerializeField] private float minTrapSpacing = 2.2f;
        [Tooltip("Height at which tiger heads are mounted on walls")]
        [SerializeField] private float tigerMountHeight = 1.65f;
        [Tooltip("Passage clearance for player when two pillows are placed")]
        [SerializeField] private float doublePillowPassageClearance = 1.25f;

        [Header("Auto Spawn")]
        [SerializeField] private bool autoSpawnOnStart = false;

        private void Start()
        {
            if (Application.isPlaying && autoSpawnOnStart)
            {
                var existing = GameObject.Find(TrapsParentName);
                if (existing == null || existing.transform.childCount == 0)
                {
                    SpawnTraps();
                }
            }
        }

#if UNITY_EDITOR
        public static void EnsureTrapsSpawned(bool forceRegenerate = false)
        {
            var existing = GameObject.Find(TrapsParentName);
            if (!forceRegenerate && existing != null && existing.transform.childCount > 0)
            {
                return;
            }

            var spawner = Object.FindFirstObjectByType<MazeTrapSpawner>();
            if (spawner == null)
            {
                var go = new GameObject("MazeTrapSpawner");
                spawner = go.AddComponent<MazeTrapSpawner>();
                Undo.RegisterCreatedObjectUndo(go, "Create MazeTrapSpawner");
            }

            spawner.SpawnTraps();

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.isLoaded)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }
        }

        [MenuItem("Tools/Traps & Player/Spawn Random Traps in Maze", false, 15)]
        public static void MenuSpawnTraps()
        {
            var spawner = Object.FindFirstObjectByType<MazeTrapSpawner>();
            if (spawner == null)
            {
                var go = new GameObject("MazeTrapSpawner");
                spawner = go.AddComponent<MazeTrapSpawner>();
                Undo.RegisterCreatedObjectUndo(go, "Create MazeTrapSpawner");
            }

            spawner.SpawnTraps();

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);

            EditorUtility.DisplayDialog(
                "Maze Traps Spawned",
                $"Successfully spawned random traps in the maze:\n\n" +
                $"• {spawner.tigerCount} Wall-mounted Tiger Fire Traps (centered on wall faces)\n" +
                $"• {spawner.singlePillowCount} Single Rotating Spiked Pillars\n" +
                $"• {spawner.doublePillowPairs * 2} Paired Pillars ({spawner.doublePillowPairs} pairs with guaranteed passage space for player)\n" +
                $"• Safe zone maintained around player spawn point!",
                "Awesome!"
            );
        }

        [MenuItem("Tools/Traps & Player/Clear Maze Traps", false, 16)]
        public static void MenuClearTraps()
        {
            var trapsRoot = GameObject.Find(TrapsParentName);
            if (trapsRoot != null)
            {
                Undo.DestroyObjectImmediate(trapsRoot);
                Debug.Log("[MazeTrapSpawner] Cleared all maze traps.");
            }
            else
            {
                Debug.Log("[MazeTrapSpawner] No maze traps root found to clear.");
            }
        }
#endif

        public void SpawnTraps()
        {
            // Load prefabs
            GameObject tigerPrefab = null;
            GameObject pillowPrefab = null;

#if UNITY_EDITOR
            tigerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TigerPrefabPath);
            pillowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PillowPrefabPath);
            if (pillowPrefab == null)
            {
                pillowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/project/pillow_low 1.prefab");
            }
#endif

            if (tigerPrefab == null || pillowPrefab == null)
            {
                Debug.LogError("[MazeTrapSpawner] Could not load tiger.prefab or pillow_low.prefab!");
                return;
            }

            // Find or create Traps root
            GameObject trapsRoot = GameObject.Find(TrapsParentName);
            if (trapsRoot != null)
            {
                // Clear existing children
                while (trapsRoot.transform.childCount > 0)
                {
                    var child = trapsRoot.transform.GetChild(0).gameObject;
                    if (Application.isPlaying) Destroy(child);
                    else DestroyImmediate(child);
                }
            }
            else
            {
                trapsRoot = new GameObject(TrapsParentName);
            }

            // Find player position
            Vector3 playerPos = Vector3.zero;
            var player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                var fpc = Object.FindFirstObjectByType<FirstPersonMovement>();
                if (fpc != null) player = fpc.gameObject;
            }
            if (player != null)
            {
                playerPos = player.transform.position;
            }

            // Collect all wall segments
            List<Transform> walls = FindAllMazeWalls();
            if (walls.Count == 0)
            {
                Debug.LogWarning("[MazeTrapSpawner] No maze walls found in scene!");
                return;
            }

            // Shuffle walls list for random distribution
            ShuffleList(walls);

            List<Vector3> occupiedPositions = new List<Vector3>();
            int tigersSpawned = 0;
            int singlePillowsSpawned = 0;
            int doublePillowPairsSpawned = 0;

            // ==========================================
            // 1. SPAWN TIGER FIRE TRAPS ON WALLS
            // ==========================================
            foreach (var wall in walls)
            {
                if (tigersSpawned >= tigerCount) break;

                Vector3 wallCenter = wall.position;
                if (Vector3.Distance(wallCenter, playerPos) < playerSafetyRadius) continue;
                if (IsTooCloseToOthers(wallCenter, occupiedPositions, minTrapSpacing)) continue;

                // Determine open corridor normal
                if (TryGetWallOpenNormal(wall, out Vector3 outwardNormal, out float wallThickness))
                {
                    // Exact middle of wall, pushed slightly outward flush against wall surface
                    Vector3 mountPos = wallCenter + outwardNormal * (wallThickness * 0.5f + 0.05f);
                    mountPos.y = tigerMountHeight;

                    // Tiger faces outward into corridor: LookRotation(outwardNormal) * Euler(-90, 0, 0)
                    Quaternion mountRot = Quaternion.LookRotation(outwardNormal, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f);

                    GameObject tigerInst = InstantiatePrefabOrClone(tigerPrefab, mountPos, mountRot, trapsRoot.transform);
                    tigerInst.name = $"Tiger_FireTrap_{tigersSpawned + 1}";

                    // Stagger spray timing slightly
                    var fireTrap = tigerInst.GetComponentInChildren<FireSprayTrap>(true);
                    if (fireTrap != null)
                    {
                        // Use serialized field or reflection for initial delay
                        float delay = (tigersSpawned % 4) * 0.5f;
                        SetInitialDelay(fireTrap, delay);
                    }

                    occupiedPositions.Add(wallCenter);
                    tigersSpawned++;
                }
            }

            // ==========================================
            // 2. SPAWN DOUBLE PILLOW TRAPS (PAIRED WITH GUARANTEED PASSAGE)
            // ==========================================
            foreach (var wall in walls)
            {
                if (doublePillowPairsSpawned >= doublePillowPairs) break;

                Vector3 wallCenter = wall.position;
                if (Vector3.Distance(wallCenter, playerPos) < playerSafetyRadius) continue;
                if (IsTooCloseToOthers(wallCenter, occupiedPositions, minTrapSpacing + 2f)) continue;

                if (TryGetWallOpenNormal(wall, out Vector3 outwardNormal, out _))
                {
                    Vector3 corridorDir = Vector3.Cross(Vector3.up, outwardNormal).normalized;
                    Vector3 corridorCenter = wallCenter + outwardNormal * 1.75f;
                    corridorCenter.y = 0f;

                    // Staggered chicane placement:
                    // Pillow A on left side of corridor at step -1.3m along corridor
                    // Pillow B on right side of corridor at step +1.3m along corridor
                    // This creates a guaranteed passage gap (doublePillowPassageClearance) for the player to weave through!
                    float sideOffset = Mathf.Max(0.5f, doublePillowPassageClearance * 0.5f);
                    float stepAlong = 1.35f;

                    Vector3 posA = corridorCenter - (corridorDir * stepAlong) - (outwardNormal * sideOffset);
                    Vector3 posB = corridorCenter + (corridorDir * stepAlong) + (outwardNormal * sideOffset);
                    posA.y = 0f;
                    posB.y = 0f;

                    // Spawn Pillow A (clockwise)
                    GameObject pillowA = InstantiatePrefabOrClone(pillowPrefab, posA, Quaternion.identity, trapsRoot.transform);
                    pillowA.name = $"Pillow_Pair_{doublePillowPairsSpawned + 1}_A";

                    // Spawn Pillow B (counter-clockwise)
                    GameObject pillowB = InstantiatePrefabOrClone(pillowPrefab, posB, Quaternion.identity, trapsRoot.transform);
                    pillowB.name = $"Pillow_Pair_{doublePillowPairsSpawned + 1}_B";
                    var trapB = pillowB.GetComponent<RotatingSpikeTrap>();
                    if (trapB != null)
                    {
                        SetReverseDirection(trapB, true);
                    }

                    occupiedPositions.Add(corridorCenter);
                    doublePillowPairsSpawned++;
                }
            }

            // ==========================================
            // 3. SPAWN SINGLE PILLOW TRAPS
            // ==========================================
            foreach (var wall in walls)
            {
                if (singlePillowsSpawned >= singlePillowCount) break;

                Vector3 wallCenter = wall.position;
                if (Vector3.Distance(wallCenter, playerPos) < playerSafetyRadius) continue;
                if (IsTooCloseToOthers(wallCenter, occupiedPositions, minTrapSpacing)) continue;

                if (TryGetWallOpenNormal(wall, out Vector3 outwardNormal, out _))
                {
                    // Place pillow in the corridor, offset slightly toward one side to leave a clear passing lane
                    Vector3 corridorPos = wallCenter + outwardNormal * 1.55f;
                    corridorPos.y = 0f;

                    GameObject pillowInst = InstantiatePrefabOrClone(pillowPrefab, corridorPos, Quaternion.identity, trapsRoot.transform);
                    pillowInst.name = $"Pillow_Single_{singlePillowsSpawned + 1}";

                    occupiedPositions.Add(corridorPos);
                    singlePillowsSpawned++;
                }
            }

            Debug.Log($"[MazeTrapSpawner] Spawned {tigersSpawned} Tigers, {singlePillowsSpawned} Single Pillows, and {doublePillowPairsSpawned * 2} Paired Pillows ({doublePillowPairsSpawned} pairs).");
        }

        private List<Transform> FindAllMazeWalls()
        {
            var result = new List<Transform>();
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var t in allTransforms)
            {
                if (t.name.Contains("Cube (1)"))
                {
                    result.Add(t);
                }
            }

            // Fallback: search under MazeBuild
            if (result.Count == 0)
            {
                var mazeBuild = GameObject.Find("MazeBuild");
                if (mazeBuild != null)
                {
                    var renderers = mazeBuild.GetComponentsInChildren<MeshRenderer>(true);
                    foreach (var r in renderers)
                    {
                        result.Add(r.transform);
                    }
                }
            }

            return result;
        }

        private bool TryGetWallOpenNormal(Transform wall, out Vector3 openNormal, out float thickness)
        {
            thickness = wall.lossyScale.z > 0.05f ? wall.lossyScale.z : 0.4f;

            // In local space, forward is the wall normal
            Vector3 norm1 = wall.forward;
            Vector3 norm2 = -wall.forward;

            Vector3 center = wall.position;
            center.y = 1.5f;

            // Check if norm1 has open space
            bool open1 = IsCorridorSpace(center + norm1 * 1.8f);
            bool open2 = IsCorridorSpace(center + norm2 * 1.8f);

            if (open1 && !open2)
            {
                openNormal = norm1;
                return true;
            }
            if (open2 && !open1)
            {
                openNormal = norm2;
                return true;
            }
            if (open1 && open2)
            {
                // Both sides open: choose randomly
                openNormal = Random.value > 0.5f ? norm1 : norm2;
                return true;
            }

            openNormal = norm1;
            return false;
        }

        private bool IsCorridorSpace(Vector3 point)
        {
            // Check if there is collision with walls at this point
            Collider[] hits = Physics.OverlapSphere(point, 0.7f);
            foreach (var hit in hits)
            {
                if (hit.name.Contains("Cube") || hit.name.Contains("Wall"))
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsTooCloseToOthers(Vector3 pos, List<Vector3> others, float minDistance)
        {
            foreach (var o in others)
            {
                if (Vector3.Distance(pos, o) < minDistance)
                {
                    return true;
                }
            }
            return false;
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int r = Random.Range(i, list.Count);
                T temp = list[i];
                list[i] = list[r];
                list[r] = temp;
            }
        }

        private GameObject InstantiatePrefabOrClone(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                instance.transform.position = pos;
                instance.transform.rotation = rot;
                Undo.RegisterCreatedObjectUndo(instance, "Spawn Trap");
                return instance;
            }
#endif
            return Instantiate(prefab, pos, rot, parent);
        }

        private void SetInitialDelay(FireSprayTrap trap, float delay)
        {
            var field = typeof(FireSprayTrap).GetField("initialDelay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(trap, delay);
            }
        }

        private void SetReverseDirection(RotatingSpikeTrap trap, bool reverse)
        {
            var field = typeof(RotatingSpikeTrap).GetField("reverseDirection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(trap, reverse);
            }
        }
    }
}
