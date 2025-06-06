using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VoronoiBiomeTerrain : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int width = 100;
    public int height = 100;
    public float meshScale = 1f;

    [Header("Biome Settings")]
    public int biomeCount = 5;
    public float biomeNoiseScale = 0.1f;

    private List<Biome> _biomes = new List<Biome>();
    private Vector2[] _biomeCenters;

    void Start()
    {
        GenerateBiomes();
        GenerateMesh();
    }

    void GenerateBiomes()
    {
        _biomeCenters = new Vector2[biomeCount];
        for (int i = 0; i < biomeCount; i++)
        {
            Vector2 center = new Vector2(Random.Range(0, width), Random.Range(0, height));
            _biomeCenters[i] = center;

            Biome b = new Biome();
            b.name = "Biome " + i;
            b.color = Random.ColorHSV();
            b.noiseScale = Random.Range(0.02f, 0.1f);
            b.heightMultiplier = Random.Range(2f, 2f * biomeNoiseScale);
            b.seed = Random.Range(0, 9999);
            _biomes.Add(b);
        }
    }

    void GenerateMesh()
    {
        Vector3[] vertices = new Vector3[(width + 1) * (height + 1)];
        Color[] colors = new Color[vertices.Length];
        int[] triangles = new int[width * height * 6];

        for (int z = 0, i = 0; z <= height; z++)
        {
            for (int x = 0; x <= width; x++, i++)
            {
                Biome biome = GetClosestBiome(x, z);
                float nx = (x + biome.seed) * biome.noiseScale;
                float nz = (z + biome.seed) * biome.noiseScale;
                float y = Mathf.PerlinNoise(nx, nz) * biome.heightMultiplier;
                vertices[i] = new Vector3(x * meshScale, y, z * meshScale);
                colors[i] = biome.color;
            }
        }

        int vert = 0;
        int tris = 0;
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + width + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + width + 1;
                triangles[tris + 5] = vert + width + 2;
                vert++;
                tris += 6;
            }
            vert++;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
    }

    Biome GetClosestBiome(int x, int z)
    {
        float minDist = float.MaxValue;
        int closestIndex = 0;
        for (int i = 0; i < _biomeCenters.Length; i++)
        {
            float dist = Vector2.Distance(new Vector2(x, z), _biomeCenters[i]);
            if (dist < minDist)
            {
                minDist = dist;
                closestIndex = i;
            }
        }
        return _biomes[closestIndex];
    }

    class Biome
    {
        public string name;
        public Color color;
        public float noiseScale;
        public float heightMultiplier;
        public int seed;
    }
}
