# Procedural Terrain Generator (Unity)

## Included scripts
- `MapGenerator.cs` — settings, UI wiring, generation, presets, export.
- `Noise.cs` — seeded multi-octave Perlin noise and normalisation.
- `FalloffGenerator.cs` — edge falloff for island-like maps.
- `TerrainTypes.cs` — serialisable elevation/colour bands.
- `TextureGenerator.cs` — texture creation from colours and heights.
- `MapDisplay.cs` — 2D texture preview and 3D mesh generation.

## Setup
The included `SampleScene` has the `MapGenerator`, 2D display, mesh preview, material, and UI references wired up. Its scrollable settings panel uses the existing control styles and includes map dimensions, noise settings, offsets, island falloff, presets, draw mode, seed, and export controls. Choose `Mesh` for the angled 3D preview.

For a new scene, add `MapGenerator` and `MapDisplay`, assign the display to `MapGenerator.display`, and assign a texture renderer to `MapDisplay.textureRenderer`. Add a `MeshFilter` and `MeshRenderer` to a `Terrain Mesh Preview` object, then assign them to `MapDisplay.meshFilter` and `MapDisplay.meshRenderer`. Assign a material to `MapDisplay.terrainMaterial` if needed. The generator can create the mesh preview at runtime when those mesh components are missing.

## Important notes
- Terrain region entries must be sorted by ascending `height` for the colour bands to work correctly.
- The island falloff is applied by subtracting a falloff field from the normalised noise map. It creates a centred, broadly square island shape; it is not a geological simulation.
- Exports are written to `Application.persistentDataPath`. Find the exact location in the Unity Console after exporting.
- Heightmap export creates a grayscale PNG. Mesh export creates an OBJ file.
- Mesh export requires `Mesh` draw mode to have generated a mesh first.
- This uses Unity's built-in `UnityEngine.UI` components, not TextMeshPro.
- For large maps, generation and mesh creation are synchronous and may stall the main thread. Start at 128x128 or 256x256 before increasing resolution.
