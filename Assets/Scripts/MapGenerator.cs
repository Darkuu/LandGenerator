using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    {
        Noisemap, ColourMap
    };

    // Sliders and UI Elements
    [Header("Sliders")]
    
    [Header("Detailed Settings")]
    public Slider noiseScaleSlider;
    public Slider octavesSlider;
    public Slider persistanceSlider;
    public Slider lacunaritySlider;
    [Header("Seed")]
    public InputField seedInputField;
    [Header("Auto Update")]
    public Toggle autoUpdateToggle;
    [Header("Drawmode")]
    public Dropdown drawModeDropdown;
    public DrawMode drawMode;
    [Header("Generate Button ")]
    public Button generateButton; 

    [Header("Current Values")]
    
    // Map settings
    public int mapWidth = 100;
    public int mapHeight = 100;
    public float noiseScale;
    public int octaves;
    [Range(0, 1)] public float persistance;
    public float lacunarity;
    public int seed;
    public Vector2 offset;
    public bool autoUpdate;

    public TerrainTypes[] regions;

    public void Start()
    {
        drawModeDropdown.options.Clear(); 
        drawModeDropdown.options.Add(new Dropdown.OptionData(DrawMode.Noisemap.ToString()));  
        drawModeDropdown.options.Add(new Dropdown.OptionData(DrawMode.ColourMap.ToString()));
        
        drawModeDropdown.value = (int)drawMode;
        drawModeDropdown.onValueChanged.AddListener(OnDrawModeChanged);
        
        // Setup sliders
        noiseScaleSlider.minValue = 1f;
        noiseScaleSlider.maxValue = 500f;
        noiseScaleSlider.value = noiseScale;

        octavesSlider.minValue = 1;
        octavesSlider.maxValue = 3;
        octavesSlider.wholeNumbers = false;
        octavesSlider.value = octaves;

        persistanceSlider.minValue = 0f;
        persistanceSlider.maxValue = 1f;
        persistanceSlider.value = persistance;

        lacunaritySlider.minValue = 1f;
        lacunaritySlider.maxValue = 10f;
        lacunaritySlider.value = lacunarity;

        seedInputField.text = seed.ToString();

        autoUpdateToggle.isOn = autoUpdate;

        // Initialize Dropdown
        drawModeDropdown.options.Clear();
        drawModeDropdown.options.Add(new Dropdown.OptionData(DrawMode.Noisemap.ToString()));
        drawModeDropdown.options.Add(new Dropdown.OptionData(DrawMode.ColourMap.ToString()));
        drawModeDropdown.value = (int)drawMode;

        // Add listener for Generate Button
        generateButton.onClick.AddListener(GenerateMap);

        // Add listeners for UI interaction
        noiseScaleSlider.onValueChanged.AddListener(OnNoiseScaleChanged);
        octavesSlider.onValueChanged.AddListener(OnOctavesChanged);
        persistanceSlider.onValueChanged.AddListener(OnPersistanceChanged);
        lacunaritySlider.onValueChanged.AddListener(OnLacunarityChanged);
        seedInputField.onEndEdit.AddListener(OnSeedChanged);
        autoUpdateToggle.onValueChanged.AddListener(OnAutoUpdateChanged);
        drawModeDropdown.onValueChanged.AddListener(OnDrawModeChanged);
    }

    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, noiseScale, octaves, persistance, lacunarity, offset);

        Color[] colourMap = new Color[mapWidth * mapHeight];
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colourMap[y * mapHeight + x] = regions[i].colour;
                        break;
                    }
                }
            }
        }

        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.Noisemap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        }
        else if (drawMode == DrawMode.ColourMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColourMap(colourMap, mapWidth, mapHeight));
        }
    }
    

    void OnNoiseScaleChanged(float value)
    {
        noiseScale = value;
        if (autoUpdate) GenerateMap();
    }

    void OnOctavesChanged(float value)
    {
        octaves = Mathf.RoundToInt(value);
        if (autoUpdate) GenerateMap();
    }

    void OnPersistanceChanged(float value)
    {
        persistance = value;
        if (autoUpdate) GenerateMap();
    }

    void OnLacunarityChanged(float value)
    {
        lacunarity = value;
        if (autoUpdate) GenerateMap();
    }

    void OnSeedChanged(string value)
    {
        if (int.TryParse(value, out int newSeed))
        {
            seed = newSeed;
            if (autoUpdate) GenerateMap();
        }
    }
    
    void OnAutoUpdateChanged(bool value)
    {
        autoUpdate = value;
        if (autoUpdate) GenerateMap();  
    }

    void OnDrawModeChanged(int index)
    {
        drawMode = (DrawMode)index; 
        if (autoUpdate)
        {
            GenerateMap();
        }
    }


    // Validation to prevent invalid values
    void OnValidate()
    {
        if (mapWidth < 1) mapWidth = 1;
        if (mapHeight < 1) mapHeight = 1;
        if (lacunarity < 1) lacunarity = 1;
        if (octaves < 0) octaves = 0;
    }
}

[System.Serializable]
public struct TerrainTypes
{
    public string name;
    public float height;
    public Color colour;
}