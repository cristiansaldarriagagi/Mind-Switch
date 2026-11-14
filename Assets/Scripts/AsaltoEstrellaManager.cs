using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class AsaltoEstrellaManager : MonoBehaviour
{
    public static AsaltoEstrellaManager Instancia;

    [Header("--- REFERENCIAS ---")]
    public Transform estrellaObjetivo;
    public Transform jugador;

    [Header("--- CONFIGURACIÓN DE DEFENSA ---")]
    [Tooltip("Vida que tendrá la Estrella cuando empiece el frenesí")]
    public float vidaEstrella = 300f;
    [Range(0.1f, 0.9f)]
    [Tooltip("Porcentaje de enemigos que atacarán la Estrella (el resto te atacará a ti)")]
    public float proporcionAtacantesEstrella = 0.55f;
    [Tooltip("Distancia a la que un enemigo golpea la Estrella")]
    public float rangoAtaqueEstrella = 2.4f;
    [Tooltip("Daño por golpe de cada enemigo a la Estrella")]
    public float danoPorGolpe = 10f;
    public float intervaloGolpe = 1.2f;

    private bool frenesiActivo = false;
    private Salud saludEstrella;

    // Registro de qué enemigos van por la Estrella y sus temporizadores de ataque
    private HashSet<int> enemigosContraEstrella = new HashSet<int>();
    private HashSet<int> enemigosRegistrados = new HashSet<int>();
    private Dictionary<int, float> timerAtaqueEnemigo = new Dictionary<int, float>();

    public bool FrenesiActivo => frenesiActivo;

    void Awake()
    {
        Instancia = this;
    }

    void Start()
    {
        if (estrellaObjetivo == null)
        {
            GameObject obj = GameObject.Find("Estrella_Objetivo");
            if (obj != null) estrellaObjetivo = obj.transform;
        }

        if (jugador == null)
        {
            GameObject objP = GameObject.FindGameObjectWithTag("Player");
            if (objP != null) jugador = objP.transform;
        }

        if (estrellaObjetivo != null)
        {
            saludEstrella = estrellaObjetivo.GetComponent<Salud>();
            if (saludEstrella == null)
            {
                saludEstrella = estrellaObjetivo.gameObject.AddComponent<Salud>();
            }

            saludEstrella.vidaMaxima = vidaEstrella;
            saludEstrella.esObjetivoCritico = true;
            saludEstrella.destruirAlMorir = false;
            saludEstrella.esInmune = true; // Inmune hasta que el jugador toque la Estrella
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f || estrellaObjetivo == null) return;

        // 1. Detecta automáticamente cuando el jugador toca la Estrella para iniciar el asalto
        if (!frenesiActivo && jugador != null)
        {
            float distJugador = Vector3.Distance(jugador.position, estrellaObjetivo.position);
            if (distJugador <= 2.6f)
            {
                ActivarAsaltoAEstrella();
            }
        }

        if (!frenesiActivo) return;

        // 2. Gestiona todos los enemigos vivos en la escena
        Salud[] todos = FindObjectsByType<Salud>(FindObjectsSortMode.None);
        foreach (Salud entidad in todos)
        {
            if (entidad == null || entidad.CompareTag("Player") || entidad.esObjetivoCritico || entidad.VidaActual <= 0f)
                continue;

            int id = entidad.gameObject.GetInstanceID();

            // Asigna rol la primera vez que vemos a este enemigo durante el frenesí
            if (!enemigosRegistrados.Contains(id))
            {
                enemigosRegistrados.Add(id);
                if (Random.value <= proporcionAtacantesEstrella)
                {
                    enemigosContraEstrella.Add(id);
                }
            }

            // Si este enemigo tiene asignado atacar la Estrella, lo dirigimos y hacemos que la golpee
            if (enemigosContraEstrella.Contains(id))
            {
                DirigirYAtacarEstrella(entidad.transform, id);
            }
        }
    }

    public void ActivarAsaltoAEstrella()
    {
        if (frenesiActivo) return;
        frenesiActivo = true;

        if (saludEstrella != null)
        {
            saludEstrella.esInmune = false;
            saludEstrella.Curar(vidaEstrella);
        }

        Debug.Log("¡FRENESÍ ACTIVADO! Los enemigos ahora atacan tanto al jugador como al Núcleo Estrella.");
    }

    private void DirigirYAtacarEstrella(Transform enemigo, int id)
    {
        Vector3 posEstrella = new Vector3(estrellaObjetivo.position.x, enemigo.position.y, estrellaObjetivo.position.z);
        float distancia = Vector3.Distance(enemigo.position, posEstrella);

        NavMeshAgent agente = enemigo.GetComponent<NavMeshAgent>();
        if (agente != null && agente.isActiveAndEnabled && agente.isOnNavMesh)
        {
            agente.SetDestination(posEstrella);
        }
        else if (distancia > rangoAtaqueEstrella * 0.85f)
        {
            Vector3 dir = (posEstrella - enemigo.position).normalized;
            enemigo.position += dir * 4.2f * Time.deltaTime;
            if (dir != Vector3.zero)
            {
                enemigo.rotation = Quaternion.Slerp(enemigo.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
            }
        }

        // Si llegó al rango de la Estrella, la golpea periódicamente
        if (distancia <= rangoAtaqueEstrella)
        {
            if (!timerAtaqueEnemigo.ContainsKey(id)) timerAtaqueEnemigo[id] = 0f;
            timerAtaqueEnemigo[id] += Time.deltaTime;

            if (timerAtaqueEnemigo[id] >= intervaloGolpe)
            {
                timerAtaqueEnemigo[id] = 0f;
                if (saludEstrella != null)
                {
                    saludEstrella.RecibirDanio(danoPorGolpe);
                    if (SFXManager.Instancia != null)
                    {
                        SFXManager.Instancia.ReproducirImpacto(estrellaObjetivo.position);
                    }
                }
            }
        }
    }
}