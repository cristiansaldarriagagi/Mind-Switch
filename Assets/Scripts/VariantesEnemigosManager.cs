using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class VariantesEnemigosManager : MonoBehaviour
{
    public static VariantesEnemigosManager Instancia;

    [Header("--- PROBABILIDADES DE CLASE (SUMA ~1.0) ---")]
    [Range(0f, 1f)] public float probCorredorKamikaze = 0.28f;
    [Range(0f, 1f)] public float probColosoBlindado = 0.17f;

    [Header("--- PARÁMETROS CORREDOR KAMIKAZE ---")]
    public float escalaKamikaze = 0.72f;
    public float multiplicadorVidaKamikaze = 0.45f;
    public float multiplicadorVelocidadKamikaze = 1.75f;
    public float danoExplosionKamikaze = 24f;
    public float radioDetonacionKamikaze = 2.1f;

    [Header("--- PARÁMETROS COLOSO BLINDADO ---")]
    public float escalaColoso = 1.48f;
    public float multiplicadorVidaColoso = 3.0f;
    public float multiplicadorVelocidadColoso = 0.65f;

    private enum ClaseEnemigo { Estandar, Kamikaze, Coloso }

    private class DatosEnemigo
    {
        public Salud salud;
        public ClaseEnemigo clase;
        public float velocidadExtraKamikaze;
    }

    private HashSet<int> procesados = new HashSet<int>();
    private List<DatosEnemigo> enemigosActivos = new List<DatosEnemigo>();
    private Transform jugador;

    void Awake()
    {
        Instancia = this;
    }

    void Start()
    {
        GameObject objP = GameObject.FindGameObjectWithTag("Player");
        if (objP != null) jugador = objP.transform;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        DetectarYTransformarNuevosEnemigos();
        ActualizarComportamientoKamikazes();
    }

    private void DetectarYTransformarNuevosEnemigos()
    {
        Salud[] todos = FindObjectsByType<Salud>(FindObjectsSortMode.None);
        foreach (Salud entidad in todos)
        {
            if (entidad == null || entidad.CompareTag("Player") || entidad.esObjetivoCritico || entidad.VidaActual <= 0f)
                continue;

            int id = entidad.gameObject.GetInstanceID();
            if (procesados.Contains(id)) continue;

            procesados.Add(id);

            float r = Random.value;
            ClaseEnemigo claseElegida = ClaseEnemigo.Estandar;
            if (r < probCorredorKamikaze)
                claseElegida = ClaseEnemigo.Kamikaze;
            else if (r < probCorredorKamikaze + probColosoBlindado)
                claseElegida = ClaseEnemigo.Coloso;

            AplicarClaseEnemigo(entidad, claseElegida);
        }
    }

    private void AplicarClaseEnemigo(Salud entidad, ClaseEnemigo clase)
    {
        if (clase == ClaseEnemigo.Estandar)
        {
            enemigosActivos.Add(new DatosEnemigo { salud = entidad, clase = ClaseEnemigo.Estandar });
            return;
        }

        NavMeshAgent agente = entidad.GetComponent<NavMeshAgent>();

        if (clase == ClaseEnemigo.Kamikaze)
        {
            entidad.transform.localScale *= escalaKamikaze;
            entidad.vidaMaxima = Mathf.Max(20f, entidad.vidaMaxima * multiplicadorVidaKamikaze);
            entidad.Curar(entidad.vidaMaxima);
            entidad.alturaBarraEnemigo *= 0.85f;

            if (agente != null)
            {
                agente.speed *= multiplicadorVelocidadKamikaze;
                agente.acceleration *= 1.5f;
            }

            AplicarTinteVisual(entidad.gameObject, new Color(1f, 0.45f, 0.05f), 2.4f);

            enemigosActivos.Add(new DatosEnemigo
            {
                salud = entidad,
                clase = ClaseEnemigo.Kamikaze,
                velocidadExtraKamikaze = 3.2f
            });
        }
        else if (clase == ClaseEnemigo.Coloso)
        {
            entidad.transform.localScale *= escalaColoso;
            entidad.vidaMaxima *= multiplicadorVidaColoso;
            entidad.Curar(entidad.vidaMaxima);
            entidad.alturaBarraEnemigo *= 1.35f;

            if (agente != null)
            {
                agente.speed *= multiplicadorVelocidadColoso;
            }

            AplicarTinteVisual(entidad.gameObject, new Color(0.68f, 0.15f, 1f), 2.0f);

            enemigosActivos.Add(new DatosEnemigo
            {
                salud = entidad,
                clase = ClaseEnemigo.Coloso
            });
        }
    }

    private void AplicarTinteVisual(GameObject obj, Color color, float intensidadLuz)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            foreach (Material m in r.materials)
            {
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
                if (m.HasProperty("_Color")) m.color = color;
                m.EnableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color * 1.4f);
            }
        }

        Light luz = obj.GetComponentInChildren<Light>();
        if (luz == null) luz = obj.AddComponent<Light>();
        luz.color = color;
        luz.range = 4.2f;
        luz.intensity = intensidadLuz;
    }

    private void ActualizarComportamientoKamikazes()
    {
        for (int i = enemigosActivos.Count - 1; i >= 0; i--)
        {
            DatosEnemigo d = enemigosActivos[i];
            if (d.salud == null || d.salud.VidaActual <= 0f)
            {
                enemigosActivos.RemoveAt(i);
                continue;
            }

            if (d.clase != ClaseEnemigo.Kamikaze) continue;

            Transform tEnemigo = d.salud.transform;

            // Si no usa NavMeshAgent, le damos impulso extra hacia el jugador
            NavMeshAgent agente = tEnemigo.GetComponent<NavMeshAgent>();
            if ((agente == null || !agente.isActiveAndEnabled) && jugador != null)
            {
                Vector3 dir = (jugador.position - tEnemigo.position);
                dir.y = 0f;
                if (dir.magnitude > 0.5f)
                {
                    tEnemigo.position += dir.normalized * d.velocidadExtraKamikaze * Time.deltaTime;
                }
            }

            // Detonación suicida si toca al jugador
            if (jugador != null && Vector3.Distance(tEnemigo.position, jugador.position) <= radioDetonacionKamikaze)
            {
                Salud saludPlayer = jugador.GetComponent<Salud>();
                if (saludPlayer != null) saludPlayer.RecibirDanio(danoExplosionKamikaze);
                d.salud.RecibirDanio(9999f); // Se autodestruye explotando
                continue;
            }

            // Detonación suicida si toca la Estrella durante el frenesí
            if (AsaltoEstrellaManager.Instancia != null && AsaltoEstrellaManager.Instancia.FrenesiActivo && AsaltoEstrellaManager.Instancia.estrellaObjetivo != null)
            {
                Transform tEstrella = AsaltoEstrellaManager.Instancia.estrellaObjetivo;
                if (Vector3.Distance(tEnemigo.position, tEstrella.position) <= radioDetonacionKamikaze)
                {
                    Salud saludEstrella = tEstrella.GetComponent<Salud>();
                    if (saludEstrella != null) saludEstrella.RecibirDanio(danoExplosionKamikaze);
                    d.salud.RecibirDanio(9999f);
                }
            }
        }
    }
}