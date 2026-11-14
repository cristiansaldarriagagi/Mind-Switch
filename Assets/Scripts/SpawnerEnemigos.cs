using UnityEngine;
using System.Collections.Generic;

public class SpawnerEnemigos : MonoBehaviour
{
    [Header("--- PREFAB Y PUNTOS DE SPAWN ---")]
    public GameObject enemigoPrefab;
    public Transform[] puntosDeSpawn;

    [Header("--- CONFIGURACIÓN DE OLEADAS ---")]
    [Tooltip("Enemigos que aparecen de inmediato al iniciar la partida")]
    public int enemigosAlIniciar = 8;
    [Tooltip("Segundos entre cada aparición normal")]
    public float intervaloSpawn = 2.2f;
    [Tooltip("Máximo de enemigos simultáneos en el mapa")]
    public int maximoEnemigosVivos = 24;
    public float distanciaMinimaAlJugador = 8f;

    [Header("--- MODO DEFENSA DE LA ESTRELLA ---")]
    public float intervaloSupervivencia = 1.0f;
    public int maximoSupervivencia = 35;

    private List<GameObject> enemigosActivos = new List<GameObject>();
    private float temporizador;

    void Start()
    {
        // Genera una oleada inicial repartida en las 4 habitaciones al arrancar
        for (int i = 0; i < enemigosAlIniciar; i++)
        {
            if (puntosDeSpawn.Length == 0) break;
            Transform punto = puntosDeSpawn[i % puntosDeSpawn.Length];
            SpawnearEnPunto(punto);
        }
    }

    void Update()
    {
        enemigosActivos.RemoveAll(e => e == null);

        if (enemigosActivos.Count >= maximoEnemigosVivos || puntosDeSpawn.Length == 0) return;

        temporizador += Time.deltaTime;
        if (temporizador >= intervaloSpawn)
        {
            SpawnearEnemigo();
            temporizador = 0f;
        }
    }

    void SpawnearEnemigo()
    {
        Transform puntoElegido = puntosDeSpawn[Random.Range(0, puntosDeSpawn.Length)];
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null && Vector3.Distance(puntoElegido.position, player.transform.position) < distanciaMinimaAlJugador)
        {
            return;
        }

        SpawnearEnPunto(puntoElegido);
    }

    void SpawnearEnPunto(Transform punto)
    {
        if (enemigoPrefab == null || punto == null) return;

        // Pequeña variación aleatoria de 1m para que no nazcan exactamente uno encima de otro
        Vector3 offset = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        GameObject nuevoEnemigo = Instantiate(enemigoPrefab, punto.position + offset, punto.rotation);
        enemigosActivos.Add(nuevoEnemigo);
    }

    // Conecta este método al evento On Supervivencia Iniciada de Estrella_Objetivo
    public void ActivarModoSupervivencia()
    {
        intervaloSpawn = intervaloSupervivencia;
        maximoEnemigosVivos = maximoSupervivencia;
        Debug.Log("¡ALERTA! Oleadas intensificadas para defender la Estrella.");
    }
}