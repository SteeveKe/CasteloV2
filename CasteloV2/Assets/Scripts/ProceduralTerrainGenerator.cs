using System.Collections.Generic;
using UnityEngine;

public class ProceduralTerrainGenerator : MonoBehaviour
{
    [Header("Terrain Settings")]
    public Terrain terrain;
    public int terrainSize = 512;
    public int heightmapResolution = 513;
    public float heightMultiplier = 30f;

    [Header("Biome Settings")]
    public int biomeCount = 5;
    public int biomeSeed = 1234;
    public float biomeSize = 128f;
    public TerrainLayer[] biomeTextures;

    private Vector2[] biomeCenters;
    private Biome[] biomes;

    private void Start()
    {
        GenerateBiomes();
        GenerateTerrain();
    }

    void GenerateBiomes()
    {
        Random.InitState(biomeSeed);
        biomeCenters = new Vector2[biomeCount];
        biomes = new Biome[biomeCount];

        for (int i = 0; i < biomeCount; i++)
        {
            biomeCenters[i] = new Vector2(
                Random.Range(0, terrainSize),
                Random.Range(0, terrainSize)
            );

            biomes[i] = new Biome
            {
                name = $"Biome_{i}",
                noiseScale = Random.Range(0.01f, 0.05f),
                heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1),
                textureIndex = i % biomeTextures.Length
            };
        }
    }

    void GenerateTerrain()
    {
        TerrainData tData = new TerrainData();
        tData.heightmapResolution = heightmapResolution;
        tData.size = new Vector3(terrainSize, heightMultiplier, terrainSize);

        float[,] heights = new float[heightmapResolution, heightmapResolution];
        float[,,] alphaMap = new float[tData.alphamapResolution, tData.alphamapResolution, biomeTextures.Length];

        for (int z = 0; z < heightmapResolution; z++)
        {
            for (int x = 0; x < heightmapResolution; x++)
            {
                float worldX = ((float)x / (heightmapResolution - 1)) * terrainSize;
                float worldZ = ((float)z / (heightmapResolution - 1)) * terrainSize;

                // Biome interpolation
                (Biome bA, Biome bB, float t) = GetBlendedBiome(worldX, worldZ);

                float nA = Mathf.PerlinNoise(worldX * bA.noiseScale, worldZ * bA.noiseScale);
                float nB = Mathf.PerlinNoise(worldX * bB.noiseScale, worldZ * bB.noiseScale);
                float h = Mathf.Lerp(bA.heightCurve.Evaluate(nA), bB.heightCurve.Evaluate(nB), t);

                heights[z, x] = h;

                // Paint texture
                int ax = Mathf.RoundToInt((float)x / (heightmapResolution - 1) * (tData.alphamapResolution - 1));
                int az = Mathf.RoundToInt((float)z / (heightmapResolution - 1) * (tData.alphamapResolution - 1));
                alphaMap[az, ax, bA.textureIndex] += 1f - t;
                alphaMap[az, ax, bB.textureIndex] += t;
            }
        }

        NormalizeAlphaMap(alphaMap);

        tData.SetHeights(0, 0, heights);
        tData.terrainLayers = biomeTextures;
        tData.SetAlphamaps(0, 0, alphaMap);
        terrain.terrainData = tData;
    }

    (Biome, Biome, float) GetBlendedBiome(float x, float z)
    {
        List<(int index, float distance)> list = new();

        for (int i = 0; i < biomeCenters.Length; i++)
        {
            float d = Vector2.Distance(new Vector2(x, z), biomeCenters[i]);
            list.Add((i, d));
        }

        list.Sort((a, b) => a.distance.CompareTo(b.distance));
        float total = list[0].distance + list[1].distance;
        float t = list[1].distance / total;

        return (biomes[list[0].index], biomes[list[1].index], t);
    }

    void NormalizeAlphaMap(float[,,] alphaMap)
    {
        int width = alphaMap.GetLength(0);
        int height = alphaMap.GetLength(1);
        int layers = alphaMap.GetLength(2);

        for (int z = 0; z < width; z++)
        {
            for (int x = 0; x < height; x++)
            {
                float total = 0f;
                for (int l = 0; l < layers; l++)
                    total += alphaMap[z, x, l];

                for (int l = 0; l < layers; l++)
                    alphaMap[z, x, l] = alphaMap[z, x, l] / Mathf.Max(total, 0.0001f);
            }
        }
    }

    class Biome
    {
        public string name;
        public float noiseScale;
        public AnimationCurve heightCurve;
        public int textureIndex;
    }
}
