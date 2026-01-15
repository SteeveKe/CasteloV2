using UnityEngine;
using System.Collections.Generic;

public class HexWorldGenerator : MonoBehaviour
{
    [Header("Réglages Visuels")]
    // ICI : Tu vas glisser ton matériau créé manuellement
    public Material terrainMaterial; 

    [Header("Paramètres de la Carte")]
    public int mapWidth = 30;
    public int mapHeight = 30;

    [Header("Paramètres du Bruit")]
    public float noiseScale = 0.1f;
    public float heightMultiplier = 6f;
    public int seaLevel = 2;

    [Header("Paramètres des Rivières")]
    public float riverFrequency = 0.06f;
    [Range(0f, 0.2f)] public float riverWidth = 0.07f;

    private Mesh hexMesh; 

    void Start()
    {
        // Sécurité : Si tu as oublié de mettre le matériau, on met une erreur
        if (terrainMaterial == null)
        {
            Debug.LogError("ATTENTION : Tu as oublié de glisser le Material 'HexMat' dans la case 'Terrain Material' du script !");
            return;
        }

        hexMesh = CreateHexagonMesh();
        GenerateWorld();
    }

    // --- (La fonction CreateHexagonMesh ne change pas) ---
    Mesh CreateHexagonMesh()
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        
        float outerRadius = 1f;
        float height = 1f;
        Vector3[] cornersTop = new Vector3[6];
        Vector3[] cornersBot = new Vector3[6];

        for (int i = 0; i < 6; i++)
        {
            float angle_deg = 60 * i - 30;
            float angle_rad = Mathf.PI / 180f * angle_deg;
            float x = Mathf.Cos(angle_rad) * outerRadius;
            float z = Mathf.Sin(angle_rad) * outerRadius;
            cornersTop[i] = new Vector3(x, height, z);
            cornersBot[i] = new Vector3(x, 0, z);
        }
        Vector3 centerTop = new Vector3(0, height, 0);

        for (int i = 0; i < 6; i++) // Top
        {
            vertices.Add(centerTop); vertices.Add(cornersTop[i]); vertices.Add(cornersTop[(i + 1) % 6]);
            int vCount = vertices.Count;
            triangles.Add(vCount - 3); triangles.Add(vCount - 2); triangles.Add(vCount - 1);
        }
        for (int i = 0; i < 6; i++) // Sides
        {
            Vector3 cTop1 = cornersTop[i]; Vector3 cTop2 = cornersTop[(i + 1) % 6];
            Vector3 cBot1 = cornersBot[i]; Vector3 cBot2 = cornersBot[(i + 1) % 6];
            
            vertices.Add(cTop1); vertices.Add(cBot1); vertices.Add(cBot2);
            triangles.Add(vertices.Count - 3); triangles.Add(vertices.Count - 2); triangles.Add(vertices.Count - 1);
            vertices.Add(cTop1); vertices.Add(cBot2); vertices.Add(cTop2);
            triangles.Add(vertices.Count - 3); triangles.Add(vertices.Count - 2); triangles.Add(vertices.Count - 1);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals(); 
        return mesh;
    }

    void GenerateWorld()
    {
        // Nettoyage
        foreach(Transform child in transform) Destroy(child.gameObject);

        float seed = Random.Range(0, 10000f);
        float xOffset = 1.732f;
        float zOffset = 1.5f;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int z = 0; z < mapHeight; z++)
            {
                float xPos = x * xOffset;
                if (z % 2 == 1) xPos += xOffset / 2f;
                float zPos = z * zOffset;

                float hNoise = Mathf.PerlinNoise((x + seed) * noiseScale, (z + seed) * noiseScale);
                float rNoise = Mathf.PerlinNoise((x + seed) * riverFrequency, (z + seed) * riverFrequency);
                float rDig = Mathf.Abs(rNoise - 0.5f);

                int finalY = Mathf.FloorToInt(hNoise * heightMultiplier);
                bool isRiver = rDig < riverWidth;
                
                if (isRiver && finalY >= seaLevel) finalY = seaLevel - 1;

                for (int y = -1; y <= finalY; y++)
                    CreateHexBlock(new Vector3(xPos, y, zPos), y, finalY, isRiver);
                
                if (finalY < seaLevel)
                    for (int y = finalY + 1; y <= seaLevel; y++)
                        CreateWaterBlock(new Vector3(xPos, y, zPos));
            }
        }
    }

    void CreateHexBlock(Vector3 pos, int y, int maxY, bool isRiver)
    {
        GameObject hex = new GameObject($"Hex");
        hex.transform.position = pos;
        hex.transform.parent = this.transform;
        
        MeshFilter mf = hex.AddComponent<MeshFilter>();
        MeshRenderer mr = hex.AddComponent<MeshRenderer>();
        
        mf.mesh = hexMesh;
        // On utilise le matériau que tu as glissé dans l'éditeur
        mr.material = terrainMaterial; 
        
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;

        MaterialPropertyBlock props = new MaterialPropertyBlock();
        Color color = Color.green;

        if (y < maxY) color = new Color(0.4f, 0.3f, 0.2f); 
        else if (isRiver) color = Color.grey; 
        else if (y < seaLevel + 1) color = new Color(1f, 0.9f, 0.5f); 
        else if (y > heightMultiplier * 0.7f) color = Color.white; 
        else if (y > heightMultiplier * 0.4f) color = Color.grey; 

        props.SetColor("_BaseColor", color); // URP utilise _BaseColor
        // Si ça reste noir/blanc, décommente la ligne suivante :
        props.SetColor("_Color", color); // Built-in utilise _Color

        mr.SetPropertyBlock(props);
    }

    void CreateWaterBlock(Vector3 pos)
    {
        GameObject water = new GameObject("Water");
        water.transform.position = pos;
        water.transform.parent = this.transform;
        
        MeshFilter mf = water.AddComponent<MeshFilter>();
        MeshRenderer mr = water.AddComponent<MeshRenderer>();
        
        mf.mesh = hexMesh;
        mr.material = terrainMaterial; // On utilise le même mat pour tester

        MaterialPropertyBlock props = new MaterialPropertyBlock();
        Color waterCol = new Color(0, 0.4f, 1f, 1f); // Bleu
        
        props.SetColor("_BaseColor", waterCol);
        props.SetColor("_Color", waterCol);
        
        mr.SetPropertyBlock(props);
    }
}