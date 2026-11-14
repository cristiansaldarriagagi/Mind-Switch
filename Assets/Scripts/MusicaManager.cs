using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class MusicaManager : MonoBehaviour
{
    public static MusicaManager Instancia;

    [Header("--- PISTAS DE AUDIO (OPCIONAL) ---")]
    [Tooltip("Si las dejas vacías, se usará el sintetizador electrónico integrado")]
    public AudioClip musicaInicialClip;
    public AudioClip musicaFreneticaClip;

    [Header("--- VOLUMEN Y TRANSICIÓN ---")]
    [Range(0f, 1f)] public float volumenMaximo = 0.45f;
    public float duracionFundido = 1.2f;

    private AudioSource fuenteAudio;
    private bool modoFrenetico = false;
    private bool usandoSintetizador = false;

    // Variables del sintetizador procedural
    private double faseBajo = 0;
    private double faseLead = 0;
    private int pasoSecuencia = 0;
    private double tiempoPaso = 0;
    private float sampleRate = 44100f;

    // Notas en Hz (Escala Menor Épica / Cyberpunk en Re Menor)
    private readonly float[] notasIniciales = { 146.83f, 146.83f, 174.61f, 146.83f, 220.00f, 196.00f, 174.61f, 164.81f };
    private readonly float[] notasFreneticas = { 146.83f, 293.66f, 174.61f, 349.23f, 220.00f, 440.00f, 233.08f, 329.63f };

    void Awake()
    {
        if (Instancia == null) Instancia = this;
        fuenteAudio = GetComponent<AudioSource>();
        fuenteAudio.loop = true;
        fuenteAudio.playOnAwake = false;
        sampleRate = AudioSettings.outputSampleRate;
    }

    void Start()
    {
        if (musicaInicialClip != null)
        {
            usandoSintetizador = false;
            fuenteAudio.clip = musicaInicialClip;
            fuenteAudio.volume = volumenMaximo;
            fuenteAudio.Play();
        }
        else
        {
            // Arranca el sintetizador procedural si no hay clips asignados
            usandoSintetizador = true;
            fuenteAudio.volume = volumenMaximo;
            fuenteAudio.Play();
        }
    }

    // Conecta este método al evento On Supervivencia Iniciada de Estrella_Objetivo
    public void ActivarMusicaFrenetica()
    {
        if (modoFrenetico) return;
        modoFrenetico = true;
        pasoSecuencia = 0;

        if (!usandoSintetizador && musicaFreneticaClip != null)
        {
            StartCoroutine(CambiarPistaConFundido(musicaFreneticaClip));
        }
    }

    private IEnumerator CambiarPistaConFundido(AudioClip nuevaPista)
    {
        float tiempo = 0f;
        float volInicial = fuenteAudio.volume;

        // Baja suavemente la música actual
        while (tiempo < duracionFundido * 0.5f)
        {
            tiempo += Time.deltaTime;
            fuenteAudio.volume = Mathf.Lerp(volInicial, 0f, tiempo / (duracionFundido * 0.5f));
            yield return null;
        }

        fuenteAudio.Stop();
        fuenteAudio.clip = nuevaPista;
        fuenteAudio.Play();

        // Sube con fuerza la música frenética
        tiempo = 0f;
        while (tiempo < duracionFundido * 0.5f)
        {
            tiempo += Time.deltaTime;
            fuenteAudio.volume = Mathf.Lerp(0f, volumenMaximo, tiempo / (duracionFundido * 0.5f));
            yield return null;
        }
        fuenteAudio.volume = volumenMaximo;
    }

    // Generador de audio electrónico en tiempo real cuando no hay archivos MP3/WAV
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!usandoSintetizador) return;

        double bpm = modoFrenetico ? 156.0 : 108.0;
        double duracionSemicorchea = 60.0 / bpm / 2.0;
        float[] escala = modoFrenetico ? notasFreneticas : notasIniciales;

        for (int i = 0; i < data.Length; i += channels)
        {
            tiempoPaso += 1.0 / sampleRate;
            if (tiempoPaso >= duracionSemicorchea)
            {
                tiempoPaso -= duracionSemicorchea;
                pasoSecuencia = (pasoSecuencia + 1) % escala.Length;
            }

            float freqLead = escala[pasoSecuencia];
            float freqBajo = escala[pasoSecuencia % 4] * 0.5f;

            faseLead += (freqLead * 2.0 * Mathf.PI) / sampleRate;
            faseBajo += (freqBajo * 2.0 * Mathf.PI) / sampleRate;

            // Envolvente rítmica percusiva en cada pulso
            float env = Mathf.Exp((float)(-tiempoPaso * (modoFrenetico ? 14.0 : 7.0)));

            // Onda cuadrada/sierra sintetizada para bajo y arpegio
            float bajo = Mathf.Sign(Mathf.Sin((float)faseBajo)) * 0.25f * env;
            float lead = (float)((faseLead % (2.0 * Mathf.PI)) / Mathf.PI - 1.0) * (modoFrenetico ? 0.22f : 0.12f) * env;

            // Pulso de bombo electrónico en los tiempos pares
            float kick = (pasoSecuencia % 2 == 0) ? Mathf.Sin((float)(tiempoPaso * 280.0)) * Mathf.Exp((float)(-tiempoPaso * 28.0)) * (modoFrenetico ? 0.45f : 0.25f) : 0f;

            float muestra = Mathf.Clamp((bajo + lead + kick) * volumenMaximo, -0.8f, 0.8f);

            for (int c = 0; c < channels; c++)
            {
                data[i + c] = muestra;
            }
        }
    }
}