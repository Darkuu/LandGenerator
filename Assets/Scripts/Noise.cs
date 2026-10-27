using System;
using UnityEngine;

public static class Noise
{
    public static float[,] GenerateNoiseMap(
        int width, int height, int seed, float scale,
        int octaves, float persistence, float lacunarity, Vector2 offset)
    {
        width = Mathf.Max(2, width);
        height = Mathf.Max(2, height);
        scale = Mathf.Max(0.0001f, scale);
        octaves = Mathf.Clamp(octaves, 1, 12);
        persistence = Mathf.Clamp01(persistence);
        lacunarity = Mathf.Max(1f, lacunarity);

        System.Random random = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            octaveOffsets[i] = new Vector2(
                random.Next(-100000, 100000) + offset.x,
                random.Next(-100000, 100000) + offset.y);
        }

        float[,] map = new float[width, height];
        float min = float.MaxValue;
        float max = float.MinValue;
        float halfWidth = (width - 1) / 2f;
        float halfHeight = (height - 1) / 2f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float value = 0f;

                for (int octave = 0; octave < octaves; octave++)
                {
                    float sampleX = ((x - halfWidth) / scale) * frequency + octaveOffsets[octave].x;
                    float sampleY = ((y - halfHeight) / scale) * frequency + octaveOffsets[octave].y;
                    float perlin = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
                    value += perlin * amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                map[x, y] = value;
                if (value < min) min = value;
                if (value > max) max = value;
            }
        }

        float range = Mathf.Max(0.0001f, max - min);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                map[x, y] = Mathf.Clamp01((map[x, y] - min) / range);

        return map;
    }
}
