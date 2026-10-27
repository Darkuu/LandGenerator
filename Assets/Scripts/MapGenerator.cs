using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, ColourMap, SlopeMap, Mesh }
    public enum WorldPreset { Custom, Island, Mountainous, ArchipelagoStyle }

    [Header("Display")]
    public MapDisplay display;
    public DrawMode drawMode = DrawMode.ColourMap;

    [Header("World Settings")]
    [Min(2)] public int mapWidth = 128;
    [Min(2)] public int mapHeight = 128;
    public int seed = 12345;
    public Vector2 offset;
    [Min(0.001f)] public float noiseScale = 45f;
    [Range(1, 12)] public int octaves = 5;
    [Range(0f, 1f)] public float persistence = 0.5f;
    [Min(1f)] public float lacunarity = 2f;

    [Header("Island Settings")]
    public bool useFalloff = true;
    [Range(0f, 2f)] public float falloffStrength = 1f;
    [Range(0.5f, 6f)] public float falloffExponent = 3f;

    [Header("3D Settings")]
    [Min(0f)] public float meshHeight = 20f;
    public AnimationCurve heightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Terrain Regions (sort by ascending height)")]
    public TerrainTypes[] regions =
    {
        new TerrainTypes { name = "Deep Water", height = 0.25f, colour = new Color(0.03f, 0.12f, 0.32f) },
        new TerrainTypes { name = "Shallow Water", height = 0.32f, colour = new Color(0.10f, 0.42f, 0.65f) },
        new TerrainTypes { name = "Beach", height = 0.38f, colour = new Color(0.85f, 0.76f, 0.50f) },
        new TerrainTypes { name = "Grassland", height = 0.58f, colour = new Color(0.25f, 0.55f, 0.20f) },
        new TerrainTypes { name = "Forest", height = 0.72f, colour = new Color(0.08f, 0.32f, 0.15f) },
        new TerrainTypes { name = "Rock", height = 0.88f, colour = new Color(0.42f, 0.40f, 0.36f) },
        new TerrainTypes { name = "Snow", height = 1f, colour = Color.white }
    };

    [Header("Optional UI - assign in Inspector")]
    public Slider mapWidthSlider;
    public Slider mapHeightSlider;
    public Slider noiseScaleSlider;
    public Slider octavesSlider;
    public Slider persistenceSlider;
    public Slider lacunaritySlider;
    public Slider offsetXSlider;
    public Slider offsetYSlider;
    public Slider falloffSlider;
    public Slider falloffExponentSlider;
    public Slider meshHeightSlider;
    public InputField seedInputField;
    public Toggle autoUpdateToggle;
    public Toggle useFalloffToggle;
    public Dropdown drawModeDropdown;
    public Dropdown presetDropdown;
    public Button generateButton;
    public Button randomSeedButton;
    public Button exportHeightmapButton;
    public Button exportMeshButton;

    [Header("Behaviour")]
    public bool autoUpdate;
    public WorldPreset preset = WorldPreset.Custom;

    float[,] lastHeightMap;
    bool suppressUIEvents;
    Camera previewCamera;
    Vector3 initialCameraPosition;
    Quaternion initialCameraRotation;
    float initialCameraSize;
    bool initialCameraOrthographic;
    bool cameraPoseCaptured;
    bool meshPreviewCreatedAtRuntime;

    void Start()
    {
        CaptureCameraPose();
        EnsureMeshPreview();
        SetupUI();
        GenerateMap();
    }

    void EnsureMeshPreview()
    {
        if (display == null) return;
        if (display.meshFilter == null)
        {
            GameObject meshObject = new GameObject("Terrain Mesh Preview");
            meshObject.transform.SetParent(transform, false);
            display.meshFilter = meshObject.AddComponent<MeshFilter>();
            display.meshRenderer = meshObject.AddComponent<MeshRenderer>();
            display.meshRenderer.sharedMaterial = display.terrainMaterial != null
                ? display.terrainMaterial
                : (display.textureRenderer != null ? display.textureRenderer.sharedMaterial : null);
            meshPreviewCreatedAtRuntime = true;
            meshObject.SetActive(false);
        }
        else if (display.meshRenderer == null)
        {
            display.meshRenderer = display.meshFilter.GetComponent<MeshRenderer>();
            if (display.meshRenderer == null)
                display.meshRenderer = display.meshFilter.gameObject.AddComponent<MeshRenderer>();
        }

        if ((meshPreviewCreatedAtRuntime || display.meshFilter.gameObject.name == "Terrain Mesh Preview") && display.textureRenderer != null)
        {
            Vector3 mapOrigin = display.textureRenderer.transform.position -
                display.textureRenderer.transform.right * (mapWidth * 0.5f) -
                display.textureRenderer.transform.forward * (mapHeight * 0.5f);
            display.meshFilter.transform.SetPositionAndRotation(mapOrigin, display.textureRenderer.transform.rotation);
            display.meshFilter.transform.localScale = Vector3.one;
        }
    }

    void CaptureCameraPose()
    {
        previewCamera = Camera.main;
        if (previewCamera == null) return;
        initialCameraPosition = previewCamera.transform.position;
        initialCameraRotation = previewCamera.transform.rotation;
        initialCameraOrthographic = previewCamera.orthographic;
        initialCameraSize = previewCamera.orthographicSize;
        cameraPoseCaptured = true;
    }

    void SetCameraForDrawMode()
    {
        if (previewCamera == null || !cameraPoseCaptured) return;
        if (drawMode != DrawMode.Mesh)
        {
            previewCamera.transform.SetPositionAndRotation(initialCameraPosition, initialCameraRotation);
            previewCamera.orthographic = initialCameraOrthographic;
            if (initialCameraOrthographic) previewCamera.orthographicSize = initialCameraSize;
            return;
        }

        Vector3 mapCenter = display != null && display.textureRenderer != null
            ? display.textureRenderer.transform.position
            : transform.position + new Vector3(mapWidth * 0.5f, 0f, mapHeight * 0.5f);
        mapCenter.y += meshHeight * 0.15f;
        float distance = Mathf.Max(mapWidth, mapHeight) * 1.4f;
        previewCamera.orthographic = true;
        previewCamera.orthographicSize = Mathf.Max(mapWidth, mapHeight) * 0.85f;
        previewCamera.transform.position = mapCenter + new Vector3(0f, distance, -distance);
        previewCamera.transform.LookAt(mapCenter);
    }

    void OnDestroy()
    {
        RemoveListeners();
    }

    void SetupUI()
    {
        suppressUIEvents = true;

        ConfigureSlider(mapWidthSlider, 32f, 1024f, mapWidth, true, OnMapWidthChanged);
        ConfigureSlider(mapHeightSlider, 32f, 1024f, mapHeight, true, OnMapHeightChanged);
        if (noiseScaleSlider != null)
        {
            noiseScaleSlider.minValue = 1f;
            noiseScaleSlider.maxValue = 250f;
            noiseScaleSlider.value = noiseScale;
            noiseScaleSlider.onValueChanged.AddListener(OnNoiseScaleChanged);
        }
        if (octavesSlider != null)
        {
            octavesSlider.minValue = 1;
            octavesSlider.maxValue = 12;
            octavesSlider.wholeNumbers = true;
            octavesSlider.value = octaves;
            octavesSlider.onValueChanged.AddListener(OnOctavesChanged);
        }
        if (persistenceSlider != null)
        {
            persistenceSlider.minValue = 0f;
            persistenceSlider.maxValue = 1f;
            persistenceSlider.value = persistence;
            persistenceSlider.onValueChanged.AddListener(OnPersistenceChanged);
        }
        if (lacunaritySlider != null)
        {
            lacunaritySlider.minValue = 1f;
            lacunaritySlider.maxValue = 4f;
            lacunaritySlider.value = lacunarity;
            lacunaritySlider.onValueChanged.AddListener(OnLacunarityChanged);
        }
        ConfigureSlider(offsetXSlider, -1000f, 1000f, offset.x, false, OnOffsetXChanged);
        ConfigureSlider(offsetYSlider, -1000f, 1000f, offset.y, false, OnOffsetYChanged);
        if (falloffSlider != null)
        {
            falloffSlider.minValue = 0f;
            falloffSlider.maxValue = 2f;
            falloffSlider.value = falloffStrength;
            falloffSlider.onValueChanged.AddListener(OnFalloffChanged);
        }
        ConfigureSlider(falloffExponentSlider, 0.5f, 6f, falloffExponent, false, OnFalloffExponentChanged);
        ConfigureSlider(meshHeightSlider, 0f, 100f, meshHeight, false, OnMeshHeightChanged);
        if (seedInputField != null)
        {
            seedInputField.text = seed.ToString();
            seedInputField.onEndEdit.AddListener(OnSeedChanged);
        }
        if (autoUpdateToggle != null)
        {
            autoUpdateToggle.isOn = autoUpdate;
            autoUpdateToggle.onValueChanged.AddListener(OnAutoUpdateChanged);
        }
        if (useFalloffToggle != null)
        {
            useFalloffToggle.isOn = useFalloff;
            useFalloffToggle.onValueChanged.AddListener(OnUseFalloffChanged);
        }
        if (drawModeDropdown != null)
        {
            drawModeDropdown.ClearOptions();
            drawModeDropdown.AddOptions(new System.Collections.Generic.List<string>(Enum.GetNames(typeof(DrawMode))));
            drawModeDropdown.value = (int)drawMode;
            drawModeDropdown.onValueChanged.AddListener(OnDrawModeChanged);
        }
        if (presetDropdown != null)
        {
            presetDropdown.ClearOptions();
            presetDropdown.AddOptions(new System.Collections.Generic.List<string>(Enum.GetNames(typeof(WorldPreset))));
            presetDropdown.value = (int)preset;
            presetDropdown.onValueChanged.AddListener(OnPresetChanged);
        }

        if (generateButton != null) generateButton.onClick.AddListener(GenerateMap);
        if (randomSeedButton != null) randomSeedButton.onClick.AddListener(RandomizeSeed);
        if (exportHeightmapButton != null) exportHeightmapButton.onClick.AddListener(ExportHeightmap);
        if (exportMeshButton != null) exportMeshButton.onClick.AddListener(ExportMesh);

        suppressUIEvents = false;
    }

    void ConfigureSlider(Slider slider, float min, float max, float value, bool wholeNumbers, UnityEngine.Events.UnityAction<float> listener)
    {
        if (slider == null) return;
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;
        slider.value = value;
        slider.onValueChanged.AddListener(listener);
    }

    void RemoveListeners()
    {
        if (mapWidthSlider != null) mapWidthSlider.onValueChanged.RemoveListener(OnMapWidthChanged);
        if (mapHeightSlider != null) mapHeightSlider.onValueChanged.RemoveListener(OnMapHeightChanged);
        if (noiseScaleSlider != null) noiseScaleSlider.onValueChanged.RemoveListener(OnNoiseScaleChanged);
        if (octavesSlider != null) octavesSlider.onValueChanged.RemoveListener(OnOctavesChanged);
        if (persistenceSlider != null) persistenceSlider.onValueChanged.RemoveListener(OnPersistenceChanged);
        if (lacunaritySlider != null) lacunaritySlider.onValueChanged.RemoveListener(OnLacunarityChanged);
        if (offsetXSlider != null) offsetXSlider.onValueChanged.RemoveListener(OnOffsetXChanged);
        if (offsetYSlider != null) offsetYSlider.onValueChanged.RemoveListener(OnOffsetYChanged);
        if (falloffSlider != null) falloffSlider.onValueChanged.RemoveListener(OnFalloffChanged);
        if (falloffExponentSlider != null) falloffExponentSlider.onValueChanged.RemoveListener(OnFalloffExponentChanged);
        if (meshHeightSlider != null) meshHeightSlider.onValueChanged.RemoveListener(OnMeshHeightChanged);
        if (seedInputField != null) seedInputField.onEndEdit.RemoveListener(OnSeedChanged);
        if (autoUpdateToggle != null) autoUpdateToggle.onValueChanged.RemoveListener(OnAutoUpdateChanged);
        if (useFalloffToggle != null) useFalloffToggle.onValueChanged.RemoveListener(OnUseFalloffChanged);
        if (drawModeDropdown != null) drawModeDropdown.onValueChanged.RemoveListener(OnDrawModeChanged);
        if (presetDropdown != null) presetDropdown.onValueChanged.RemoveListener(OnPresetChanged);
        if (generateButton != null) generateButton.onClick.RemoveListener(GenerateMap);
        if (randomSeedButton != null) randomSeedButton.onClick.RemoveListener(RandomizeSeed);
        if (exportHeightmapButton != null) exportHeightmapButton.onClick.RemoveListener(ExportHeightmap);
        if (exportMeshButton != null) exportMeshButton.onClick.RemoveListener(ExportMesh);
    }

    public void GenerateMap()
    {
        if (display == null)
        {
            Debug.LogError("MapGenerator: assign a MapDisplay in the Inspector.");
            return;
        }

        mapWidth = Mathf.Clamp(mapWidth, 2, 2048);
        mapHeight = Mathf.Clamp(mapHeight, 2, 2048);
        noiseScale = Mathf.Max(0.001f, noiseScale);
        octaves = Mathf.Clamp(octaves, 1, 12);
        lacunarity = Mathf.Max(1f, lacunarity);
        EnsureMeshPreview();
        SetCameraForDrawMode();

        float[,] map = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, noiseScale, octaves, persistence, lacunarity, offset);

        if (useFalloff)
        {
            float[,] falloff = FalloffGenerator.GenerateFalloffMap(mapWidth, mapHeight, falloffStrength, falloffExponent);
            for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
                map[x, y] = Mathf.Clamp01(map[x, y] - falloff[x, y]);
        }

        lastHeightMap = map;

        switch (drawMode)
        {
            case DrawMode.NoiseMap:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(map));
                break;
            case DrawMode.ColourMap:
                display.DrawTexture(TextureGenerator.TextureFromColourMap(BuildColourMap(map), mapWidth, mapHeight));
                break;
            case DrawMode.SlopeMap:
                display.DrawTexture(TextureGenerator.TextureFromColourMap(BuildSlopeMap(map), mapWidth, mapHeight));
                break;
            case DrawMode.Mesh:
                display.DrawMesh(map, meshHeight, heightCurve);
                break;
        }
    }

    void ApplyPresetSettings()
    {
        switch (preset)
        {
            case WorldPreset.Island:
                useFalloff = true;
                falloffStrength = 1f;
                falloffExponent = 3f;
                break;
            case WorldPreset.Mountainous:
                useFalloff = false;
                octaves = Mathf.Max(octaves, 6);
                persistence = Mathf.Max(persistence, 0.55f);
                break;
            case WorldPreset.ArchipelagoStyle:
                useFalloff = true;
                falloffStrength = 0.75f;
                falloffExponent = 2.2f;
                octaves = Mathf.Max(octaves, 5);
                break;
        }
    }

    Color[] BuildColourMap(float[,] map)
    {
        Color[] colours = new Color[mapWidth * mapHeight];
        for (int y = 0; y < mapHeight; y++)
        for (int x = 0; x < mapWidth; x++)
        {
            float value = map[x, y];
            Color colour = Color.magenta;
            bool found = false;

            if (regions != null)
            {
                for (int i = 0; i < regions.Length; i++)
                {
                    if (value <= regions[i].height)
                    {
                        colour = regions[i].colour;
                        found = true;
                        break;
                    }
                }
                if (!found && regions.Length > 0) colour = regions[regions.Length - 1].colour;
            }
            colours[y * mapWidth + x] = colour;
        }
        return colours;
    }

    Color[] BuildSlopeMap(float[,] map)
    {
        Color[] colours = new Color[mapWidth * mapHeight];
        for (int y = 0; y < mapHeight; y++)
        for (int x = 0; x < mapWidth; x++)
        {
            int left = Mathf.Max(0, x - 1);
            int right = Mathf.Min(mapWidth - 1, x + 1);
            int down = Mathf.Max(0, y - 1);
            int up = Mathf.Min(mapHeight - 1, y + 1);
            float dx = map[right, y] - map[left, y];
            float dy = map[x, up] - map[x, down];
            float slope = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) * 5f);
            colours[y * mapWidth + x] = Color.Lerp(Color.black, Color.white, slope);
        }
        return colours;
    }

    public void RandomizeSeed()
    {
        seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        if (seedInputField != null) seedInputField.text = seed.ToString();
        GenerateMap();
    }

    public void ExportHeightmap()
    {
        if (lastHeightMap == null) GenerateMap();
        if (lastHeightMap == null) return;

        Texture2D texture = TextureGenerator.TextureFromHeightMap(lastHeightMap);
        string path = Path.Combine(Application.persistentDataPath, "heightmap_" + seed + ".png");
        File.WriteAllBytes(path, texture.EncodeToPNG());
        if (Application.isPlaying) Destroy(texture); else DestroyImmediate(texture);
        Debug.Log("Heightmap exported to: " + path);
    }

    public void ExportMesh()
    {
        if (lastHeightMap == null) GenerateMap();
        if (lastHeightMap == null || display == null)
        {
            Debug.LogWarning("Generate a map before exporting the terrain mesh.");
            return;
        }

        EnsureMeshPreview();
        if (display.meshFilter == null) return;
        display.DrawMesh(lastHeightMap, meshHeight, heightCurve);
        if (display.meshFilter.sharedMesh == null) return;

        Mesh mesh = display.meshFilter.sharedMesh;
        string path = Path.Combine(Application.persistentDataPath, "terrain_" + seed + ".obj");
        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("# Procedural terrain mesh");
            foreach (Vector3 v in mesh.vertices) writer.WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "v {0} {1} {2}", v.x, v.y, v.z));
            foreach (Vector2 uv in mesh.uv) writer.WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "vt {0} {1}", uv.x, uv.y));
            foreach (Vector3 n in mesh.normals) writer.WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "vn {0} {1} {2}", n.x, n.y, n.z));
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i] + 1;
                int b = triangles[i + 1] + 1;
                int c = triangles[i + 2] + 1;
                writer.WriteLine($"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}");
            }
        }
        Debug.Log("Terrain mesh exported to: " + path);
    }

    void OnMapWidthChanged(float value) { mapWidth = Mathf.RoundToInt(value); RegenerateIfNeeded(); }
    void OnMapHeightChanged(float value) { mapHeight = Mathf.RoundToInt(value); RegenerateIfNeeded(); }
    void OnNoiseScaleChanged(float value) { noiseScale = value; RegenerateIfNeeded(); }
    void OnOctavesChanged(float value) { octaves = Mathf.RoundToInt(value); RegenerateIfNeeded(); }
    void OnPersistenceChanged(float value) { persistence = value; RegenerateIfNeeded(); }
    void OnLacunarityChanged(float value) { lacunarity = value; RegenerateIfNeeded(); }
    void OnOffsetXChanged(float value) { offset.x = value; RegenerateIfNeeded(); }
    void OnOffsetYChanged(float value) { offset.y = value; RegenerateIfNeeded(); }
    void OnFalloffChanged(float value) { falloffStrength = value; RegenerateIfNeeded(); }
    void OnFalloffExponentChanged(float value) { falloffExponent = value; RegenerateIfNeeded(); }
    void OnMeshHeightChanged(float value) { meshHeight = value; RegenerateIfNeeded(); }
    void OnSeedChanged(string value)
    {
        if (int.TryParse(value, out int parsed)) { seed = parsed; RegenerateIfNeeded(); }
        else if (seedInputField != null) seedInputField.text = seed.ToString();
    }
    void OnAutoUpdateChanged(bool value) { autoUpdate = value; if (autoUpdate) GenerateMap(); }
    void OnUseFalloffChanged(bool value) { useFalloff = value; RegenerateIfNeeded(); }
    void OnDrawModeChanged(int index) { drawMode = (DrawMode)index; GenerateMap(); }
    void OnPresetChanged(int index)
    {
        preset = (WorldPreset)index;
        ApplyPresetSettings();
        SyncSettingsControls();
        GenerateMap();
    }

    void SyncSettingsControls()
    {
        bool wasSuppressing = suppressUIEvents;
        suppressUIEvents = true;
        if (mapWidthSlider != null) mapWidthSlider.value = mapWidth;
        if (mapHeightSlider != null) mapHeightSlider.value = mapHeight;
        if (octavesSlider != null) octavesSlider.value = octaves;
        if (persistenceSlider != null) persistenceSlider.value = persistence;
        if (falloffSlider != null) falloffSlider.value = falloffStrength;
        if (falloffExponentSlider != null) falloffExponentSlider.value = falloffExponent;
        if (useFalloffToggle != null) useFalloffToggle.isOn = useFalloff;
        suppressUIEvents = wasSuppressing;
    }

    void RegenerateIfNeeded()
    {
        if (!suppressUIEvents && autoUpdate) GenerateMap();
    }

    void OnValidate()
    {
        mapWidth = Mathf.Clamp(mapWidth, 2, 2048);
        mapHeight = Mathf.Clamp(mapHeight, 2, 2048);
        noiseScale = Mathf.Max(0.001f, noiseScale);
        octaves = Mathf.Clamp(octaves, 1, 12);
        persistence = Mathf.Clamp01(persistence);
        lacunarity = Mathf.Max(1f, lacunarity);
        falloffStrength = Mathf.Clamp(falloffStrength, 0f, 2f);
        falloffExponent = Mathf.Clamp(falloffExponent, 0.5f, 6f);
        meshHeight = Mathf.Max(0f, meshHeight);
    }
}
