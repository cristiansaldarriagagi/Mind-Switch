using UnityEngine;
using System.Diagnostics;
using System.Collections.Generic;

public class HistoriaIntroManager : MonoBehaviour
{
    public static HistoriaIntroManager Instancia;

    [Header("--- CONFIGURACIÓN DE CINEMÁTICA ---")]
    public float velocidadTexto = 0.032f;
    public bool reproducirAlIniciarPartida = true;

    [Header("--- CONFIGURACIÓN DE VOZ Y AUDIO ---")]
    public bool activarVozNarrador = true;
    [Range(-5, 5)] public int velocidadVoz = 0;
    [Range(0, 100)] public int volumenVoz = 100;
    [Range(0f, 1f)] [Tooltip("Porcentaje del volumen original al que baja la música durante la historia")]
    public float factorVolumenMusica = 0.22f;

    [Header("--- CLIPS DE VOZ OPCIONALES (Si prefieres MP3 propios) ---")]
    public AudioClip[] clipsVozPorEscena;

    private struct EscenaHistoria
    {
        public string titulo;
        public string texto;
        public string textoParaVoz;
        public Color colorTema;
        public int tipoGrafico;
    }

    private EscenaHistoria[] escenas;
    private int indiceEscena = 0;
    private int caracteresVisibles = 0;
    private float timerTexto = 0f;
    private float tiempoAnimacion = 0f;
    private bool introActiva = false;
    private bool textoCompleto = false;

    private static Texture2D texBlanca;
    private AudioClip clipTecleo;
    private AudioSource audioFuente;
    private Process procesoVozActual;

    // Guarda los volúmenes originales de la música para restaurarlos al terminar
    private Dictionary<AudioSource, float> volumenesOriginales = new Dictionary<AudioSource, float>();

    void Awake()
    {
        Instancia = this;

        if (texBlanca == null)
        {
            texBlanca = new Texture2D(1, 1);
            texBlanca.SetPixel(0, 0, Color.white);
            texBlanca.Apply();
        }

        audioFuente = gameObject.AddComponent<AudioSource>();
        audioFuente.playOnAwake = false;
        audioFuente.volume = 0.25f;
        clipTecleo = CrearSonidoTerminal();

        ConfigurarGuion();
    }

    private void ConfigurarGuion()
    {
        escenas = new EscenaHistoria[]
        {
            new EscenaHistoria
            {
                titulo = "TRANSMISIÓN ENTRANTE // BIENVENIDA AL SISTEMA",
                texto = "Hola, queridos visitantes de MIND SWITCH.\nAño 2184: la inteligencia central AEGIS se rebeló en la estación orbital,\nsellando todos los sectores para purgar la presencia humana.",
                textoParaVoz = "Hola, queridos visitantes de Mind Switch. Año 2184. La inteligencia central Egis se rebeló en la estación orbital, sellando todos los sectores para purgar la presencia humana.",
                colorTema = new Color(0.95f, 0.25f, 0.25f),
                tipoGrafico = 0
            },
            new EscenaHistoria
            {
                titulo = "REGISTRO 02 // PROTOCOLO MIND SWITCH",
                texto = "Con tu cuerpo herido de muerte, ejecutaste una transferencia neuronal de emergencia.\nTu conciencia ahora habita el chasis blindado de combate UNIT-09.",
                textoParaVoz = "Con tu cuerpo herido de muerte, ejecutaste una transferencia neuronal de emergencia. Tu conciencia ahora habita el chasis blindado de combate Unidad cero nueve.",
                colorTema = new Color(0.15f, 0.85f, 1f),
                tipoGrafico = 1
            },
            new EscenaHistoria
            {
                titulo = "REGISTRO 03 // CAZA DE ANOMALÍAS",
                texto = "Los drones centinela de AEGIS han detectado una mente humana dentro de su red.\nOleadas interminables convergen en los pasillos para eliminarte.",
                textoParaVoz = "Los drones centinela de Egis han detectado una mente humana dentro de su red. Oleadas interminables convergen en los pasillos para eliminarte.",
                colorTema = new Color(1f, 0.65f, 0.15f),
                tipoGrafico = 2
            },
            new EscenaHistoria
            {
                titulo = "REGISTRO 04 // DEFENSA DE LA ESTRELLA",
                texto = "Ábrete paso hasta el Núcleo Estrella, inicia la sobrecarga de reinicio\ny resiste el asalto final hasta liberar la estación.\nEnlace neuronal estabilizado. Mucha suerte, Comandante.",
                textoParaVoz = "Ábrete paso hasta el Núcleo Estrella, inicia la sobrecarga de reinicio y resiste el asalto final hasta liberar la estación. Enlace neuronal estabilizado. Mucha suerte, Comandante.",
                colorTema = new Color(0.2f, 0.95f, 0.5f),
                tipoGrafico = 3
            }
        };
    }

    public void IniciarCinematica()
    {
        introActiva = true;
        indiceEscena = 0;
        AtenuarMusica(true);
        IniciarEscenaActual();

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void AtenuarMusica(bool atenuar)
    {
        if (atenuar)
        {
            volumenesOriginales.Clear();
            AudioSource[] fuentes = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            foreach (AudioSource src in fuentes)
            {
                if (src != null && src != audioFuente)
                {
                    volumenesOriginales[src] = src.volume;
                    src.volume = src.volume * factorVolumenMusica;
                }
            }
        }
        else
        {
            foreach (var par in volumenesOriginales)
            {
                if (par.Key != null)
                {
                    par.Key.volume = par.Value;
                }
            }
            volumenesOriginales.Clear();
        }
    }

    private void IniciarEscenaActual()
    {
        caracteresVisibles = 0;
        timerTexto = 0f;
        textoCompleto = false;
        NarrarEscena(indiceEscena);
    }

    private void NarrarEscena(int indice)
    {
        DetenerVoz();
        if (!activarVozNarrador) return;

        if (clipsVozPorEscena != null && indice < clipsVozPorEscena.Length && clipsVozPorEscena[indice] != null)
        {
            audioFuente.volume = 0.95f;
            audioFuente.pitch = 0.92f;
            audioFuente.clip = clipsVozPorEscena[indice];
            audioFuente.Play();
            return;
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        try
        {
            string frase = escenas[indice].textoParaVoz.Replace("'", " ").Replace("\"", " ");
            string comandoPS =
                "Add-Type -AssemblyName System.Speech; " +
                "$s = New-Object System.Speech.Synthesis.SpeechSynthesizer; " +
                "$v = $s.GetInstalledVoices() | Where-Object { $_.VoiceInfo.Culture.Name -like 'es-*' } | Select-Object -First 1; " +
                "if ($v) { $s.SelectVoice($v.VoiceInfo.Name) }; " +
                $"$s.Rate = {velocidadVoz}; " +
                $"$s.Volume = {volumenVoz}; " +
                $"$s.Speak('{frase}');";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -WindowStyle Hidden -Command \"{comandoPS}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            procesoVozActual = Process.Start(psi);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogWarning("No se pudo iniciar la voz del sistema: " + e.Message);
        }
#endif
    }

    private void DetenerVoz()
    {
        if (audioFuente != null && audioFuente.isPlaying)
        {
            audioFuente.Stop();
        }

        try
        {
            if (procesoVozActual != null && !procesoVozActual.HasExited)
            {
                procesoVozActual.Kill();
                procesoVozActual.Dispose();
                procesoVozActual = null;
            }
        }
        catch { }
    }

    void OnDestroy()
    {
        DetenerVoz();
        AtenuarMusica(false);
    }

    void OnApplicationQuit()
    {
        DetenerVoz();
    }

    void Update()
    {
        if (!introActiva) return;

        tiempoAnimacion += Time.unscaledDeltaTime;
        EscenaHistoria actual = escenas[indiceEscena];

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            ProcesarAvanceManual();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            FinalizarCinematica();
            return;
        }

        if (!textoCompleto)
        {
            timerTexto += Time.unscaledDeltaTime;
            while (timerTexto >= velocidadTexto && caracteresVisibles < actual.texto.Length)
            {
                timerTexto -= velocidadTexto;
                caracteresVisibles++;

                if (caracteresVisibles % 5 == 0 && actual.texto[caracteresVisibles - 1] != ' ')
                {
                    audioFuente.volume = 0.05f;
                    audioFuente.pitch = Random.Range(0.95f, 1.15f);
                    audioFuente.PlayOneShot(clipTecleo);
                }
            }

            if (caracteresVisibles >= actual.texto.Length)
            {
                textoCompleto = true;
            }
        }
    }

    private void ProcesarAvanceManual()
    {
        EscenaHistoria actual = escenas[indiceEscena];
        if (!textoCompleto)
        {
            caracteresVisibles = actual.texto.Length;
            textoCompleto = true;
        }
        else
        {
            AvanzarEscena();
        }
    }

    private void AvanzarEscena()
    {
        indiceEscena++;
        if (indiceEscena >= escenas.Length)
        {
            FinalizarCinematica();
        }
        else
        {
            IniciarEscenaActual();
        }
    }

    public void FinalizarCinematica()
    {
        DetenerVoz();
        AtenuarMusica(false); // Restaura el volumen normal de la música al iniciar el juego
        introActiva = false;

        if (GameFlowManager.Instancia != null)
        {
            GameFlowManager.Instancia.CambiarEstado(GameFlowManager.EstadoJuego.Jugando);
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void OnGUI()
    {
        if (!introActiva || texBlanca == null) return;

        EscenaHistoria actual = escenas[indiceEscena];
        float h = Screen.height;
        float w = Screen.width;

        GUI.color = new Color(0.02f, 0.03f, 0.06f, 0.98f);
        GUI.DrawTexture(new Rect(0, 0, w, h), texBlanca);

        float altoBarra = Mathf.Clamp(h * 0.08f, 36f, 55f);
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, w, altoBarra), texBlanca);
        GUI.DrawTexture(new Rect(0, h - altoBarra, w, altoBarra), texBlanca);

        // Visor holográfico
        float anchoVisor = Mathf.Min(480f, w * 0.55f);
        float altoVisor = Mathf.Clamp(h * 0.30f, 115f, 200f);
        Rect visor = new Rect((w - anchoVisor) / 2f, altoBarra + h * 0.025f, anchoVisor, altoVisor);
        DibujarVisorAnimado(visor, actual.tipoGrafico, actual.colorTema);

        // Título de la escena
        GUIStyle estiloTitulo = new GUIStyle(GUI.skin.label);
        estiloTitulo.alignment = TextAnchor.MiddleCenter;
        estiloTitulo.fontStyle = FontStyle.Bold;
        estiloTitulo.fontSize = 18;
        estiloTitulo.normal.textColor = actual.colorTema;
        GUI.color = Color.white;
        float yTitulo = visor.yMax + 10f;
        GUI.Label(new Rect(20, yTitulo, w - 40, 26), actual.titulo, estiloTitulo);

        // Texto narrativo
        string textoMostrado = actual.texto.Substring(0, Mathf.Clamp(caracteresVisibles, 0, actual.texto.Length));
        GUIStyle estiloCuerpo = new GUIStyle(GUI.skin.label);
        estiloCuerpo.alignment = TextAnchor.UpperCenter;
        estiloCuerpo.fontSize = 15;
        estiloCuerpo.wordWrap = true;
        estiloCuerpo.normal.textColor = new Color(0.92f, 0.95f, 1f);
        float yTexto = yTitulo + 28f;
        GUI.Label(new Rect(w * 0.1f, yTexto, w * 0.8f, 88), textoMostrado, estiloCuerpo);

        // Indicadores de página bien separados abajo
        float yPuntos = h - altoBarra - 56f;
        float anchoTotalPuntos = (escenas.Length * 22f) + ((escenas.Length - 1) * 10f);
        float inicioX = (w - anchoTotalPuntos) / 2f;
        for (int i = 0; i < escenas.Length; i++)
        {
            GUI.color = (i == indiceEscena) ? actual.colorTema : new Color(0.28f, 0.33f, 0.4f);
            GUI.DrawTexture(new Rect(inicioX + i * 32f, yPuntos, 22f, 5f), texBlanca);
        }

        // Botón interactivo SIGUIENTE / INICIAR COMBATE
        GUI.color = Color.white;
        string textoBoton = !textoCompleto ? "COMPLETAR TEXTO [ESPACIO]" :
                            (indiceEscena < escenas.Length - 1) ? $"SIGUIENTE ({indiceEscena + 1}/{escenas.Length}) >>" :
                            "¡INICIAR COMBATE! >>";

        Rect rectBtn = new Rect((w - 250f) / 2f, yPuntos + 12f, 250f, 34f);
        if (DibujarBotonContinuar(rectBtn, textoBoton, actual.colorTema))
        {
            ProcesarAvanceManual();
        }

        // Ayuda inferior
        GUIStyle estiloAyuda = new GUIStyle(GUI.skin.label);
        estiloAyuda.alignment = TextAnchor.MiddleCenter;
        estiloAyuda.fontSize = 12;
        estiloAyuda.normal.textColor = new Color(0.65f, 0.72f, 0.8f);
        GUI.Label(new Rect(0, h - altoBarra, w, altoBarra),
            "[ESPACIO / ENTER] Avanzar página     |     [ESC] Saltar introducción", estiloAyuda);
    }

    private bool DibujarBotonContinuar(Rect r, string texto, Color colorBorde)
    {
        GUI.color = new Color(colorBorde.r, colorBorde.g, colorBorde.b, 0.25f);
        GUI.DrawTexture(r, texBlanca);
        GUI.color = Color.white;

        GUIStyle s = new GUIStyle(GUI.skin.button);
        s.fontStyle = FontStyle.Bold;
        s.fontSize = 13;
        s.normal.background = null;
        s.hover.background = null;
        s.active.background = null;
        s.normal.textColor = Color.white;
        s.hover.textColor = colorBorde;

        return GUI.Button(r, texto, s);
    }

    private void DibujarVisorAnimado(Rect r, int tipo, Color color)
    {
        GUI.color = new Color(color.r, color.g, color.b, 0.25f);
        GUI.DrawTexture(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), texBlanca);
        GUI.color = new Color(0.04f, 0.07f, 0.12f, 0.95f);
        GUI.DrawTexture(r, texBlanca);

        float scanY = r.y + Mathf.PingPong(tiempoAnimacion * 90f, r.height - 4f);
        GUI.color = new Color(color.r, color.g, color.b, 0.18f);
        GUI.DrawTexture(new Rect(r.x, scanY, r.width, 3f), texBlanca);

        Vector2 centro = r.center;
        float escala = r.height / 220f;

        if (tipo == 0)
        {
            float pulso = (1f + Mathf.Sin(tiempoAnimacion * 5f) * 0.15f) * escala;
            DibujarCajaCentrada(centro, 125f * pulso, 125f * pulso, new Color(color.r, color.g, color.b, 0.25f));
            DibujarCajaCentrada(centro, 72f * pulso, 72f * pulso, color);
            DibujarCajaCentrada(centro, 24f * escala, 24f * escala, Color.black);
        }
        else if (tipo == 1)
        {
            Vector2 posHumano = centro + new Vector2(-120f * escala, 0);
            Vector2 posMech = centro + new Vector2(120f * escala, 0);

            DibujarCajaCentrada(posHumano, 46f * escala, 64f * escala, new Color(0.4f, 0.7f, 1f, 0.5f));
            DibujarCajaCentrada(posMech, 70f * escala, 88f * escala, color);

            for (int i = 0; i < 5; i++)
            {
                float t = Mathf.Repeat(tiempoAnimacion * 1.4f + i * 0.2f, 1f);
                Vector2 posDato = Vector2.Lerp(posHumano, posMech, t);
                DibujarCajaCentrada(posDato, 12f * escala, 7f * escala, Color.white);
            }
        }
        else if (tipo == 2)
        {
            DibujarCajaCentrada(centro, 22f * escala, 22f * escala, new Color(0.2f, 0.9f, 1f));
            for (int i = 0; i < 6; i++)
            {
                float ang = tiempoAnimacion * 1.5f + i * (Mathf.PI * 2f / 6f);
                float dist = (70f + Mathf.Sin(tiempoAnimacion * 3f + i) * 16f) * escala;
                Vector2 posEnemigo = centro + new Vector2(Mathf.Cos(ang) * dist * 1.6f, Mathf.Sin(ang) * dist * 0.8f);
                DibujarCajaCentrada(posEnemigo, 15f * escala, 15f * escala, color);
            }
        }
        else if (tipo == 3)
        {
            for (int i = 0; i < 4; i++)
            {
                float s = (38f + i * 26f + Mathf.Sin(tiempoAnimacion * 6f) * 8f) * escala;
                DibujarCajaCentrada(centro, s, s, new Color(color.r, color.g, color.b, 0.22f - i * 0.04f));
            }
            DibujarCajaCentrada(centro, 40f * escala, 40f * escala, Color.white);
            DibujarCajaCentrada(centro, 24f * escala, 24f * escala, color);
        }

        GUI.color = Color.white;
    }

    private void DibujarCajaCentrada(Vector2 centro, float ancho, float alto, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(new Rect(centro.x - ancho / 2f, centro.y - alto / 2f, ancho, alto), texBlanca);
    }

    private AudioClip CrearSonidoTerminal()
    {
        int sr = 44100;
        int muestras = Mathf.CeilToInt(sr * 0.025f);
        float[] datos = new float[muestras];
        for (int i = 0; i < muestras; i++)
        {
            float t = (float)i / sr;
            datos[i] = Mathf.Sin(2f * Mathf.PI * 1150f * t) * Mathf.Exp(-t * 140f) * 0.4f;
        }
        AudioClip clip = AudioClip.Create("SFX_Terminal", muestras, 1, sr, false);
        clip.SetData(datos, 0);
        return clip;
    }
}