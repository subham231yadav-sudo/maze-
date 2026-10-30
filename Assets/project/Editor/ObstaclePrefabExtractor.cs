using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Project.Editor
{
    [InitializeOnLoad]
    public class ObstaclePrefabExtractorWindow : EditorWindow
    {
        private const string FbxPath = "Assets/project/obstacles/scenes/obstacles.fbx";
        private const string TexturesFolder = "Assets/project/obstacles/sourceimages";
        private const string DefaultOutputFolder = "Assets/project/obstacles/Prefabs";
        private const string MaterialPath = "Assets/project/obstacles/M_Obstacles.mat";

        private string outputFolder = DefaultOutputFolder;
        private bool assignMaterial = true;
        private bool addColliders = true;
        private bool centerPivotAtBase = true;
        private GameObject customSourceRoot;
        private Vector2 scrollPos;

        [MenuItem("Tools/Obstacles/Obstacle Prefab Extractor Window", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<ObstaclePrefabExtractorWindow>("Obstacle Extractor");
            window.minSize = new Vector2(360, 480);
            window.Show();
        }

        [MenuItem("Tools/Obstacles/Extract demo_scene to Prefabs (1-Click)", false, 1)]
        public static void QuickExtract()
        {
            ExtractObstacles(DefaultOutputFolder, true, true, true, null, true);
        }

        [MenuItem("Tools/Obstacles/Open Prefabs Folder", false, 2)]
        public static void OpenFolder()
        {
            if (!Directory.Exists(DefaultOutputFolder))
            {
                Directory.CreateDirectory(DefaultOutputFolder);
                AssetDatabase.Refresh();
            }

            var obj = AssetDatabase.LoadAssetAtPath<Object>(DefaultOutputFolder);
            if (obj != null)
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }
        }

        static ObstaclePrefabExtractorWindow()
        {
            EditorApplication.delayCall += RunAutoExtractionOnce;
        }

        private static void RunAutoExtractionOnce()
        {
            if (!Directory.Exists(DefaultOutputFolder) || Directory.GetFiles(DefaultOutputFolder, "*.prefab").Length == 0)
            {
                ExtractObstacles(DefaultOutputFolder, true, true, true, null, false);
            }
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Obstacle Prefab Extractor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Extracts all individual obstacle pieces from 'demo_scene' into separate, clean prefabs with URP materials, colliders, and ground-aligned pivots.",
                MessageType.Info
            );

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);

            customSourceRoot = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Source Object", "Leave empty to auto-detect 'demo_scene' in hierarchy or FBX"),
                customSourceRoot,
                typeof(GameObject),
                true
            );

            outputFolder = EditorGUILayout.TextField(new GUIContent("Output Folder"), outputFolder);

            GUILayout.Space(6);
            assignMaterial = EditorGUILayout.Toggle(new GUIContent("Apply URP Material", "Creates and applies M_Obstacles with PBR textures"), assignMaterial);
            addColliders = EditorGUILayout.Toggle(new GUIContent("Add Mesh Colliders", "Adds MeshCollider components to all obstacle meshes"), addColliders);
            centerPivotAtBase = EditorGUILayout.Toggle(new GUIContent("Center Pivot to Floor", "Aligns obstacle bottom to Y=0 for easy placement"), centerPivotAtBase);

            GUILayout.Space(12);

            // Preview detected source in scene
            GameObject sceneSource = FindDemoSceneInHierarchy(customSourceRoot);

            EditorGUILayout.LabelField("Detected Obstacles in demo_scene:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));
            if (sceneSource != null && sceneSource.transform.childCount > 0)
            {
                for (int i = 0; i < sceneSource.transform.childCount; i++)
                {
                    Transform child = sceneSource.transform.GetChild(i);
                    EditorGUILayout.LabelField($"• {child.name} ({child.childCount} parts)");
                }
            }
            else
            {
                EditorGUILayout.LabelField("• cylinder_low (Spiked rotating roller)");
                EditorGUILayout.LabelField("• floor_cone_low (Floor spike cones)");
                EditorGUILayout.LabelField("• hammer_swing_low (Pendulum swinging hammer)");
                EditorGUILayout.LabelField("• pillow_low (Spiked wooden pillars)");
                EditorGUILayout.LabelField("• rays_low (Rotating ray beams)");
                EditorGUILayout.LabelField("• sphere_low (Spiked mace / wrecking ball)");
                EditorGUILayout.LabelField("• waves_low (Horizontal roller wave bars)");
                EditorGUILayout.LabelField("• obstacles_low (Base obstacle props)");
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(12);
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("Extract All Obstacles into Prefabs", GUILayout.Height(36)))
            {
                ExtractObstacles(outputFolder, assignMaterial, addColliders, centerPivotAtBase, customSourceRoot, true);
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(6);
            if (GUILayout.Button("Open Output Folder in Project"))
            {
                OpenFolder();
            }
        }

        public static void ExtractObstacles(
            string targetFolder,
            bool applyMaterial,
            bool addCols,
            bool centerPivot,
            GameObject manualSource,
            bool showDialog)
        {
            GameObject tempRoot = null;
            try
            {
                EditorUtility.DisplayProgressBar("Obstacle Prefab Extractor", "Locating demo_scene...", 0.1f);

                GameObject demoSceneObj = ResolveSource(manualSource, out bool isTemporary);
                if (isTemporary)
                {
                    tempRoot = demoSceneObj;
                }

                if (demoSceneObj == null)
                {
                    Debug.LogError("[ObstaclePrefabExtractor] Could not find 'demo_scene' in hierarchy or FBX!");
                    if (showDialog)
                    {
                        EditorUtility.DisplayDialog("Error", "Could not find 'demo_scene'. Please ensure obstacles are in the scene or FBX is at " + FbxPath, "OK");
                    }
                    return;
                }

                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                    AssetDatabase.Refresh();
                }

                Material obstacleMat = null;
                if (applyMaterial)
                {
                    obstacleMat = GetOrCreateObstacleMaterial();
                }

                int childCount = demoSceneObj.transform.childCount;
                List<string> created = new List<string>();

                for (int i = 0; i < childCount; i++)
                {
                    Transform child = demoSceneObj.transform.GetChild(i);
                    float progress = 0.2f + (0.7f * (i / (float)childCount));
                    EditorUtility.DisplayProgressBar("Obstacle Prefab Extractor", $"Creating {child.name}.prefab...", progress);

                    string prefabName = child.name;
                    string targetPath = $"{targetFolder}/{prefabName}.prefab";

                    GameObject clone = Instantiate(child.gameObject);
                    clone.name = prefabName;
                    clone.transform.SetParent(null);
                    clone.transform.position = Vector3.zero;
                    clone.transform.rotation = Quaternion.identity;
                    clone.transform.localScale = Vector3.one;

                    if (applyMaterial && obstacleMat != null)
                    {
                        ApplyMaterialToRenderers(clone, obstacleMat);
                    }

                    if (addCols)
                    {
                        AddMeshColliders(clone);
                    }

                    if (centerPivot)
                    {
                        CenterPivotAtBase(clone);
                    }

                    // Save as clean prefab
                    GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(clone, targetPath);
                    DestroyImmediate(clone);

                    if (savedPrefab != null)
                    {
                        created.Add(prefabName);
                        Debug.Log($"[ObstaclePrefabExtractor] Created prefab: {targetPath}");
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                OpenFolder();

                Debug.Log($"[ObstaclePrefabExtractor] Successfully extracted {created.Count} obstacle prefabs into {targetFolder}!");

                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "Obstacles Extracted",
                        $"Successfully created {created.Count} prefabs in {targetFolder}:\n\n• " + string.Join("\n• ", created),
                        "Awesome!"
                    );
                }
            }
            finally
            {
                if (tempRoot != null)
                {
                    DestroyImmediate(tempRoot);
                }
                EditorUtility.ClearProgressBar();
            }
        }

        private static GameObject FindDemoSceneInHierarchy(GameObject manualSource = null)
        {
            if (manualSource != null)
            {
                if (manualSource.name == "demo_scene") return manualSource;
                var found = FindChildRecursive(manualSource.transform, "demo_scene");
                if (found != null) return found.gameObject;
                return manualSource;
            }

            // Check hierarchy selection
            if (Selection.activeGameObject != null)
            {
                if (Selection.activeGameObject.name == "demo_scene")
                {
                    return Selection.activeGameObject;
                }
                var found = FindChildRecursive(Selection.activeGameObject.transform, "demo_scene");
                if (found != null) return found.gameObject;
            }

            // Search active scene
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
            {
                if (root.name == "demo_scene") return root;
                var found = FindChildRecursive(root.transform, "demo_scene");
                if (found != null) return found.gameObject;
            }

            return null;
        }

        private static GameObject ResolveSource(GameObject manualSource, out bool isTemporary)
        {
            isTemporary = false;
            var foundInScene = FindDemoSceneInHierarchy(manualSource);
            if (foundInScene != null) return foundInScene;

            // Fallback to FBX
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (fbx != null)
            {
                var instance = Instantiate(fbx);
                isTemporary = true;
                var found = FindChildRecursive(instance.transform, "demo_scene");
                if (found != null) return found.gameObject;
                return instance;
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var result = FindChildRecursive(parent.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }

        private static Material GetOrCreateObstacleMaterial()
        {
            string normalPath = $"{TexturesFolder}/obstacles_low_lambert1_Normal.png";
            EnsureNormalMapImportSetting(normalPath);

            var existingMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existingMat != null)
            {
                ConfigureMaterialTextures(existingMat);
                return existingMat;
            }

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null) urpShader = Shader.Find("Standard");

            Material mat = new Material(urpShader);
            mat.name = "M_Obstacles";
            ConfigureMaterialTextures(mat);

            AssetDatabase.CreateAsset(mat, MaterialPath);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static void EnsureNormalMapImportSetting(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureMaterialTextures(Material mat)
        {
            var baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/obstacles_low_lambert1_BaseColor.png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/obstacles_low_lambert1_Normal.png");
            var metallic = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/obstacles_low_lambert1_Metallic.png");
            var roughness = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/obstacles_low_lambert1_Roughness.png");
            var emissive = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/obstacles_low_lambert1_Emissive.png");

            if (baseColor != null) mat.SetTexture("_BaseMap", baseColor);
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (metallic != null)
            {
                mat.SetTexture("_MetallicGlossMap", metallic);
                mat.SetFloat("_Metallic", 1.0f);
            }
            if (roughness != null)
            {
                mat.SetFloat("_Smoothness", 0.5f);
            }
            if (emissive != null)
            {
                mat.SetTexture("_EmissionMap", emissive);
                mat.SetColor("_EmissionColor", Color.white);
                mat.EnableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(mat);
        }

        private static void ApplyMaterialToRenderers(GameObject root, Material material)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers)
            {
                r.sharedMaterial = material;
            }
        }

        private static void AddMeshColliders(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers)
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    var col = r.GetComponent<Collider>();
                    if (col == null)
                    {
                        var meshCol = r.gameObject.AddComponent<MeshCollider>();
                        meshCol.sharedMesh = mf.sharedMesh;
                        meshCol.convex = false;
                    }
                }
            }
        }

        private static void CenterPivotAtBase(GameObject root)
        {
            if (root.name.ToLower().Contains("pillow"))
            {
                PillowFixer.AlignAndCleanHierarchy(root);
                return;
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 bottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

            for (int i = 0; i < root.transform.childCount; i++)
            {
                Transform child = root.transform.GetChild(i);
                child.position -= bottomCenter;
            }
        }
    }
}
