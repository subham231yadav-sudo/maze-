using System.Collections.Generic;
using UnityEngine;

namespace MazeGenerator
{
    /// <summary>
    /// Spawns solution debug markers in the clear space between walls.
    /// Ribbon: one marker per logical cell. Block: rooms + passage voxels between them.
    /// </summary>
    public sealed class MazeDebugMarkerBuilder
    {
        readonly Transform _root;
        readonly MazePrefabConfig _prefabs;
        readonly float _cellSize;

        MazeWallPrefabMetrics _primaryMetrics;
        MazeWallPrefabMetrics _debugMetrics;
        Vector3 _clearEnvelope;
        bool _metricsReady;

        public MazeDebugMarkerBuilder(
            Transform root,
            MazePrefabConfig prefabs,
            float cellSize)
        {
            _root = root;
            _prefabs = prefabs;
            _cellSize = cellSize;
        }

        public void Clear()
        {
            MazeEditorSafeDestroy.DestroyChildren(_root);
        }

        /// <summary>
        /// Spawns markers along a full path (fills block-mode corridor voxels too).
        /// </summary>
        public void SpawnSolutionPath(IReadOnlyList<Vector2Int> path)
        {
            if (path == null || path.Count == 0)
            {
                return;
            }

            if (!EnsureMetrics())
            {
                return;
            }

            if (_primaryMetrics.IsElongated)
            {
                for (var i = 0; i < path.Count; i++)
                {
                    SpawnAtLogicalCell(path[i]);
                }

                return;
            }

            for (var i = 0; i < path.Count; i++)
            {
                SpawnAtBlockRoom(path[i]);
                if (i + 1 < path.Count)
                {
                    SpawnAtBlockPassage(path[i], path[i + 1]);
                }
            }
        }

        public void SpawnSolution(Vector2Int cell)
        {
            if (!EnsureMetrics())
            {
                return;
            }

            if (_primaryMetrics.IsElongated)
            {
                SpawnAtLogicalCell(cell);
                return;
            }

            SpawnAtBlockRoom(cell);
        }

        void SpawnAtLogicalCell(Vector2Int cell)
        {
            SpawnMarker(
                $"Solution_{cell.x}_{cell.y}",
                new Vector3(
                    cell.x * _cellSize + _cellSize * 0.5f,
                    0f,
                    cell.y * _cellSize + _cellSize * 0.5f));
        }

        void SpawnAtBlockRoom(Vector2Int cell)
        {
            var s = _primaryMetrics.ModuleSize;
            var vx = cell.x * 2 + 1;
            var vz = cell.y * 2 + 1;
            SpawnMarker($"Solution_{vx}_{vz}", new Vector3(vx * s, 0f, vz * s));
        }

        void SpawnAtBlockPassage(Vector2Int a, Vector2Int b)
        {
            // Mid voxel between adjacent rooms on the (2W+1) lattice.
            var s = _primaryMetrics.ModuleSize;
            var vx = a.x + b.x + 1;
            var vz = a.y + b.y + 1;
            SpawnMarker($"Solution_{vx}_{vz}", new Vector3(vx * s, 0f, vz * s));
        }

        void SpawnMarker(string name, Vector3 center)
        {
            if (_prefabs == null || _prefabs.DebugSolutionPrefab == null || _root == null)
            {
                return;
            }

            var existing = _root.Find(name);
            if (existing != null)
            {
                return;
            }

            var prefab = _prefabs.DebugSolutionPrefab;
            var instance = Object.Instantiate(prefab, _root);
            instance.name = name;

            MazePrefabTransform.ApplyFitTransform(
                instance.transform,
                prefab.transform,
                Quaternion.identity,
                _debugMetrics,
                _clearEnvelope);

            MazePrefabTransform.PlaceCentered(instance.transform, center);
        }

        bool EnsureMetrics()
        {
            if (_metricsReady)
            {
                return true;
            }

            var primary = MazePrefabTransform.ResolvePrimaryWallPrefab(_prefabs);
            if (!MazePrefabTransform.TryMeasureWall(primary, out _primaryMetrics))
            {
                return false;
            }

            if (!MazePrefabTransform.TryMeasureWall(_prefabs.DebugSolutionPrefab, out _debugMetrics))
            {
                return false;
            }

            _clearEnvelope = MazePrefabTransform.ClearCellEnvelope(_primaryMetrics, _cellSize);
            _metricsReady = true;
            return true;
        }
    }
}
