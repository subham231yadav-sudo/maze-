using System;
using UnityEngine;

namespace MazeGenerator
{
    /// <summary>
    /// All configurable inputs for maze generation and post-processing.
    /// Generation always starts at (0,0); if inactive, a random active cell is used.
    /// </summary>
    [Serializable]
    public class MazeGenerationParams
    {
        /// <summary>
        /// Soft limit used for editor/runtime warnings (prefab builds get heavy beyond this).
        /// </summary>
        public const int RecommendedMaxCells = 64 * 64;

        [Header("Grid")]
        [Min(1)] public int Width = 15;
        [Min(1)] public int Height = 15;
        public int Seed = 12345;
        public MazeAlgorithmType Algorithm = MazeAlgorithmType.RecursiveBacktracker;

        [Header("Shape")]
        public MazeShape Shape = MazeShape.Rectangle;
        [Tooltip("Used when Shape is CustomMask. Non-black pixels = active cells. Size is remapped to Width x Height.")]
        public Texture2D CustomMaskTexture;

        [Header("Openings (entries/exits)")]
        [Min(0)] public int OpeningCount = 2;

        [Header("Braiding")]
        [Range(0f, 1f)] public float BraidFactor = 0f;

        [Header("Empty Rooms")]
        [Min(0)] public int RoomCount = 0;
        [Min(1)] public int RoomMinSize = 2;
        [Min(1)] public int RoomMaxSize = 4;

        [Header("Build")]
        [Tooltip("Parent spawned pieces under Chunk_x_y transforms (helps editor hierarchy and Clear).")]
        [Min(1)] public int BuildChunkSize = 16;
        [Tooltip("Mark spawned pieces as static for batching / occlusion (Play Mode).")]
        public bool MarkBuildStatic = true;

        // Filled automatically from the wall prefab bounds at Generate (not shown in inspector).
        public const float DefaultCellSize = 2f;
        public const float DefaultWallHeight = 2.5f;
        public const float DefaultWallThickness = 0.15f;

        [HideInInspector, Min(0.1f)] public float CellSize = DefaultCellSize;
        [HideInInspector, Min(0.1f)] public float WallHeight = DefaultWallHeight;
        [HideInInspector, Min(0.05f)] public float WallThickness = DefaultWallThickness;

        public int ApproximateCellCount => Width * Height;

        public void Clamp()
        {
            Width = Mathf.Max(1, Width);
            Height = Mathf.Max(1, Height);
            BraidFactor = Mathf.Clamp01(BraidFactor);
            OpeningCount = Mathf.Max(0, OpeningCount);
            RoomCount = Mathf.Max(0, RoomCount);
            RoomMinSize = Mathf.Max(1, RoomMinSize);
            RoomMaxSize = Mathf.Max(RoomMinSize, RoomMaxSize);
            BuildChunkSize = Mathf.Max(1, BuildChunkSize);
            CellSize = Mathf.Max(0.1f, CellSize);
            WallHeight = Mathf.Max(0.1f, WallHeight);
            WallThickness = Mathf.Max(0.05f, WallThickness);
        }

        public string Validate()
        {
            Clamp();
            if (Shape == MazeShape.CustomMask && CustomMaskTexture == null)
            {
                return "CustomMask shape requires a CustomMaskTexture.";
            }

            if (RoomCount > 0 && RoomMinSize > Mathf.Min(Width, Height))
            {
                return "RoomMinSize cannot exceed the smaller grid dimension.";
            }

            return null;
        }

        /// <summary>
        /// Non-blocking advice for large prefab builds. Null when within the soft limit.
        /// </summary>
        public string GetSizeWarning()
        {
            Clamp();
            if (ApproximateCellCount <= RecommendedMaxCells)
            {
                return null;
            }

            return
                $"Large maze ({Width}x{Height} ≈ {ApproximateCellCount} cells). " +
                $"Prefab builds above ~{RecommendedMaxCells} cells can slow or crash the editor. " +
                "Consider a smaller size or a thinner wall module.";
        }
    }
}
