using UnityEngine;

namespace MazeGenerator
{
    /// <summary>
    /// Scene component that generates and builds a maze from inspector parameters.
    /// Generation is explicit (inspector / Tools menu) — not automatic on Play.
    /// Geometry lives under auto children <c>MazeBuild</c> / <c>MazeDebug</c>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Maze Generator/Maze Generator")]
    public sealed class MazeGeneratorBehaviour : MonoBehaviour
    {
        const string BuildRootName = "MazeBuild";
        const string DebugRootName = "MazeDebug";

        [SerializeField] MazeGenerationParams parameters = new MazeGenerationParams();
        [SerializeField] MazePrefabConfig prefabs = new MazePrefabConfig();

        MazeGrid _grid;

        /// <summary>Editor-only solve debug message (not serialized).</summary>
        public string SolveDebugStatus { get; set; } = string.Empty;

        public MazeGenerationParams Parameters => parameters;
        public MazePrefabConfig Prefabs
        {
            get => prefabs;
            set => prefabs = value ?? new MazePrefabConfig();
        }

        public MazeGrid CurrentGrid => _grid;

        public void Generate()
        {
            var error = parameters.Validate();
            if (error != null)
            {
                Debug.LogError($"[MazeGenerator] {error}", this);
                return;
            }

            var prefabError = prefabs?.ValidateRequired();
            if (prefabError != null)
            {
                Debug.LogError($"[MazeGenerator] {prefabError}", this);
                return;
            }

            var sizeWarning = parameters.GetSizeWarning();
            if (sizeWarning != null)
            {
                Debug.LogWarning($"[MazeGenerator] {sizeWarning}", this);
            }

            ClearDebugMarkers();
            MazePrefabTransform.ApplyBuildMetricsToParams(prefabs, parameters);
            parameters.Clamp();
            _grid = MazeGeneratorService.Generate(parameters);
            new PrefabMazeBuilder(GetOrCreateChild(BuildRootName), prefabs, parameters).Build(_grid);
        }

        public void Clear()
        {
            ClearDebugMarkers();
            new PrefabMazeBuilder(GetOrCreateChild(BuildRootName), prefabs, parameters).Clear();
            _grid = null;
        }

        public void ReseedAndGenerate()
        {
            parameters.Seed = Random.Range(int.MinValue, int.MaxValue);
            Generate();
        }

        public void ClearDebugMarkers()
        {
            new MazeDebugMarkerBuilder(
                GetOrCreateChild(DebugRootName),
                prefabs,
                parameters.CellSize).Clear();
        }

        public MazeDebugMarkerBuilder CreateDebugMarkerBuilder()
        {
            return new MazeDebugMarkerBuilder(
                GetOrCreateChild(DebugRootName),
                prefabs,
                parameters.CellSize);
        }

        Transform GetOrCreateChild(string childName)
        {
            var existing = transform.Find(childName);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            return go.transform;
        }

        void OnValidate()
        {
            parameters?.Clamp();
            prefabs?.Validate();
        }
    }
}
