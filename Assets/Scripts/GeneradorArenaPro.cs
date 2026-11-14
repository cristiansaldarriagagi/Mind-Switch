using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GeneradorArenaPro : MonoBehaviour
{
    [Header("--- 1. ARQUITECTURA (Suelo hace de techo) ---")]
    public GameObject floorPrefab; 
    public GameObject flatWallPrefab; 
    public GameObject doorPrefab;

    [Header("--- 2. OBSTÁCULOS TÁCTICOS ---")]
    public GameObject[] coverPrefabs;
    public GameObject[] propPrefabs;

    private const float TILE = 4f;
    private const float HEIGHT = 4f; 

    private readonly string[] mapLayout = new string[] {
        "0000000000000000000000000",
        "0000000000000000000000000",
        "0111111102222222011111110",
        "0111111102222222011111110",
        "0110001102222222011000110",
        "0110330100002000010440110",
        "0110331111111111111440110",
        "0110330100001000010440110",
        "0110000105555555010000110",
        "0111111115555555111111110",
        "0000110005555555000110000",
        "0660110700005000080110990",
        "0661111711111111181111990", 
        "066011070000A000080110990",
        "000011000AAAAAAA000110000",
        "011111111AAAAAAA111111110",
        "011000010AAAAAAA010000110",
        "0110B0010000A0000100C0110",
        "0110B111111111111111C0110",
        "0110B0010000D0000100C0110",
        "011000110DDDDDDD011000110",
        "011111110DDDDDDD011111110",
        "011111110DDDDDDD011111110",
        "0000000000000000000000000",
        "0000000000000000000000000" 
    };

    private HashSet<string> placedObjects = new HashSet<string>();
    private Transform rootTransform;

    public void ConstruirMapaPro()
    {
        while (transform.childCount > 0) DestroyImmediate(transform.GetChild(0).gameObject);

        GameObject root = new GameObject("[ARENA_PRO_100x100m_SOLIDA]");
        rootTransform = root.transform;
        rootTransform.parent = transform;
        placedObjects.Clear();

        Vector3 offsetGlobal = new Vector3(-12.5f * TILE, 0, -12.5f * TILE);

        for (int z = 0; z < 25; z++)
        {
            for (int x = 0; x < 25; x++)
            {
                char tile = mapLayout[z][x];

                if (tile != '0')
                {
                    Vector3 pos = new Vector3(x * TILE, 0, (24 - z) * TILE) + offsetGlobal;

                    if (floorPrefab)
                    {
                        Instantiate(floorPrefab, pos, Quaternion.identity, rootTransform);
                        Instantiate(floorPrefab, pos + Vector3.up * HEIGHT, Quaternion.Euler(180, 0, 0), rootTransform);
                    }

                    char n = z > 0 ? mapLayout[z - 1][x] : '0';  
                    char s = z < 24 ? mapLayout[z + 1][x] : '0'; 
                    char e = x < 24 ? mapLayout[z][x + 1] : '0'; 
                    char w = x > 0 ? mapLayout[z][x - 1] : '0';  

                    if (n != tile) PonerSeparador(n == '0' ? flatWallPrefab : doorPrefab, pos, 0, n != '0');
                    if (s != tile) PonerSeparador(s == '0' ? flatWallPrefab : doorPrefab, pos, 180, s != '0');
                    if (e != tile) PonerSeparador(e == '0' ? flatWallPrefab : doorPrefab, pos, 90, e != '0');
                    if (w != tile) PonerSeparador(w == '0' ? flatWallPrefab : doorPrefab, pos, -90, w != '0');

                    float coverChance = (tile == '1') ? 0.02f : 0.15f;
                    if (coverPrefabs != null && coverPrefabs.Length > 0 && Random.value < coverChance)
                        Instantiate(coverPrefabs[Random.Range(0, coverPrefabs.Length)], pos, Quaternion.Euler(0, Random.Range(0, 4) * 90, 0), rootTransform);
                    
                    if (propPrefabs != null && propPrefabs.Length > 0 && Random.value < (coverChance / 2f))
                        Instantiate(propPrefabs[Random.Range(0, propPrefabs.Length)], pos, Quaternion.Euler(0, Random.Range(0, 4) * 90, 0), rootTransform);
                }
            }
        }

        // --- MAGIA NUEVA: HACER QUE TODO SEA SÓLIDO ---
        SellarFisicas(rootTransform);
    }

    void PonerSeparador(GameObject prefab, Vector3 tilePos, float rotY, bool isDoor)
    {
        if (prefab == null) return;

        Vector3 offset = Vector3.zero;
        if (rotY == 0) offset = new Vector3(0, 0, TILE / 2f);
        if (rotY == 180) offset = new Vector3(0, 0, -TILE / 2f);
        if (rotY == 90) offset = new Vector3(TILE / 2f, 0, 0);
        if (rotY == -90) offset = new Vector3(-TILE / 2f, 0, 0);

        Vector3 finalPos = tilePos + offset;
        
        string key = isDoor ? $"DOOR_{Mathf.Round(finalPos.x)}_{Mathf.Round(finalPos.z)}" 
                            : $"WALL_{Mathf.Round(finalPos.x)}_{Mathf.Round(finalPos.z)}_{rotY}";

        if (!placedObjects.Contains(key))
        {
            Instantiate(prefab, finalPos, Quaternion.Euler(0, rotY, 0), rootTransform);
            placedObjects.Add(key);
        }
    }

    // Esta función busca todas las mallas visuales (Mesh) y les pone un colisionador (Collider)
    void SellarFisicas(Transform padre)
    {
        MeshRenderer[] todasLasPiezas = padre.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer pieza in todasLasPiezas)
        {
            if (pieza.gameObject.GetComponent<Collider>() == null)
            {
                pieza.gameObject.AddComponent<MeshCollider>();
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GeneradorArenaPro))]
public class GeneradorArenaProEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GeneradorArenaPro script = (GeneradorArenaPro)target;

        EditorGUILayout.Space(20);
        GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold };
        GUI.backgroundColor = new Color(0.9f, 0.1f, 0.1f); 
        
        if (GUILayout.Button("🔥 GENERAR MAPA SÓLIDO 🔥", btnStyle, GUILayout.Height(55)))
        {
            script.ConstruirMapaPro();
        }
    }
}
#endif