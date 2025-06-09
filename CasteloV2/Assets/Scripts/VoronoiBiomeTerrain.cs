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
                BiomeBlend blend = GetBlendedBiome(x, z);

                Biome bA = blend.biomeA;
                Biome bB = blend.biomeB;
                float t = blend.blendFactor;

                float nA = Mathf.PerlinNoise((x + bA.seed) * bA.noiseScale, (z + bA.seed) * bA.noiseScale);
                float nB = Mathf.PerlinNoise((x + bB.seed) * bB.noiseScale, (z + bB.seed) * bB.noiseScale);

                float height = Mathf.Lerp(nA * bA.heightMultiplier, nB * bB.heightMultiplier, t);
                Color color = Color.Lerp(bA.color, bB.color, t);

                vertices[i] = new Vector3(x * meshScale, height, z * meshScale);
                colors[i] = color;

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

    BiomeBlend GetBlendedBiome(int x, int z)
    {
        List<(Biome biome, float distance)> distances = new List<(Biome, float)>();

        Vector2 point = new Vector2(x, z);
        for (int i = 0; i < _biomeCenters.Length; i++)
        {
            float dist = Vector2.Distance(point, _biomeCenters[i]);
            distances.Add((_biomes[i], dist));
        }

        // Trier les distances pour garder les 2 plus proches
        distances.Sort((a, b) => a.distance.CompareTo(b.distance));
        float d0 = distances[0].distance;
        float d1 = distances[1].distance;

        float total = d0 + d1;
        float t = d0 / total; // poids entre les deux

        Biome b0 = distances[0].biome;
        Biome b1 = distances[1].biome;

        return new BiomeBlend
        {
            biomeA = b0,
            biomeB = b1,
            blendFactor = Mathf.Clamp01(1f - t) // entre 0 et 1
        };
    }

    class BiomeBlend
    {
        public Biome biomeA;
        public Biome biomeB;
        public float blendFactor; // 0 = biomeA, 1 = biomeB
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
