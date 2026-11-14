using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class PulsoEMPManager : MonoBehaviour
{
    public static PulsoEMPManager Instancia;

    [Header("--- CONTROLES ---")]
    public KeyCode teclaEMP = KeyCode.Q;
    public bool permitirClicDerecho = true;

    [Header("--- CONFIGURACIÓN DEL PULSO EMP ---")]
    public float tiempoRecarga = 10f;
    public float radioOnda = 8.5f;
    public float fuerzaEmpuje = 6.5f;
    public float duracionAturdimiento = 2.6f;
    public float danoEMP = 20f;

    private float timerRecarga = 0f;
    private Transform jugador;
    private static Texture2D texBlanca;
    private AudioSource audioEMP;
    private AudioClip clipEMP;

    private class EnemigoAturdido
    {
        public Transform transform;
        public NavMeshAgent agente;
        public float tiempoFinAturdimiento;
        public Vector3 velocidadEmpuje;
        public float timerEmpuje;
    }

    private List<EnemigoAturdido> aturdidos = new List<EnemigoAturdido>();

    private GameObject anilloVisual;
    private float timerOndaVisual = 0f;
    private float duracionOndaVisual = 0.45f;

    void Awake()
    {
        Instancia = this;
        if (texBlanca == null)
        {
            texBlanca = new Texture2D(1, 1);
            texBlanca.SetPixel(0, 0, Color.white);
            texBlanca.Apply();
        }

        audioEMP = gameObject.AddComponent<AudioSource>();
        audioEMP.playOnAwake = false;
        audioEMP.volume = 0.65f;
        clipEMP = CrearSonidoEMP();
    }

    void Start()
    {
        GameObject objP = GameObject.FindGameObjectWithTag("Player");
        if (objP != null) jugador = objP.transform;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        if (timerRecarga > 0f)
        {
            timerRecarga = Mathf.Max(0f, timerRecarga - Time.deltaTime);
        }

        bool activar = Input.GetKeyDown(teclaEMP) || (permitirClicDerecho && Input.GetMouseButtonDown(1));
        if (activar && timerRecarga <= 0f && jugador != null)
        {
            DetonarPulsoEMP();
        }

        ActualizarEnemigosAturdidos();
        ActualizarOndaVisual();
    }

    public void DetonarPulsoEMP()
    {
        timerRecarga = tiempoRecarga;
        audioEMP.PlayOneShot(clipEMP);
        CrearOndaVisual3D(jugador.position);

        Salud[] entidades = FindObjectsByType<Salud>(FindObjectsSortMode.None);
        foreach (Salud entidad in entidades)
        {
            if (entidad == null || entidad.CompareTag("Player") || entidad.esObjetivoCritico || entidad.VidaActual <= 0f)
                continue;

            Vector3 diff = entidad.transform.position - jugador.position;
            diff.y = 0f;
            float dist = diff.magnitude;

            if (dist <= radioOnda)
            {
                if (danoEMP > 0f)
                {
                    entidad.RecibirDanio(danoEMP);
                }

                if (entidad == null || entidad.VidaActual <= 0f) continue;

                Vector3 dirEmpuje = dist > 0.05f ? diff.normalized : entidad.transform.forward;
                float factorCercania = Mathf.Lerp(1.3f, 0.7f, dist / radioOnda);

                NavMeshAgent agente = entidad.GetComponent<NavMeshAgent>();
                if (agente != null && agente.isActiveAndEnabled && agente.isOnNavMesh)
                {
                    agente.isStopped = true;
                    agente.velocity = Vector3.zero;
                }

                AlternarIAEnemigo(entidad.transform, false);

                aturdidos.RemoveAll(a => a.transform == entidad.transform);
                aturdidos.Add(new EnemigoAturdido
                {
                    transform = entidad.transform,
                    agente = agente,
                    tiempoFinAturdimiento = Time.time + duracionAturdimiento,
                    velocidadEmpuje = dirEmpuje * (fuerzaEmpuje * factorCercania),
                    timerEmpuje = 0.25f
                });
            }
        }
    }

    private void ActualizarEnemigosAturdidos()
    {
        for (int i = aturdidos.Count - 1; i >= 0; i--)
        {
            EnemigoAturdido e = aturdidos[i];
            if (e.transform == null)
            {
                aturdidos.RemoveAt(i);
                continue;
            }

            if (e.timerEmpuje > 0f)
            {
                e.timerEmpuje -= Time.deltaTime;
                Vector3 desplazamiento = e.velocidadEmpuje * Time.deltaTime;

                if (e.agente != null && e.agente.isActiveAndEnabled && e.agente.isOnNavMesh)
                {
                    e.agente.Move(desplazamiento);
                }
                else
                {
                    e.transform.position += desplazamiento;
                }
            }

            if (Time.time >= e.tiempoFinAturdimiento)
            {
                if (e.agente != null && e.agente.isActiveAndEnabled && e.agente.isOnNavMesh)
                {
                    e.agente.isStopped = false;
                }
                AlternarIAEnemigo(e.transform, true);
                aturdidos.RemoveAt(i);
            }
        }
    }

    private void AlternarIAEnemigo(Transform enemigo, bool activo)
    {
        MonoBehaviour[] scripts = enemigo.GetComponents<MonoBehaviour>();
        foreach (var s in scripts)
        {
            if (s == null) continue;
            string n = s.GetType().Name.ToLower();
            if (n.Contains("enemigo") || n.Contains("enemy") || n.Contains("ia") || n.Contains("ai"))
            {
                s.enabled = activo;
            }
        }
    }

    private void CrearOndaVisual3D(Vector3 centro)
    {
        if (anilloVisual != null) Destroy(anilloVisual);

        anilloVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        anilloVisual.name = "Onda_EMP_Visual";
        Destroy(anilloVisual.GetComponent<Collider>());

        anilloVisual.transform.position = new Vector3(centro.x, 0.25f, centro.z);
        anilloVisual.transform.localScale = new Vector3(0.5f, 0.04f, 0.5f);

        Renderer rend = anilloVisual.GetComponent<Renderer>();
        Color cianEMP = new Color(0.1f, 0.85f, 1f, 0.45f);
        rend.material.color = cianEMP;
        rend.material.EnableKeyword("_EMISSION");
        rend.material.SetColor("_EmissionColor", cianEMP * 2.5f);

        Light luz = anilloVisual.AddComponent<Light>();
        luz.color = new Color(0.15f, 0.9f, 1f);
        luz.range = radioOnda * 1.3f;
        luz.intensity = 5f;

        timerOndaVisual = duracionOndaVisual;
    }

    private void ActualizarOndaVisual()
    {
        if (anilloVisual == null) return;

        timerOndaVisual -= Time.deltaTime;
        if (timerOndaVisual <= 0f)
        {
            Destroy(anilloVisual);
            anilloVisual = null;
            return;
        }

        float progreso = 1f - (timerOndaVisual / duracionOndaVisual);
        float diametro = Mathf.Lerp(0.8f, radioOnda * 2f, progreso);
        anilloVisual.transform.localScale = new Vector3(diametro, 0.03f, diametro);

        Light luz = anilloVisual.GetComponent<Light>();
        if (luz != null) luz.intensity = Mathf.Lerp(5f, 0f, progreso);
    }

    // --- FILA 2 (ENCIMA DE CALOR ARMA): BARRA DE RECARGA DEL PULSO EMP ---
    void OnGUI()
    {
        if (Time.timeScale == 0f || texBlanca == null || jugador == null) return;

        float ancho = 240f;
        float alto = 22f;
        Rect fondo = new Rect(Screen.width - ancho - 20f, Screen.height - 78f, ancho, alto);

        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.DrawTexture(new Rect(fondo.x - 2, fondo.y - 2, ancho + 4, alto + 4), texBlanca);

        float pctCarga = timerRecarga <= 0f ? 1f : 1f - (timerRecarga / tiempoRecarga);
        GUI.color = pctCarga >= 1f ? new Color(0.12f, 0.85f, 1f) : new Color(0.25f, 0.45f, 0.6f);
        GUI.DrawTexture(new Rect(fondo.x, fondo.y, ancho * pctCarga, alto), texBlanca);

        GUI.color = Color.white;
        GUIStyle estilo = new GUIStyle(GUI.skin.label);
        estilo.alignment = TextAnchor.MiddleCenter;
        estilo.fontStyle = FontStyle.Bold;
        estilo.fontSize = 11;

        string texto = pctCarga >= 1f ? "[Q / CLIC DER] PULSO EMP LISTO" : $"CARGANDO EMP: {timerRecarga:F1}s";
        GUI.Label(fondo, texto, estilo);
    }

    private AudioClip CrearSonidoEMP()
    {
        int sr = 44100;
        int muestras = Mathf.CeilToInt(sr * 0.55f);
        float[] datos = new float[muestras];
        for (int i = 0; i < muestras; i++)
        {
            float t = (float)i / sr;
            float p = t / 0.55f;
            float freqSub = Mathf.Lerp(260f, 48f, p);
            float pulso = Mathf.Sin(2f * Mathf.PI * freqSub * t) * 0.6f;
            float chispa = (Random.value * 2f - 1f) * Mathf.Exp(-t * 10f) * 0.25f;
            datos[i] = (pulso + chispa) * (1f - p);
        }
        AudioClip c = AudioClip.Create("SFX_PulsoEMP", muestras, 1, sr, false);
        c.SetData(datos, 0);
        return c;
    }
}