using UnityEngine;
using UnityEngine.Events;

public class Salud : MonoBehaviour, IDamageable
{
    [Header("Configuración de Vida")]
    public float vidaMaxima = 100f;
    public bool esInmune = false;
    public bool destruirAlMorir = true;
    public float retrasoDestruccion = 0.1f;
    [Tooltip("Marca esto en la Estrella para que su destrucción cause Derrota")]
    public bool esObjetivoCritico = false;

    [Header("Barra de Vida Visual")]
    public bool mostrarBarra = true;
    public float alturaBarraEnemigo = 1.2f;

    [Header("Eventos para UI / Efectos")]
    public UnityEvent<float, float> onVidaCambiada;
    public UnityEvent onMorir;

    private float vidaActual;
    private bool estaMuerto = false;
    private static Texture2D texturaBlanca;

    public float VidaActual => vidaActual;
    public float PorcentajeVida => vidaMaxima > 0 ? Mathf.Clamp01(vidaActual / vidaMaxima) : 0f;

    void Awake()
    {
        vidaActual = vidaMaxima;
        if (texturaBlanca == null)
        {
            texturaBlanca = new Texture2D(1, 1);
            texturaBlanca.SetPixel(0, 0, Color.white);
            texturaBlanca.Apply();
        }
    }

    public void RecibirDano(float cantidad) => RecibirDanio(cantidad);
    public void TomarDanio(float cantidad) => RecibirDanio(cantidad);
    public void TomarDano(float cantidad) => RecibirDanio(cantidad);
    public void TakeDamage(float cantidad) => RecibirDanio(cantidad);
    public void ApplyDamage(float cantidad) => RecibirDanio(cantidad);

    public void RecibirDanio(float cantidad)
    {
        if (estaMuerto || esInmune || cantidad <= 0f) return;

        vidaActual = Mathf.Clamp(vidaActual - cantidad, 0f, vidaMaxima);
        onVidaCambiada?.Invoke(vidaActual, vidaMaxima);

        if (vidaActual <= 0f)
        {
            Morir();
        }
    }

    public void Curar(float cantidad)
    {
        if (estaMuerto || cantidad <= 0f) return;

        vidaActual = Mathf.Clamp(vidaActual + cantidad, 0f, vidaMaxima);
        onVidaCambiada?.Invoke(vidaActual, vidaMaxima);
    }

    private void Morir()
    {
        if (estaMuerto) return;
        estaMuerto = true;
        onMorir?.Invoke();

        if (SFXManager.Instancia != null)
        {
            SFXManager.Instancia.ReproducirExplosion(transform.position);
        }

        if (GameFlowManager.Instancia != null)
        {
            if (CompareTag("Player") || esObjetivoCritico)
            {
                GameFlowManager.Instancia.MostrarDerrota();
            }
            else
            {
                GameFlowManager.Instancia.RegistrarEnemigoEliminado();
                // Genera posible recolectable en la posición del enemigo derrotado
                if (SobrecargaYLootManager.Instancia != null)
                {
                    SobrecargaYLootManager.Instancia.IntentarSoltarLoot(transform.position);
                }
            }
        }

        if (destruirAlMorir)
        {
            Destroy(gameObject, retrasoDestruccion);
        }
    }

    void OnGUI()
    {
        if (!mostrarBarra || estaMuerto || texturaBlanca == null || Time.timeScale == 0f) return;

        if (CompareTag("Player"))
        {
            DibujarBarraJugador();
        }
        else if (esObjetivoCritico)
        {
            DibujarBarraEstrellaHUD();
        }
        else
        {
            DibujarBarraFlotante();
        }
    }

    private void DibujarBarraJugador()
    {
        float ancho = 260f;
        float alto = 28f;
        Rect fondo = new Rect(20f, Screen.height - 48f, ancho, alto);

        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(new Rect(fondo.x - 2, fondo.y - 2, ancho + 4, alto + 4), texturaBlanca);

        GUI.color = PorcentajeVida > 0.5f ? new Color(0.15f, 0.85f, 0.35f) :
                    PorcentajeVida > 0.25f ? new Color(0.95f, 0.75f, 0.1f) :
                                             new Color(0.9f, 0.2f, 0.2f);
        GUI.DrawTexture(new Rect(fondo.x, fondo.y, ancho * PorcentajeVida, alto), texturaBlanca);

        GUI.color = Color.white;
        GUIStyle estilo = new GUIStyle(GUI.skin.label);
        estilo.alignment = TextAnchor.MiddleCenter;
        estilo.fontStyle = FontStyle.Bold;
        estilo.fontSize = 14;
        GUI.Label(fondo, $"INTEGRIDAD MECH: {Mathf.CeilToInt(vidaActual)} / {Mathf.CeilToInt(vidaMaxima)}", estilo);
    }

    private void DibujarBarraEstrellaHUD()
    {
        if (esInmune) return;

        float ancho = 260f;
        float alto = 26f;
        Rect fondo = new Rect(20f, Screen.height - 84f, ancho, alto);

        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(new Rect(fondo.x - 2, fondo.y - 2, ancho + 4, alto + 4), texturaBlanca);

        GUI.color = new Color(1f, 0.78f, 0.12f);
        GUI.DrawTexture(new Rect(fondo.x, fondo.y, ancho * PorcentajeVida, alto), texturaBlanca);

        GUI.color = Color.white;
        GUIStyle estilo = new GUIStyle(GUI.skin.label);
        estilo.alignment = TextAnchor.MiddleCenter;
        estilo.fontStyle = FontStyle.Bold;
        estilo.fontSize = 13;
        GUI.Label(fondo, $"NÚCLEO ESTRELLA: {Mathf.CeilToInt(vidaActual)} / {Mathf.CeilToInt(vidaMaxima)}", estilo);
    }

    private void DibujarBarraFlotante()
    {
        Camera cam = Camera.main != null ? Camera.main : Camera.current;
        if (cam == null)
        {
            Camera[] camaras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera c in camaras)
            {
                if (c.isActiveAndEnabled) { cam = c; break; }
            }
        }
        if (cam == null) return;

        Vector3 posMundo = transform.position + Vector3.up * alturaBarraEnemigo;
        Vector3 posPantalla = cam.WorldToScreenPoint(posMundo);

        if (posPantalla.z <= 0f || posPantalla.z > 45f) return;

        float x = posPantalla.x;
        float y = Screen.height - posPantalla.y;
        float ancho = 80f;
        float alto = 10f;

        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(new Rect(x - ancho / 2f - 2, y - 2, ancho + 4, alto + 4), texturaBlanca);

        GUI.color = new Color(0.95f, 0.2f, 0.2f);
        GUI.DrawTexture(new Rect(x - ancho / 2f, y, ancho * PorcentajeVida, alto), texturaBlanca);

        GUI.color = Color.white;
    }
}