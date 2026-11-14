using UnityEngine;
using System.Collections.Generic;

public class SobrecargaYLootManager : MonoBehaviour
{
    public static SobrecargaYLootManager Instancia;

    [Header("--- SISTEMA DE CALOR / SOBRECARGA ---")]
    [Tooltip("Cuánto sube el calor (0 a 100) por cada segundo disparando")]
    public float velocidadCalentamiento = 28f;
    [Tooltip("Cuánto baja el calor por segundo al soltar el gatillo")]
    public float velocidadEnfriamiento = 36f;
    [Tooltip("Segundos que el arma queda bloqueada si llega al 100%")]
    public float penalizacionSobrecarga = 2.4f;

    [Header("--- RECOLECTABLES (LOOT DE ENEMIGOS) ---")]
    [Range(0f, 1f)]
    public float probabilidadSoltarOrbe = 0.45f;
    public float curacionPorOrbeVerde = 30f;
    public float duracionPoderes = 6.5f;
    public float radioRecoleccion = 2.2f;

    private float calorActual = 0f;
    private bool estaSobrecargado = false;
    private float timerBloqueo = 0f;

    private float timerCalorInfinito = 0f;
    private float timerDisparoTriple = 0f;
    private string mensajeAviso = "";
    private float timerMensaje = 0f;

    private Transform jugador;
    private static Texture2D texBlanca;
    private AudioSource audioEfectos;
    private AudioClip clipPowerUp;
    private AudioClip clipAlertaCalor;

    private enum TipoOrbe { Reparacion, CalorInfinito, DisparoTriple }

    private class OrbeActivo
    {
        public GameObject objeto;
        public TipoOrbe tipo;
        public float tiempoCreacion;
    }

    private List<OrbeActivo> orbesEnMapa = new List<OrbeActivo>();
    private HashSet<int> balasProcesadas = new HashSet<int>();

    void Awake()
    {
        Instancia = this;
        if (texBlanca == null)
        {
            texBlanca = new Texture2D(1, 1);
            texBlanca.SetPixel(0, 0, Color.white);
            texBlanca.Apply();
        }

        audioEfectos = gameObject.AddComponent<AudioSource>();
        audioEfectos.playOnAwake = false;
        audioEfectos.volume = 0.45f;
        clipPowerUp = CrearSonidoPowerUp();
        clipAlertaCalor = CrearSonidoSobrecarga();
    }

    void Start()
    {
        GameObject objP = GameObject.FindGameObjectWithTag("Player");
        if (objP != null)
        {
            jugador = objP.transform;
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        ActualizarTemporizadoresPoderes();
        ActualizarSistemaCalor();
        ActualizarOrbesYRecoleccion();
        AplicarDisparoTripleSiActivo();
    }

    private void ActualizarTemporizadoresPoderes()
    {
        if (timerCalorInfinito > 0f) timerCalorInfinito -= Time.deltaTime;
        if (timerDisparoTriple > 0f) timerDisparoTriple -= Time.deltaTime;
        if (timerMensaje > 0f) timerMensaje -= Time.deltaTime;
    }

    private void ActualizarSistemaCalor()
    {
        bool disparando = Input.GetMouseButton(0);

        if (estaSobrecargado)
        {
            timerBloqueo -= Time.deltaTime;
            calorActual = Mathf.Lerp(0f, 100f, timerBloqueo / penalizacionSobrecarga);

            ControlarArmas(false);

            if (timerBloqueo <= 0f)
            {
                estaSobrecargado = false;
                calorActual = 0f;
                ControlarArmas(true);
            }
            return;
        }

        if (timerCalorInfinito > 0f)
        {
            calorActual = Mathf.MoveTowards(calorActual, 0f, velocidadEnfriamiento * 2f * Time.deltaTime);
            ControlarArmas(true);
            return;
        }

        if (disparando)
        {
            calorActual += velocidadCalentamiento * Time.deltaTime;
            if (calorActual >= 100f)
            {
                calorActual = 100f;
                estaSobrecargado = true;
                timerBloqueo = penalizacionSobrecarga;
                ControlarArmas(false);
                audioEfectos.PlayOneShot(clipAlertaCalor);
                MostrarAviso("¡CAÑONES SOBRECALENTADOS! ENFRIANDO...", 2.2f);
            }
        }
        else
        {
            calorActual = Mathf.Max(0f, calorActual - velocidadEnfriamiento * Time.deltaTime);
        }
    }

    private void ControlarArmas(bool habilitado)
    {
        if (jugador == null) return;
        MonoBehaviour[] componentes = jugador.GetComponentsInChildren<MonoBehaviour>();
        foreach (var c in componentes)
        {
            if (c == null) continue;
            string n = c.GetType().Name.ToLower();
            if (n.Contains("weapon") || n.Contains("arma") || n.Contains("shoot") || n.Contains("disparo") || n.Contains("turret"))
            {
                c.enabled = habilitado;
            }
        }
    }

    private void AplicarDisparoTripleSiActivo()
    {
        if (timerDisparoTriple <= 0f || estaSobrecargado) return;

        Bala[] balas = FindObjectsByType<Bala>(FindObjectsSortMode.None);
        foreach (Bala b in balas)
        {
            if (b == null) continue;
            int id = b.gameObject.GetInstanceID();
            if (balasProcesadas.Contains(id)) continue;

            balasProcesadas.Add(id);
            if (b.CompareTag("Player") || (jugador != null && Vector3.Distance(b.transform.position, jugador.position) < 3.5f))
            {
                ClonarBalaEnAngulo(b.gameObject, -12f);
                ClonarBalaEnAngulo(b.gameObject, 12f);
            }
        }
    }

    private void ClonarBalaEnAngulo(GameObject original, float anguloGrados)
    {
        Quaternion rot = original.transform.rotation * Quaternion.Euler(0f, anguloGrados, 0f);
        GameObject clon = Instantiate(original, original.transform.position, rot);
        balasProcesadas.Add(clon.GetInstanceID());
    }

    public void IntentarSoltarLoot(Vector3 posicionEnemigo)
    {
        if (Random.value > probabilidadSoltarOrbe) return;

        float r = Random.value;
        TipoOrbe tipoElegido = r < 0.42f ? TipoOrbe.Reparacion :
                               r < 0.74f ? TipoOrbe.CalorInfinito :
                                           TipoOrbe.DisparoTriple;

        GameObject esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        esfera.name = "Orbe_Poder_" + tipoElegido;
        esfera.transform.position = new Vector3(posicionEnemigo.x, 1.0f, posicionEnemigo.z);
        esfera.transform.localScale = Vector3.one * 0.65f;

        Destroy(esfera.GetComponent<Collider>());

        Renderer rend = esfera.GetComponent<Renderer>();
        Color colorOrbe = tipoElegido == TipoOrbe.Reparacion ? new Color(0.15f, 1f, 0.35f) :
                          tipoElegido == TipoOrbe.CalorInfinito ? new Color(0.1f, 0.9f, 1f) :
                                                                  new Color(0.85f, 0.25f, 1f);
        rend.material.color = colorOrbe;
        rend.material.EnableKeyword("_EMISSION");
        rend.material.SetColor("_EmissionColor", colorOrbe * 2.2f);

        Light luz = esfera.AddComponent<Light>();
        luz.color = colorOrbe;
        luz.range = 4.5f;
        luz.intensity = 2.5f;

        orbesEnMapa.Add(new OrbeActivo
        {
            objeto = esfera,
            tipo = tipoElegido,
            tiempoCreacion = Time.time
        });
    }

    private void ActualizarOrbesYRecoleccion()
    {
        if (jugador == null) return;

        for (int i = orbesEnMapa.Count - 1; i >= 0; i--)
        {
            OrbeActivo orbe = orbesEnMapa[i];
            if (orbe.objeto == null)
            {
                orbesEnMapa.RemoveAt(i);
                continue;
            }

            float yFlotante = 1.0f + Mathf.Sin((Time.time - orbe.tiempoCreacion) * 4f) * 0.22f;
            orbe.objeto.transform.position = new Vector3(orbe.objeto.transform.position.x, yFlotante, orbe.objeto.transform.position.z);
            orbe.objeto.transform.Rotate(Vector3.up, 140f * Time.deltaTime);

            if (Time.time - orbe.tiempoCreacion > 22f)
            {
                Destroy(orbe.objeto);
                orbesEnMapa.RemoveAt(i);
                continue;
            }

            float dist = Vector3.Distance(
                new Vector3(jugador.position.x, 0, jugador.position.z),
                new Vector3(orbe.objeto.transform.position.x, 0, orbe.objeto.transform.position.z));

            if (dist <= radioRecoleccion)
            {
                ActivarEfectoOrbe(orbe.tipo);
                Destroy(orbe.objeto);
                orbesEnMapa.RemoveAt(i);
            }
        }
    }

    private void ActivarEfectoOrbe(TipoOrbe tipo)
    {
        audioEfectos.PlayOneShot(clipPowerUp);

        if (tipo == TipoOrbe.Reparacion)
        {
            Salud saludPlayer = jugador.GetComponent<Salud>();
            if (saludPlayer != null) saludPlayer.Curar(curacionPorOrbeVerde);

            if (AsaltoEstrellaManager.Instancia != null && AsaltoEstrellaManager.Instancia.FrenesiActivo && AsaltoEstrellaManager.Instancia.estrellaObjetivo != null)
            {
                if (Vector3.Distance(jugador.position, AsaltoEstrellaManager.Instancia.estrellaObjetivo.position) <= 8f)
                {
                    Salud saludEstrella = AsaltoEstrellaManager.Instancia.estrellaObjetivo.GetComponent<Salud>();
                    if (saludEstrella != null) saludEstrella.Curar(curacionPorOrbeVerde);
                }
            }
            MostrarAviso($"+{curacionPorOrbeVerde} INTEGRIDAD REPARADA", 2.0f);
        }
        else if (tipo == TipoOrbe.CalorInfinito)
        {
            estaSobrecargado = false;
            calorActual = 0f;
            ControlarArmas(true);
            timerCalorInfinito = duracionPoderes;
            MostrarAviso($"¡REFRIGERACIÓN CERO! FUEGO INFINITO ({duracionPoderes:F0}s)", 2.2f);
        }
        else if (tipo == TipoOrbe.DisparoTriple)
        {
            timerDisparoTriple = duracionPoderes;
            MostrarAviso($"¡CAÑÓN DE DISPERSIÓN TRIPLE! ({duracionPoderes:F0}s)", 2.2f);
        }
    }

    private void MostrarAviso(string texto, float duracion)
    {
        mensajeAviso = texto;
        timerMensaje = duracion;
    }

    // --- HUD DE SOBRECARGA Y PODERES ACTIVOS (ALINEADO CON PULSO EMP) ---
    void OnGUI()
    {
        if (Time.timeScale == 0f || texBlanca == null) return;

        float ancho = 240f;
        float alto = 24f;
        // Fila 1 (inferior derecha): Barra de Calor del Arma
        Rect fondo = new Rect(Screen.width - ancho - 20f, Screen.height - 48f, ancho, alto);

        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.DrawTexture(new Rect(fondo.x - 2, fondo.y - 2, ancho + 4, alto + 4), texBlanca);

        float pctCalor = Mathf.Clamp01(calorActual / 100f);
        Color colorBarra = estaSobrecargado ? new Color(1f, 0.15f, 0.15f) :
                           timerCalorInfinito > 0f ? new Color(0.1f, 0.9f, 1f) :
                           pctCalor > 0.75f ? new Color(1f, 0.45f, 0.1f) :
                                              new Color(0.95f, 0.8f, 0.15f);

        GUI.color = colorBarra;
        float anchoRelleno = timerCalorInfinito > 0f ? ancho : ancho * pctCalor;
        GUI.DrawTexture(new Rect(fondo.x, fondo.y, anchoRelleno, alto), texBlanca);

        GUI.color = Color.white;
        GUIStyle estilo = new GUIStyle(GUI.skin.label);
        estilo.alignment = TextAnchor.MiddleCenter;
        estilo.fontStyle = FontStyle.Bold;
        estilo.fontSize = 12;

        string etiquetaCalor = estaSobrecargado ? $"¡SOBRECARGA! ({timerBloqueo:F1}s)" :
                               timerCalorInfinito > 0f ? $"FUEGO INFINITO: {timerCalorInfinito:F1}s" :
                               $"CALOR ARMA: {Mathf.RoundToInt(calorActual)}%";
        GUI.Label(fondo, etiquetaCalor, estilo);

        // Fila 3 (encima de la barra del Pulso EMP): Indicador de Disparo Triple
        if (timerDisparoTriple > 0f)
        {
            Rect rTriple = new Rect(fondo.x, fondo.y - 58f, ancho, 22f);
            GUI.color = new Color(0.55f, 0.15f, 0.85f, 0.9f);
            GUI.DrawTexture(rTriple, texBlanca);
            GUI.color = Color.white;
            GUI.Label(rTriple, $"DISPARO TRIPLE: {timerDisparoTriple:F1}s", estilo);
        }

        // Aviso flotante central al recoger orbe o sobrecalentarse
        if (timerMensaje > 0f)
        {
            GUIStyle estiloAviso = new GUIStyle(GUI.skin.label);
            estiloAviso.alignment = TextAnchor.MiddleCenter;
            estiloAviso.fontStyle = FontStyle.Bold;
            estiloAviso.fontSize = 16;
            estiloAviso.normal.textColor = estaSobrecargado ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 1f, 0.85f);

            GUI.Label(new Rect(Screen.width / 2f - 250f, Screen.height - 125f, 500f, 30f), mensajeAviso, estiloAviso);
        }
    }

    private AudioClip CrearSonidoPowerUp()
    {
        int sr = 44100;
        int muestras = Mathf.CeilToInt(sr * 0.28f);
        float[] datos = new float[muestras];
        for (int i = 0; i < muestras; i++)
        {
            float t = (float)i / sr;
            float freq = Mathf.Lerp(420f, 1180f, t / 0.28f);
            datos[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * (1f - t / 0.28f) * 0.45f;
        }
        AudioClip c = AudioClip.Create("SFX_PowerUp", muestras, 1, sr, false);
        c.SetData(datos, 0);
        return c;
    }

    private AudioClip CrearSonidoSobrecarga()
    {
        int sr = 44100;
        int muestras = Mathf.CeilToInt(sr * 0.35f);
        float[] datos = new float[muestras];
        for (int i = 0; i < muestras; i++)
        {
            float t = (float)i / sr;
            float freq = Mathf.Lerp(550f, 160f, t / 0.35f);
            datos[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 6f) * 0.5f;
        }
        AudioClip c = AudioClip.Create("SFX_Sobrecarga", muestras, 1, sr, false);
        c.SetData(datos, 0);
        return c;
    }
}