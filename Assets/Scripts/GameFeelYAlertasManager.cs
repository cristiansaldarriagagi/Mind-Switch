using UnityEngine;
using System.Diagnostics;

public class GameFeelYAlertasManager : MonoBehaviour
{
    public static GameFeelYAlertasManager Instancia;

    [Header("--- SACUDIDA DE CÁMARA (SCREEN SHAKE) ---")]
    public bool activarScreenShake = true;
    public float intensidadDisparo = 0.045f;
    public float intensidadDanoRecibido = 0.32f;
    public float intensidadEMP = 0.38f;

    [Header("--- DESTELLO DE DAÑO EN PANTALLA ---")]
    public bool activarDestelloDano = true;

    [Header("--- ALERTAS DE VOZ IA EN COMBATE ---")]
    public bool activarAlertasVoz = true;
    public float enfriamientoMinimoEntreAlertas = 8f;

    private Camera camaraPrincipal;
    private Vector3 offsetShakeAnterior = Vector3.zero;
    private float fuerzaShakeActual = 0f;
    private float decaimientoShake = 2.4f;

    private float alphaDestelloRojo = 0f;
    private static Texture2D texBlanca;

    private Salud saludJugador;
    private Salud saludEstrella;
    private float ultimaVidaJugador = -1f;
    private float ultimaVidaEstrella = -1f;

    // Control de alertas de voz para no repetirlas seguido
    private float tiempoUltimaAlerta = -20f;
    private bool alertaFrenesiEmitida = false;
    private bool alertaVidaCriticaEmitida = false;
    private float timerAlertaEstrella = 0f;
    private Process procesoVozAlerta;

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
        camaraPrincipal = Camera.main;

        GameObject objP = GameObject.FindGameObjectWithTag("Player");
        if (objP != null)
        {
            saludJugador = objP.GetComponent<Salud>();
            if (saludJugador != null) ultimaVidaJugador = saludJugador.VidaActual;
        }

        GameObject objE = GameObject.Find("Estrella_Objetivo");
        if (objE != null)
        {
            saludEstrella = objE.GetComponent<Salud>();
            if (saludEstrella != null) ultimaVidaEstrella = saludEstrella.VidaActual;
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f)
        {
            RestaurarOffsetCamara();
            return;
        }

        if (camaraPrincipal == null) camaraPrincipal = Camera.main;

        if (timerAlertaEstrella > 0f) timerAlertaEstrella -= Time.deltaTime;
        if (alphaDestelloRojo > 0f) alphaDestelloRojo = Mathf.Max(0f, alphaDestelloRojo - Time.deltaTime * 1.6f);

        // 1. Micro-vibración al disparar
        if (Input.GetMouseButton(0))
        {
            AnadirSacudida(intensidadDisparo * Time.deltaTime * 18f);
        }

        // 2. Sacudida al detonar Pulso EMP
        if ((Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1)) && PulsoEMPManager.Instancia != null)
        {
            AnadirSacudida(intensidadEMP);
        }

        // 3. Monitorea daño al Jugador y alertas de integridad crítica
        MonitorearJugador();

        // 4. Monitorea inicio de Frenesí y ataques a la Estrella
        MonitorearEstrella();
    }

    private void MonitorearJugador()
    {
        if (saludJugador == null) return;

        float vidaNow = saludJugador.VidaActual;
        if (ultimaVidaJugador > 0f && vidaNow < ultimaVidaJugador)
        {
            // Recibió daño
            AnadirSacudida(intensidadDanoRecibido);
            alphaDestelloRojo = 0.42f;
        }
        ultimaVidaJugador = vidaNow;

        // Alerta de voz cuando la vida baja del 30%
        if (!alertaVidaCriticaEmitida && vidaNow > 0f && saludJugador.PorcentajeVida <= 0.30f)
        {
            alertaVidaCriticaEmitida = true;
            EmitirAlertaVoz("Advertencia. Integridad del chasis crítica.");
        }
        else if (saludJugador.PorcentajeVida > 0.50f)
        {
            // Si se cura con orbes verdes, vuelve a habilitar la alerta para el futuro
            alertaVidaCriticaEmitida = false;
        }
    }

    private void MonitorearEstrella()
    {
        if (AsaltoEstrellaManager.Instancia == null) return;

        // Alerta al iniciar el modo Frenesí
        if (AsaltoEstrellaManager.Instancia.FrenesiActivo && !alertaFrenesiEmitida)
        {
            alertaFrenesiEmitida = true;
            EmitirAlertaVoz("Alerta. Sobrecarga iniciada. Defiende el Núcleo Estrella.");
            if (saludEstrella != null) ultimaVidaEstrella = saludEstrella.VidaActual;
            return;
        }

        if (!AsaltoEstrellaManager.Instancia.FrenesiActivo || saludEstrella == null) return;

        float vidaEstrellaNow = saludEstrella.VidaActual;
        if (ultimaVidaEstrella > 0f && vidaEstrellaNow < ultimaVidaEstrella && timerAlertaEstrella <= 0f)
        {
            timerAlertaEstrella = 14f;
            EmitirAlertaVoz("Atención. El Núcleo Estrella está bajo ataque.");
        }
        ultimaVidaEstrella = vidaEstrellaNow;
    }

    // --- APLICACIÓN LIMPIA DE SCREEN SHAKE EN LATEUPDATE ---
    void LateUpdate()
    {
        if (!activarScreenShake || camaraPrincipal == null || Time.timeScale == 0f) return;

        // Primero quita el desplazamiento del frame anterior para no acumular deriva
        camaraPrincipal.transform.localPosition -= offsetShakeAnterior;
        offsetShakeAnterior = Vector3.zero;

        if (fuerzaShakeActual > 0.001f)
        {
            Vector3 aleatorio = Random.insideUnitSphere * fuerzaShakeActual;
            aleatorio.z *= 0.4f;
            offsetShakeAnterior = aleatorio;
            camaraPrincipal.transform.localPosition += offsetShakeAnterior;

            fuerzaShakeActual = Mathf.Max(0f, fuerzaShakeActual - decaimientoShake * Time.deltaTime * 0.5f);
        }
    }

    private void RestaurarOffsetCamara()
    {
        if (camaraPrincipal != null && offsetShakeAnterior != Vector3.zero)
        {
            camaraPrincipal.transform.localPosition -= offsetShakeAnterior;
            offsetShakeAnterior = Vector3.zero;
        }
    }

    public void AnadirSacudida(float cantidad)
    {
        fuerzaShakeActual = Mathf.Clamp(fuerzaShakeActual + cantidad, 0f, 0.55f);
    }

    // --- MOTOR DE ALERTAS DE VOZ EN ESPAÑOL ---
    public void EmitirAlertaVoz(string mensaje)
    {
        if (!activarAlertasVoz) return;
        if (Time.time - tiempoUltimaAlerta < enfriamientoMinimoEntreAlertas) return;

        tiempoUltimaAlerta = Time.time;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        try
        {
            if (procesoVozAlerta != null && !procesoVozAlerta.HasExited)
            {
                procesoVozAlerta.Kill();
                procesoVozAlerta.Dispose();
            }

            string fraseLimpia = mensaje.Replace("'", " ").Replace("\"", " ");
            string comandoPS =
                "Add-Type -AssemblyName System.Speech; " +
                "$s = New-Object System.Speech.Synthesis.SpeechSynthesizer; " +
                "$v = $s.GetInstalledVoices() | Where-Object { $_.VoiceInfo.Culture.Name -like 'es-*' } | Select-Object -First 1; " +
                "if ($v) { $s.SelectVoice($v.VoiceInfo.Name) }; " +
                "$s.Rate = 1; " +
                "$s.Volume = 95; " +
                $"$s.Speak('{fraseLimpia}');";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -WindowStyle Hidden -Command \"{comandoPS}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            procesoVozAlerta = Process.Start(psi);
        }
        catch { }
#endif
    }

    void OnDestroy()
    {
        RestaurarOffsetCamara();
        try
        {
            if (procesoVozAlerta != null && !procesoVozAlerta.HasExited)
            {
                procesoVozAlerta.Kill();
                procesoVozAlerta.Dispose();
            }
        }
        catch { }
    }

    // --- VIÑETA ROJA EN LOS BORDES AL RECIBIR DAÑO ---
    void OnGUI()
    {
        if (!activarDestelloDano || alphaDestelloRojo <= 0.01f || texBlanca == null || Time.timeScale == 0f) return;

        float w = Screen.width;
        float h = Screen.height;
        float grosorBorde = 36f;

        GUI.color = new Color(0.95f, 0.1f, 0.1f, alphaDestelloRojo);
        // Borde superior, inferior, izquierdo y derecho
        GUI.DrawTexture(new Rect(0, 0, w, grosorBorde), texBlanca);
        GUI.DrawTexture(new Rect(0, h - grosorBorde, w, grosorBorde), texBlanca);
        GUI.DrawTexture(new Rect(0, grosorBorde, grosorBorde, h - grosorBorde * 2f), texBlanca);
        GUI.DrawTexture(new Rect(w - grosorBorde, grosorBorde, grosorBorde, h - grosorBorde * 2f), texBlanca);
        GUI.color = Color.white;
    }
}