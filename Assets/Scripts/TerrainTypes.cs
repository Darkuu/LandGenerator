using System;
using UnityEngine;

[Serializable]
public struct TerrainTypes
{
    public string name;
    [Range(0f, 1f)] public float height;
    public Color colour;
}
