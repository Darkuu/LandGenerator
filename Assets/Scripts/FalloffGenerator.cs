using UnityEngine;

public static class FalloffGenerator
{
    public static float[,] GenerateFalloffMap(int width, int height, float strength = 1f, float exponent = 3f)
    {
        float[,] map = new float[width, height];
        strength = Mathf.Max(0f, strength);
        exponent = Mathf.Max(0.1f, exponent);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = x / (float)(width - 1) * 2f - 1f;
                float ny = y / (float)(height - 1) * 2f - 1f;
                float v = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));
                float baseFalloff = Mathf.Pow(v, exponent) /
                    (Mathf.Pow(v, exponent) + Mathf.Pow(2.2f - 2.2f * v, exponent));
                map[x, y] = Mathf.Clamp01(baseFalloff * strength);
            }
        }
        return map;
    }
}
