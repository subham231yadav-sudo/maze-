# Maze Generator

Procedural **3D maze toolkit** for Unity 6 (URP).

## Quick Start

1. Open the project in **Unity 6000.3+** (URP).
2. Open `Assets/MazeGenerator/Samples/Scenes/MazeDemo.unity`.
3. Select **MazeGenerator** → **Generate Maze** in the inspector.

Wall prefabs and the debug marker are already assigned on the drop-in prefab.

## Features

- Algorithms: Recursive Backtracker, Prim, Eller
- Seed, size, shape (rectangle / circle / custom mask)
- Openings, braiding, empty rooms
- Wall prefabs: primary + up to 2 secondaries (%, fitted to primary size)
- Build: chunks, static flags; soft warning above ~64×64
- Editor: Generate / Reseed / Clear / Solve debug (A*)

## Runtime API

```csharp
var parameters = new MazeGenerationParams
{
    Width = 25,
    Height = 25,
    Seed = 123,
    Algorithm = MazeAlgorithmType.Prim,
    OpeningCount = 2
};

var prefabs = new MazePrefabConfig
{
    WallPrefab = wall,
    SecondaryWallPrefab1 = secondary,
    WallPercent = 75,
    SecondaryWall1Percent = 25,
    DebugSolutionPrefab = debugMarker
};

// Required before PrefabMazeBuilder: fills CellSize from the primary wall.
MazePrefabTransform.ApplyBuildMetricsToParams(prefabs, parameters);

var grid = MazeGeneratorService.Generate(parameters);
new PrefabMazeBuilder(buildRoot, prefabs, parameters).Build(grid);
```

Or use the `MazeGeneratorBehaviour` component (inspector handles metrics for you).

## Folder layout

```
Assets/MazeGenerator/
  MazeGenerator.prefab   # drop-in
  Runtime/               # API
  Editor/                # inspector tools
  Samples/               # demo scene + sample prefabs
  Documentation~/        # checklist publisher
  THIRD_PARTY.md
```

## Notes

- Primary wall defines maze spacing; secondaries and debug markers adapt to it.
- Custom masks need a readable `Texture2D` (Read/Write enabled).
- Sample art: see `THIRD_PARTY.md`.
- On the Unity Asset Store, usage is governed by the Asset Store EULA.