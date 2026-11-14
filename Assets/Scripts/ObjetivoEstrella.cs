using UnityEngine;
using UnityEngine.Events;

public class ObjetivoEstrella : MonoBehaviour
{
    [Header("--- TIEMPOS DE MISIÓN (En Segundos) ---")]
    [Tooltip("Ej: 240 = 4 minutos | 600 = 10 minutos")]
    public float tiempoParaLlegar = 240f;
    [Tooltip("Tiempo que debes sobrevivir tras tocar la estrella (Ej: 60 = 1 minuto)")]
    public float tiempoSupervivencia = 60f;

    [Header("--- VISUAL Y EVENTOS ---")]
    public float velocidadGiro = 90f;
    public UnityEvent onSupervivenciaIniciada;
    public UnityEvent onVictoria;
    public UnityEvent onDerrota;

    private bool estrellaAlcanzada = false;
    private bool juegoTerminado = false;
    private float tiempoRestante;

    void Start()
    {
        tiempoRestante = tiempoParaLlegar;
    }

    void Update()
    {
        if (juegoTerminado) return;

        transform.Rotate(Vector3.up, velocidadGiro * Time.deltaTime, Space.World);
        tiempoRestante -= Time.deltaTime;

        if (tiempoRestante <= 0f)
        {
            tiempoRestante = 0f;
            juegoTerminado = true;

            if (estrellaAlcanzada)
            {
                Debug.Log("¡VICTORIA! Sobreviviste y aseguraste la Estrella.");
                onVictoria?.Invoke();
            }
            else
            {
                Debug.Log("DERROTA: Se agotó el tiempo para llegar a la Estrella.");
                onDerrota?.Invoke();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (estrellaAlcanzada || juegoTerminado) return;

        if (other.CompareTag("Player"))
        {
            estrellaAlcanzada = true;
            tiempoRestante = tiempoSupervivencia;
            Debug.Log($"¡ESTRELLA ALCANZADA! Resiste {tiempoSupervivencia} segundos.");
            onSupervivenciaIniciada?.Invoke();
        }
    }

    void OnGUI()
    {
        int minutos = Mathf.FloorToInt(tiempoRestante / 60f);
        int segundos = Mathf.FloorToInt(tiempoRestante % 60f);
        string reloj = $"{minutos:00}:{segundos:00}";

        GUIStyle estilo = new GUIStyle(GUI.skin.box);
        estilo.fontSize = 22;
        estilo.fontStyle = FontStyle.Bold;
        estilo.normal.textColor = estrellaAlcanzada ? Color.yellow : Color.white;

        string mensaje = estrellaAlcanzada
            ? $"¡DEFIENDE LA ESTRELLA! {reloj}"
            : $"OBJETIVO ESTRELLA: {reloj}";

        GUI.Box(new Rect(Screen.width / 2f - 210, 20, 420, 45), mensaje, estilo);
    }
}