using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    [Header("2D Preview")]
    public Renderer textureRenderer;

    [Header("3D Mesh Preview")]
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public Material terrainMaterial;

    Mesh generatedMesh;
    Texture2D generatedTexture;

    public void DrawTexture(Texture2D texture)
    {
        if (texture == null || textureRenderer == null) return;

        if (generatedTexture != null && generatedTexture != texture)
            DestroyObjectSafe(generatedTexture);
        generatedTexture = texture;

        textureRenderer.gameObject.SetActive(true);
        if (meshFilter != null) meshFilter.gameObject.SetActive(false);
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1f, texture.height);
    }

    public void DrawMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve)
    {
        if (meshFilter == null)
        {
            Debug.LogWarning("MapDisplay: assign a MeshFilter for 3D mesh mode.");
            return;
        }

        if (textureRenderer != null) textureRenderer.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
        if (meshRenderer == null) meshRenderer = meshFilter.GetComponent<MeshRenderer>();
        if (meshRenderer != null && terrainMaterial != null) meshRenderer.sharedMaterial = terrainMaterial;

        if (generatedMesh != null) DestroyObjectSafe(generatedMesh);

        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        Vector3[] vertices = new Vector3[width * height];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[(width - 1) * (height - 1) * 6];

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int i = y * width + x;
            float h = heightCurve != null ? heightCurve.Evaluate(heightMap[x, y]) : heightMap[x, y];
            vertices[i] = new Vector3(x, h * heightMultiplier, y);
            uv[i] = new Vector2(x / (float)(width - 1), y / (float)(height - 1));
        }

        int t = 0;
        for (int y = 0; y < height - 1; y++)
        for (int x = 0; x < width - 1; x++)
        {
            int i = y * width + x;
            triangles[t++] = i;
            triangles[t++] = i + width;
            triangles[t++] = i + width + 1;
            triangles[t++] = i;
            triangles[t++] = i + width + 1;
            triangles[t++] = i + 1;
        }

        generatedMesh = new Mesh();
        generatedMesh.name = "Generated Terrain Mesh";
        if (vertices.Length > 65535) generatedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        generatedMesh.vertices = vertices;
        generatedMesh.triangles = triangles;
        generatedMesh.uv = uv;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();
        meshFilter.sharedMesh = generatedMesh;
    }

    static void DestroyObjectSafe(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Object.Destroy(obj);
        else Object.DestroyImmediate(obj);
    }

    void OnDestroy()
    {
        if (generatedTexture != null) DestroyObjectSafe(generatedTexture);
        if (generatedMesh != null) DestroyObjectSafe(generatedMesh);
    }
}
