using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instancia;

    [Header("--- CLIPS OPCIONALES (Si los dejas vacíos usa sonido sintetizado) ---")]
    public AudioClip clipDisparoMech;
    public AudioClip clipPasoMech;
    public AudioClip clipPasoEnemigo;
    public AudioClip clipImpactoBala;
    public AudioClip clipExplosion;

    [Header("--- VOLÚMENES ---")]
    [Range(0f, 1f)] public float volDisparo = 0.45f;
    [Range(0f, 1f)] public float volPasosMech = 0.35f;
    [Range(0f, 1f)] public float volPasosEnemigo = 0.25f;
    [Range(0f, 1f)] public float volImpacto = 0.4f;
    [Range(0f, 1f)] public float volExplosion = 0.6f;

    // Clips generados automáticamente si no asignas archivos
    private AudioClip sfxDisparoAuto;
    private AudioClip sfxPasoMechAuto;
    private AudioClip sfxPasoEnemigoAuto;
    private AudioClip sfxImpactoAuto;
    private AudioClip sfxExplosionAuto;

    void Awake()
    {
        if (Instancia == null) Instancia = this;
        GenerarSonidosSintetizados();
    }

    // --- MÉTODOS PÚBLICOS PARA LLAMAR DESDE CUALQUIER SCRIPT ---
    public void ReproducirDisparo(Vector3 posicion)
    {
        Reproducir3D(clipDisparoMech != null ? clipDisparoMech : sfxDisparoAuto, posicion, volDisparo, Random.Range(0.95f, 1.08f));
    }

    public void ReproducirPasoMech(Vector3 posicion)
    {
        Reproducir3D(clipPasoMech != null ? clipPasoMech : sfxPasoMechAuto, posicion, volPasosMech, Random.Range(0.88f, 1.05f));
    }

    public void ReproducirPasoEnemigo(Vector3 posicion)
    {
        Reproducir3D(clipPasoEnemigo != null ? clipPasoEnemigo : sfxPasoEnemigoAuto, posicion, volPasosEnemigo, Random.Range(0.9f, 1.15f));
    }

    public void ReproducirImpacto(Vector3 posicion)
    {
        Reproducir3D(clipImpactoBala != null ? clipImpactoBala : sfxImpactoAuto, posicion, volImpacto, Random.Range(0.9f, 1.2f));
    }

    public void ReproducirExplosion(Vector3 posicion)
    {
        Reproducir3D(clipExplosion != null ? clipExplosion : sfxExplosionAuto, posicion, volExplosion, Random.Range(0.85f, 1.05f));
    }

    private void Reproducir3D(AudioClip clip, Vector3 pos, float volumen, float pitch)
    {
        if (clip == null || Time.timeScale == 0f) return;

        GameObject go = new GameObject("SFX_Temp");
        go.transform.position = pos;
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volumen;
        src.pitch = pitch;
        src.spatialBlend = 0.85f; // Audio 3D para sentir de qué pasillo vienen los enemigos
        src.minDistance = 4f;
        src.maxDistance = 32f;
        src.Play();
        Destroy(go, clip.length + 0.1f);
    }

    // --- GENERADOR PROCEDURAL DE EFECTOS SCI-FI ---
    private void GenerarSonidosSintetizados()
    {
        int sr = 44100;

        // 1. Disparo de plasma/cañón pesado (0.16s)
        sfxDisparoAuto = CrearClip("SFX_Disparo", sr, 0.16f, (t, dur) =>
        {
            float freq = Mathf.Lerp(520f, 95f, t / dur);
            float env = Mathf.Exp(-t * 22f);
            float ruido = (Random.value * 2f - 1f) * 0.25f;
            return (Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t)) * 0.5f + ruido) * env;
        });

        // 2. Paso hidráulico grave del Mech (0.14s)
        sfxPasoMechAuto = CrearClip("SFX_PasoMech", sr, 0.14f, (t, dur) =>
        {
            float golpe = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(110f, 38f, t / dur) * t);
            float servo = (Random.value * 2f - 1f) * 0.3f * Mathf.Exp(-t * 35f);
            return (golpe * 0.8f + servo) * Mathf.Exp(-t * 24f);
        });

        // 3. Paso metálico rápido de enemigo (0.08s)
        sfxPasoEnemigoAuto = CrearClip("SFX_PasoEnemigo", sr, 0.08f, (t, dur) =>
        {
            float clic = Mathf.Sin(2f * Mathf.PI * 340f * t) * 0.4f;
            float metal = (Random.value * 2f - 1f) * 0.6f;
            return (clic + metal) * Mathf.Exp(-t * 48f);
        });

        // 4. Impacto de bala metálico (0.10s)
        sfxImpactoAuto = CrearClip("SFX_Impacto", sr, 0.10f, (t, dur) =>
        {
            float chispa = Mathf.Sin(2f * Mathf.PI * 980f * t) * 0.35f;
            float golpe = (Random.value * 2f - 1f) * 0.7f;
            return (chispa + golpe) * Mathf.Exp(-t * 38f);
        });

        // 5. Explosión al destruir enemigo (0.45s)
        sfxExplosionAuto = CrearClip("SFX_Explosion", sr, 0.45f, (t, dur) =>
        {
            float sub = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(90f, 25f, t / dur) * t) * 0.6f;
            float ruido = (Random.value * 2f - 1f) * 0.8f;
            return (sub + ruido) * Mathf.Exp(-t * 7.5f);
        });
    }

    private AudioClip CrearClip(string nombre, int sampleRate, float duracion, System.Func<float, float, float> formula)
    {
        int muestras = Mathf.CeilToInt(sampleRate * duracion);
        float[] datos = new float[muestras];
        for (int i = 0; i < muestras; i++)
        {
            float t = (float)i / sampleRate;
            datos[i] = Mathf.Clamp(formula(t, duracion), -0.95f, 0.95f);
        }
        AudioClip clip = AudioClip.Create(nombre, muestras, 1, sampleRate, false);
        clip.SetData(datos, 0);
        return clip;
    }
}