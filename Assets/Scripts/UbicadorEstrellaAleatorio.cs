using UnityEngine;
using System.Collections.Generic;

public class UbicadorEstrellaAleatorio : MonoBehaviour
{
    public static UbicadorEstrellaAleatorio Instancia;

    [Header("--- REFERENCIAS ---")]
    public Transform estrellaObjetivo;
    public Transform jugador;

    [Header("--- CONFIGURACIÓN DE DISTANCIA ---")]
    public float distanciaMinima = 18f;
    public float alturaSobreSuelo = 1.4f;
    public float retrasoInicial = 0.25f;

    [Header("--- RADAR TÁCTICO EN HUD ---")]
    public bool mostrarIndicadorDistancia = true;

    // Datos públicos del mapa para que MapaTacticoManager dibuje las salas
    [HideInInspector] public List<Bounds> salasDetectadas = new List<Bounds>();
    [HideInInspector] public Bounds limitesGlobalesMapa;
    [HideInInspector] public bool estrellaUbicada = false;

    private static Texture2D texBlanca;

    void Awake()
    {
        Instancia = this;
        if (texBlanca == null)
        {
            texBlanca = new Texture2D(1, 1);
            texBlanca.SetPixel(0, 0, Color.white);
            texBlanca.Apply();
        }
    }

    void Start()
    {
        Invoke(nameof(ReubicarEstrellaLejos), retrasoInicial);
    }

    public void ReubicarEstrellaLejos()
    {
        if (estrellaObjetivo == null)
        {
            GameObject objEstrella = GameObject.Find("Estrella_Objetivo");
            if (objEstrella != null) estrellaObjetivo = objEstrella.transform;
        }

        if (jugador == null)
        {
            GameObject objPlayer = GameObject.FindGameObjectWithTag("Player");
            if (objPlayer != null) jugador = objPlayer.transform;
        }

        if (estrellaObjetivo == null || jugador == null) return;

        Vector3 posJugador = jugador.position;
        List<Vector3> puntosCandidatos = new List<Vector3>();
        salasDetectadas.Clear();

        Collider[] todosLosColliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
        bool primerLimite = true;

        foreach (Collider col in todosLosColliders)
        {
            if (col.isTrigger) continue;

            Bounds b = col.bounds;
            bool esSuperficieHorizontal = b.size.x >= 2.2f && b.size.z >= 2.2f && b.size.y <= 1.5f;
            bool esSueloPorNombre = col.name.ToLower().Contains("floor") ||
                                    col.name.ToLower().Contains("suelo") ||
                                    col.name.ToLower().Contains("room") ||
                                    col.name.ToLower().Contains("sala");

            if (esSuperficieHorizontal || esSueloPorNombre)
            {
                salasDetectadas.Add(b);
                if (primerLimite)
                {
                    limitesGlobalesMapa = b;
                    primerLimite = false;
                }
                else
                {
                    limitesGlobalesMapa.Encapsulate(b);
                }

                Vector3 centroSuelo = new Vector3(b.center.x, b.max.y + alturaSobreSuelo, b.center.z);
                if (!Physics.CheckSphere(centroSuelo, 0.6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    puntosCandidatos.Add(centroSuelo);
                }
            }
        }

        if (puntosCandidatos.Count == 0) return;

        puntosCandidatos.Sort((a, b) =>
            Vector3.Distance(posJugador, a).CompareTo(Vector3.Distance(posJugador, b)));

        List<Vector3> puntosLejanos = puntosCandidatos.FindAll(p => Vector3.Distance(posJugador, p) >= distanciaMinima);

        if (puntosLejanos.Count == 0)
        {
            int inicioTopLejanos = Mathf.Max(0, Mathf.FloorToInt(puntosCandidatos.Count * 0.7f));
            puntosLejanos = puntosCandidatos.GetRange(inicioTopLejanos, puntosCandidatos.Count - inicioTopLejanos);
        }

        Vector3 destinoElegido = puntosLejanos[Random.Range(0, puntosLejanos.Count)];
        estrellaObjetivo.position = destinoElegido;
        estrellaUbicada = true;
    }

    // --- INDICADOR EN ESQUINA SUPERIOR IZQUIERDA (SIN CHOCAR CON EL RELOJ CENTRAL) ---
    void OnGUI()
    {
        if (!mostrarIndicadorDistancia || !estrellaUbicada || estrellaObjetivo == null || jugador == null) return;
        if (Time.timeScale == 0f) return;

        float distancia = Vector3.Distance(
            new Vector3(jugador.position.x, 0, jugador.position.z),
            new Vector3(estrellaObjetivo.position.x, 0, estrellaObjetivo.position.z));

        Camera cam = Camera.main;
        string flecha = "▲";
        if (cam != null)
        {
            Vector3 dirEstrella = (estrellaObjetivo.position - jugador.position).normalized;
            Vector3 forwardCam = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            float angulo = Vector3.SignedAngle(forwardCam, dirEstrella, Vector3.up);

            if (angulo >= -22.5f && angulo < 22.5f) flecha = "▲";
            else if (angulo >= 22.5f && angulo < 67.5f) flecha = "◥";
            else if (angulo >= 67.5f && angulo < 112.5f) flecha = "▶";
            else if (angulo >= 112.5f && angulo < 157.5f) flecha = "◢";
            else if (angulo >= -67.5f && angulo < -22.5f) flecha = "◤";
            else if (angulo >= -112.5f && angulo < -67.5f) flecha = "◀";
            else if (angulo >= -157.5f && angulo < -112.5f) flecha = "◣";
            else flecha = "▼";
        }

        // Esquina superior izquierda (x = 20, y = 18) para dejar libre el centro superior
        Rect caja = new Rect(20f, 18f, 235f, 44f);

        if (texBlanca != null)
        {
            GUI.color = new Color(0.04f, 0.08f, 0.14f, 0.82f);
            GUI.DrawTexture(caja, texBlanca);
            GUI.color = Color.white;
        }

        GUIStyle estilo = new GUIStyle(GUI.skin.label);
        estilo.alignment = TextAnchor.MiddleLeft;
        estilo.fontStyle = FontStyle.Bold;
        estilo.fontSize = 13;
        estilo.normal.textColor = new Color(1f, 0.88f, 0.25f);

        GUI.Label(new Rect(caja.x + 10f, caja.y + 2f, caja.width - 14f, 22f),
            $"{flecha} ESTRELLA: {Mathf.RoundToInt(distancia)}m", estilo);

        GUIStyle estiloSub = new GUIStyle(GUI.skin.label);
        estiloSub.alignment = TextAnchor.MiddleLeft;
        estiloSub.fontSize = 11;
        estiloSub.normal.textColor = new Color(0.35f, 0.9f, 1f);
        GUI.Label(new Rect(caja.x + 10f, caja.y + 21f, caja.width - 14f, 20f),
            "[M / TAB] Ver Plano de Salas", estiloSub);
    }
}