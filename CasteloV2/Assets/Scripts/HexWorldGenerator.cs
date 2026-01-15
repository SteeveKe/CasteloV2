using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class HexWorldGenerator : MonoBehaviour
{
    [Header("Réglages Visuels")]
    public Material terrainMaterial; // Utilise un shader "Particles/Standard Surface" !
    public bool autoUpdate = true;

    [Header("Dimensions")]
    [Range(10, 200)] public int mapWidth = 50; // On peut monter beaucoup plus haut maintenant !
    [Range(10, 200)] public int mapHeight = 50;

    [Header("Génération")]
    public float scale = 0.1f;
    public float heightMultiplier = 10f;
    public AnimationCurve heightCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public int seaLevel = 3;

    [Header("Humidité & Rivières")]
    public float moistureScale = 0.1f;
    public float moistureOffset = 500f;
    public float riverFrequency = 0.06f;
    [Range(0f, 0.2f)] public float riverWidth = 0.07f;

    // --- LISTES POUR LE MESH UNIQUE ---
    // Au lieu de créer des objets, on stocke tout ici
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Color> colors = new List<Color>();
    private List<Vector3> normals = new List<Vector3>();

    private Mesh globalMesh;
    private const float outerRadius = 1f;
    private const float height = 1f;

    void OnValidate()
    {
        if (autoUpdate)
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += GenerateWorld;
            #endif
        }
    }

    void Start()
    {
        GenerateWorld();
    }

    public void GenerateWorld()
    {
        if (this == null) return;

        // 1. Nettoyage et Préparation
        if (terrainMaterial == null) { Debug.LogError("Il faut le Material 'BiomeMat' !"); return; }
        
        // On détruit l'ancien mesh holder s'il existe
        Transform oldHolder = transform.Find("WorldMesh");
        if (oldHolder != null)
        {
             if (Application.isPlaying) Destroy(oldHolder.gameObject);
             else DestroyImmediate(oldHolder.gameObject);
        }

        // 2. On vide les listes (Reboot)
        vertices.Clear();
        triangles.Clear();
        colors.Clear();
        normals.Clear(); // On peut aussi laisser Unity recalculer, mais on vide par sécurité

        float seed = 0;
        float xOffset = 1.732f;
        float zOffset = 1.5f;

        // 3. BOUCLE DE GÉNÉRATION DES DONNÉES
        for (int x = 0; x < mapWidth; x++)
        {
            for (int z = 0; z < mapHeight; z++)
            {
                float xPos = x * xOffset;
                if (z % 2 == 1) xPos += xOffset / 2f;
                float zPos = z * zOffset;

                // Calculs des Bruits (Identique à avant)
                float rawH = Mathf.PerlinNoise((x + seed) * scale, (z + seed) * scale);
                float adjH = heightCurve.Evaluate(rawH);
                int finalY = Mathf.FloorToInt(adjH * heightMultiplier);

                float moist = Mathf.PerlinNoise((x + seed + moistureOffset) * moistureScale, (z + seed + moistureOffset) * moistureScale);
                float rNoise = Mathf.PerlinNoise((x + seed) * riverFrequency, (z + seed) * riverFrequency);
                bool isRiver = (Mathf.Abs(rNoise - 0.5f) < riverWidth) && rawH < 0.6f;

                if (isRiver && finalY >= seaLevel) finalY = seaLevel - 1;

                // --- CONSTRUCTION DU MESH ---
                // Au lieu de "CreateObject", on "AddHexData"
                for (int y = -1; y <= finalY; y++)
                {
                    Color c = GetBiomeColor(y, finalY, moist, rawH, isRiver);
                    AddHexToLists(new Vector3(xPos, y, zPos), c);
                }

                if (finalY < seaLevel)
                {
                    Color waterColor = new Color(0, 0.4f, 1f, 0.6f);
                    for (int y = finalY + 1; y <= seaLevel; y++)
                    {
                        AddHexToLists(new Vector3(xPos, y, zPos), waterColor);
                    }
                }
            }
        }

        // 4. CRÉATION DE L'OBJET UNIQUE
        CreateSingleMeshObject();
    }

    void AddHexToLists(Vector3 pos, Color color)
    {
        // On calcule les 6 coins relatifs au centre de l'hexagone
        // Astuce : On ne recalcule pas Sin/Cos à chaque fois, on pourrait optimiser, mais c'est déjà rapide.
        Vector3[] tops = new Vector3[6];
        Vector3[] bots = new Vector3[6];
        
        for (int i = 0; i < 6; i++)
        {
            float rad = Mathf.PI / 180f * (60 * i - 30);
            float x = Mathf.Cos(rad) * outerRadius;
            float z = Mathf.Sin(rad) * outerRadius;
            tops[i] = pos + new Vector3(x, height, z); // On ajoute 'pos' ici !
            bots[i] = pos + new Vector3(x, 0, z);
        }
        Vector3 centerTop = pos + new Vector3(0, height, 0);

        int startIndex = vertices.Count; // Important pour les triangles

        // --- FACE DU HAUT (6 Triangles) ---
        for (int i = 0; i < 6; i++)
        {
            vertices.Add(centerTop);
            vertices.Add(tops[(i + 1) % 6]);
            vertices.Add(tops[i]);

            // Ajout de la couleur pour ces 3 sommets
            colors.Add(color); colors.Add(color); colors.Add(color);

            // Normale vers le haut
            normals.Add(Vector3.up); normals.Add(Vector3.up); normals.Add(Vector3.up);

            triangles.Add(startIndex + 0);
            triangles.Add(startIndex + 1);
            triangles.Add(startIndex + 2);
            startIndex += 3;
        }

        // --- FACES DES CÔTÉS (12 Triangles) ---
        for (int i = 0; i < 6; i++)
        {
            Vector3 cT1 = tops[i]; Vector3 cT2 = tops[(i + 1) % 6];
            Vector3 cB1 = bots[i]; Vector3 cB2 = bots[(i + 1) % 6];

            // On ajoute les 4 points du quad (2 triangles)
            // Triangle 1
            vertices.Add(cT1); vertices.Add(cB2); vertices.Add(cB1);
            colors.Add(color); colors.Add(color); colors.Add(color);
            // Pour les normales des côtés, c'est plus joli de laisser Unity recalculer à la fin
            // ou de mettre des normales horizontales. Pour simplifier ici, on met Vector3.up temporairement
            normals.Add(Vector3.up); normals.Add(Vector3.up); normals.Add(Vector3.up); 
            
            triangles.Add(startIndex); triangles.Add(startIndex + 1); triangles.Add(startIndex + 2);
            startIndex += 3;

            // Triangle 2
            vertices.Add(cT1); vertices.Add(cT2); vertices.Add(cB2);
            colors.Add(color); colors.Add(color); colors.Add(color);
            normals.Add(Vector3.up); normals.Add(Vector3.up); normals.Add(Vector3.up);

            triangles.Add(startIndex); triangles.Add(startIndex + 1); triangles.Add(startIndex + 2);
            startIndex += 3;
        }
    }

    void CreateSingleMeshObject()
    {
        GameObject worldObj = new GameObject("WorldMesh");
        worldObj.transform.parent = this.transform;
        worldObj.transform.localPosition = Vector3.zero;

        MeshFilter mf = worldObj.AddComponent<MeshFilter>();
        MeshRenderer mr = worldObj.AddComponent<MeshRenderer>();

        globalMesh = new Mesh();
        
        // --- OPTIMISATION TRÈS IMPORTANTE ---
        // Par défaut, un Mesh est limité à 65000 sommets.
        // Avec cette ligne, on passe à 4 milliards (nécessaire pour les grandes cartes)
        globalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        globalMesh.SetVertices(vertices);
        globalMesh.SetTriangles(triangles, 0);
        globalMesh.SetColors(colors); // On injecte les couleurs calculées
        
        globalMesh.RecalculateNormals(); // Calcul auto des lumières
        globalMesh.RecalculateBounds();

        mf.mesh = globalMesh;
        mr.material = terrainMaterial;
        
        // Active les ombres
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;
    }

    Color GetBiomeColor(int y, int maxY, float moisture, float heightRaw, bool isRiver)
    {
        // (Logique identique à avant)
        if (y < maxY) return new Color(0.3f, 0.2f, 0.1f); 
        if (isRiver) return new Color(0.4f, 0.4f, 0.4f); 
        if (y <= seaLevel + 1) return new Color(0.95f, 0.85f, 0.5f); 
        if (heightRaw > 0.8f) return Color.white; 
        else if (heightRaw > 0.6f) return new Color(0.5f, 0.5f, 0.5f); 
        if (moisture < 0.3f) return new Color(0.8f, 0.7f, 0.4f); 
        else if (moisture < 0.6f) return new Color(0.4f, 0.8f, 0.2f); 
        else return new Color(0.1f, 0.6f, 0.1f); 
    }
}