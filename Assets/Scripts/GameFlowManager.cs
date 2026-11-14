using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instancia;

    public enum EstadoJuego { MenuInicio, Jugando, Pausa, Victoria, Derrota, Manual }
    public EstadoJuego estadoActual = EstadoJuego.MenuInicio;
    private EstadoJuego estadoAnteriorAlManual = EstadoJuego.MenuInicio;

    [Header("--- CONFIGURACIÓN DE PUNTUACIÓN ---")]
    public int puntosPorEnemigo = 100;
    public int puntosPorEstrella = 500;
    public bool mostrarMenuAlArrancar = true;

    private int enemigosEliminados = 0;
    private int puntuacionTotal = 0;
    private bool estrellaActivada = false;

    // --- PERSISTENCIA DE PROGRESO (PlayerPrefs) ---
    private const string KEY_MEJOR_PUNTAJE = "MindSwitch_MejorPuntaje";
    private const string KEY_TOTAL_BAJAS = "MindSwitch_TotalBajas";
    private const string KEY_VICTORIAS = "MindSwitch_Victorias";

    private int mejorPuntajeGuardado = 0;
    private int totalBajasHistoricas = 0;
    private int victoriasTotales = 0;
    private bool esNuevoRecord = false;
    private bool progresoPartidaGuardado = false;

    private Vector2 scrollManual = Vector2.zero;
    private static Texture2D texBlanca;
    private static bool saltarMenuEnReinicio = false;

    // Resolución base virtual para que los menús escalen perfecto en cualquier pantalla
    private const float ANCHO_VIRTUAL = 1280f;
    private const float ALTO_VIRTUAL = 760f;

    void Awake()
    {
        Instancia = this;
        if (texBlanca == null)
        {
            texBlanca = new Texture2D(1, 1);
            texBlanca.SetPixel(0, 0, Color.white);
            texBlanca.Apply();
        }
        CargarProgreso();
    }

    void Start()
    {
        if (mostrarMenuAlArrancar && !saltarMenuEnReinicio)
        {
            CambiarEstado(EstadoJuego.MenuInicio);
        }
        else
        {
            saltarMenuEnReinicio = false;
            CambiarEstado(EstadoJuego.Jugando);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (estadoActual == EstadoJuego.Jugando)
                CambiarEstado(EstadoJuego.Pausa);
            else if (estadoActual == EstadoJuego.Pausa)
                CambiarEstado(EstadoJuego.Jugando);
            else if (estadoActual == EstadoJuego.Manual)
                CambiarEstado(estadoAnteriorAlManual);
        }
    }

    private void CargarProgreso()
    {
        mejorPuntajeGuardado = PlayerPrefs.GetInt(KEY_MEJOR_PUNTAJE, 0);
        totalBajasHistoricas = PlayerPrefs.GetInt(KEY_TOTAL_BAJAS, 0);
        victoriasTotales = PlayerPrefs.GetInt(KEY_VICTORIAS, 0);
    }

    private void GuardarProgresoPartida(bool ganoMision)
    {
        if (progresoPartidaGuardado) return;
        progresoPartidaGuardado = true;

        totalBajasHistoricas += enemigosEliminados;
        if (ganoMision) victoriasTotales++;

        if (puntuacionTotal > mejorPuntajeGuardado)
        {
            mejorPuntajeGuardado = puntuacionTotal;
            esNuevoRecord = true;
        }

        PlayerPrefs.SetInt(KEY_MEJOR_PUNTAJE, mejorPuntajeGuardado);
        PlayerPrefs.SetInt(KEY_TOTAL_BAJAS, totalBajasHistoricas);
        PlayerPrefs.SetInt(KEY_VICTORIAS, victoriasTotales);
        PlayerPrefs.Save();
    }

    public void BorrarProgresoGuardado()
    {
        PlayerPrefs.DeleteKey(KEY_MEJOR_PUNTAJE);
        PlayerPrefs.DeleteKey(KEY_TOTAL_BAJAS);
        PlayerPrefs.DeleteKey(KEY_VICTORIAS);
        PlayerPrefs.Save();
        CargarProgreso();
    }

    public void CambiarEstado(EstadoJuego nuevoEstado)
    {
        estadoActual = nuevoEstado;
        scrollManual = Vector2.zero;

        if (estadoActual == EstadoJuego.Jugando)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void AbrirManual()
    {
        estadoAnteriorAlManual = estadoActual;
        CambiarEstado(EstadoJuego.Manual);
    }

    public void RegistrarEnemigoEliminado()
    {
        if (estadoActual != EstadoJuego.Jugando) return;
        enemigosEliminados++;
        puntuacionTotal += puntosPorEnemigo;
    }

    public void RegistrarEstrellaActivada()
    {
        if (estrellaActivada) return;
        estrellaActivada = true;
        puntuacionTotal += puntosPorEstrella;
    }

    public void MostrarVictoria()
    {
        if (estadoActual == EstadoJuego.Victoria || estadoActual == EstadoJuego.Derrota) return;
        puntuacionTotal += 1000;
        GuardarProgresoPartida(true);
        CambiarEstado(EstadoJuego.Victoria);
    }

    public void MostrarDerrota()
    {
        if (estadoActual == EstadoJuego.Victoria || estadoActual == EstadoJuego.Derrota) return;
        GuardarProgresoPartida(false);
        CambiarEstado(EstadoJuego.Derrota);
    }

    public void ReiniciarPartidaDirecta()
    {
        saltarMenuEnReinicio = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void VolverAlMenuPrincipal()
    {
        saltarMenuEnReinicio = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnGUI()
    {
        if (texBlanca == null) return;

        if (estadoActual == EstadoJuego.Jugando)
        {
            DibujarHUDPuntos();
            return;
        }

        // Fondo oscuro cubre toda la pantalla real
        GUI.color = new Color(0.03f, 0.05f, 0.09f, 0.90f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texBlanca);
        GUI.color = Color.white;

        // Escalado automático proporcional para que ningún menú se corte ni se superponga
        Matrix4x4 matrizOriginal = GUI.matrix;
        float escalaX = Screen.width / ANCHO_VIRTUAL;
        float escalaY = Screen.height / ALTO_VIRTUAL;
        float escala = Mathf.Min(escalaX, escalaY);

        float anchoEscalado = ANCHO_VIRTUAL * escala;
        float altoEscalado = ALTO_VIRTUAL * escala;
        float margenX = (Screen.width - anchoEscalado) * 0.5f;
        float margenY = (Screen.height - altoEscalado) * 0.5f;

        GUI.matrix = Matrix4x4.TRS(new Vector3(margenX, margenY, 0f), Quaternion.identity, new Vector3(escala, escala, 1f));

        if (estadoActual == EstadoJuego.Manual)
        {
            DibujarPantallaManual();
            GUI.matrix = matrizOriginal;
            return;
        }

        float anchoCaja = 620f;
        float altoCaja = 560f;
        Rect caja = new Rect((ANCHO_VIRTUAL - anchoCaja) / 2f, (ALTO_VIRTUAL - altoCaja) / 2f, anchoCaja, altoCaja);

        DibujarPanel(caja, new Color(0.08f, 0.12f, 0.18f, 0.97f), new Color(0.1f, 0.8f, 0.95f, 1f));

        switch (estadoActual)
        {
            case EstadoJuego.MenuInicio:
                DibujarMenuInicio(caja);
                break;
            case EstadoJuego.Pausa:
                DibujarMenuPausa(caja);
                break;
            case EstadoJuego.Victoria:
                DibujarPantallaFinal(caja, "¡MISIÓN CUMPLIDA!", new Color(0.15f, 0.9f, 0.45f), "HAS DEFENDIDO LA ESTRELLA CON ÉXITO");
                break;
            case EstadoJuego.Derrota:
                DibujarPantallaFinal(caja, "SISTEMA DESTRUIDO", new Color(0.95f, 0.25f, 0.25f), "TU MECH O EL NÚCLEO HAN CAÍDO");
                break;
        }

        GUI.matrix = matrizOriginal;
    }

    private void DibujarHUDPuntos()
    {
        GUIStyle estilo = new GUIStyle(GUI.skin.label);
        estilo.alignment = TextAnchor.UpperRight;
        estilo.fontStyle = FontStyle.Bold;
        estilo.fontSize = 15;
        estilo.normal.textColor = new Color(0.3f, 0.95f, 1f);

        GUI.Label(new Rect(Screen.width - 310f, 18f, 290f, 65f),
            $"PUNTOS: {puntuacionTotal}  (RÉCORD: {Mathf.Max(mejorPuntajeGuardado, puntuacionTotal)})\nBAJAS: {enemigosEliminados}", estilo);
    }

    private void DibujarMenuInicio(Rect c)
    {
        DibujarTitulo(new Rect(c.x, c.y + 28, c.width, 54), "MIND SWITCH", 42, new Color(0.15f, 0.85f, 1f));

        // Panel de Récords Guardados
        Rect panelRecord = new Rect(c.x + 45, c.y + 96, c.width - 90, 68);
        DibujarPanel(panelRecord, new Color(0.05f, 0.09f, 0.14f, 0.95f), new Color(0.95f, 0.78f, 0.2f, 0.85f));
        DibujarTitulo(new Rect(panelRecord.x, panelRecord.y + 8, panelRecord.width, 26),
            $"RÉCORD MÁXIMO: {mejorPuntajeGuardado} PTS", 17, new Color(1f, 0.85f, 0.25f));
        DibujarSubtitulo(new Rect(panelRecord.x, panelRecord.y + 36, panelRecord.width, 24),
            $"Misiones completadas: {victoriasTotales}   |   Robots destruidos: {totalBajasHistoricas}", 14);

        DibujarSubtitulo(new Rect(c.x + 35, c.y + 180, c.width - 70, 62),
            "OBJETIVO: Localiza el Núcleo Estrella y resiste la oleada final.\n[WASD] Mover   |   [CLIC IZQ] Disparar   |   [Q] Pulso EMP   |   [M] Mapa", 15);

        float anchoBoton = 320f;
        float xBoton = c.x + (c.width - anchoBoton) / 2f;

        if (DibujarBoton(new Rect(xBoton, c.y + 262, anchoBoton, 54), "INICIAR MISIÓN", new Color(0.12f, 0.75f, 0.4f)))
        {
            if (HistoriaIntroManager.Instancia != null && HistoriaIntroManager.Instancia.reproducirAlIniciarPartida)
            {
                estadoActual = EstadoJuego.Jugando;
                HistoriaIntroManager.Instancia.IniciarCinematica();
            }
            else
            {
                CambiarEstado(EstadoJuego.Jugando);
            }
        }

        if (DibujarBoton(new Rect(xBoton, c.y + 332, anchoBoton, 52), "MANUAL DE USUARIO", new Color(0.16f, 0.55f, 0.85f)))
        {
            AbrirManual();
        }

        if (DibujarBoton(new Rect(xBoton, c.y + 400, anchoBoton, 48), "SALIR DEL JUEGO", new Color(0.45f, 0.2f, 0.2f)))
        {
            Application.Quit();
        }

        if (mejorPuntajeGuardado > 0 || totalBajasHistoricas > 0)
        {
            GUIStyle sReset = new GUIStyle(GUI.skin.button);
            sReset.fontSize = 13;
            sReset.normal.textColor = new Color(0.75f, 0.82f, 0.9f);
            if (GUI.Button(new Rect(c.x + (c.width - 230f) / 2f, c.yMax - 46f, 230f, 30f), "Reiniciar récords guardados", sReset))
            {
                BorrarProgresoGuardado();
            }
        }
    }

    private void DibujarMenuPausa(Rect c)
    {
        DibujarTitulo(new Rect(c.x, c.y + 34, c.width, 52), "JUEGO EN PAUSA", 36, new Color(0.95f, 0.8f, 0.2f));
        DibujarSubtitulo(new Rect(c.x, c.y + 98, c.width, 54),
            $"Puntos actuales: {puntuacionTotal} pts   |   Récord: {mejorPuntajeGuardado} pts\nEnemigos abatidos en esta misión: {enemigosEliminados}", 16);

        float anchoBoton = 320f;
        float xBoton = c.x + (c.width - anchoBoton) / 2f;

        if (DibujarBoton(new Rect(xBoton, c.y + 180, anchoBoton, 54), "CONTINUAR", new Color(0.15f, 0.7f, 0.4f)))
        {
            CambiarEstado(EstadoJuego.Jugando);
        }

        if (DibujarBoton(new Rect(xBoton, c.y + 250, anchoBoton, 52), "MANUAL DE USUARIO", new Color(0.16f, 0.55f, 0.85f)))
        {
            AbrirManual();
        }

        if (DibujarBoton(new Rect(xBoton, c.y + 320, anchoBoton, 52), "REINICIAR MISIÓN", new Color(0.28f, 0.48f, 0.68f)))
        {
            ReiniciarPartidaDirecta();
        }

        if (DibujarBoton(new Rect(xBoton, c.y + 390, anchoBoton, 50), "MENÚ DE INICIO", new Color(0.35f, 0.35f, 0.4f)))
        {
            VolverAlMenuPrincipal();
        }
    }

    // --- MANUAL DE USUARIO AMPLIO Y ESPACIADO ---
    private void DibujarPantallaManual()
    {
        float ancho = 1080f;
        float alto = 700f;
        Rect m = new Rect((ANCHO_VIRTUAL - ancho) / 2f, (ALTO_VIRTUAL - alto) / 2f, ancho, alto);

        DibujarPanel(m, new Color(0.06f, 0.1f, 0.16f, 0.98f), new Color(0.15f, 0.85f, 1f, 1f));

        DibujarTitulo(new Rect(m.x, m.y + 14, m.width, 38), "MANUAL DE OPERACIONES // UNIT-09", 26, new Color(0.2f, 0.9f, 1f));

        float pad = 28f;
        float anchoCol = m.width - pad * 2f;

        // 1. Controles, Pulso EMP y Mapa
        Rect sec1 = new Rect(m.x + pad, m.y + 62, anchoCol, 86);
        DibujarPanel(sec1, new Color(0.09f, 0.14f, 0.22f, 0.92f), new Color(0.25f, 0.45f, 0.6f, 1f));
        DibujarTitulo(new Rect(sec1.x + 14, sec1.y + 6, sec1.width - 28, 24), "1. CONTROLES, PULSO EMP Y MAPA TÁCTICO", 15, new Color(0.4f, 0.95f, 1f));
        DibujarTextoIzquierda(new Rect(sec1.x + 16, sec1.y + 32, sec1.width - 32, 48),
            "• [W A S D] Mover   |   [CLIC IZQ] Disparar   |   [Q / CLIC DER] Pulso EMP (onda expansiva que empuja y aturde 2.6s)\n" +
            "• [M / TAB] Plano de Salas en vivo (el juego sigue corriendo: ¡vigila los puntos rojos!)   |   [ESC / P] Pausa", 14);

        // 2. Sobrecarga y Defensa
        Rect sec2 = new Rect(m.x + pad, sec1.yMax + 12, anchoCol, 86);
        DibujarPanel(sec2, new Color(0.09f, 0.14f, 0.22f, 0.92f), new Color(0.25f, 0.45f, 0.6f, 1f));
        DibujarTitulo(new Rect(sec2.x + 14, sec2.y + 6, sec2.width - 28, 24), "2. SOBRECARGA DE CAÑÓN Y DEFENSA DEL NÚCLEO", 15, new Color(1f, 0.8f, 0.25f));
        DibujarTextoIzquierda(new Rect(sec2.x + 16, sec2.y + 32, sec2.width - 32, 48),
            "• CALOR DE ARMA: Disparar sin pausa llega al 100% y bloquea los cañones 2.4s. Suelta el gatillo para enfriar.\n" +
            "• MODO FRENESÍ: Al tocar el Núcleo Estrella, la mitad de las fuerzas de AEGIS intentarán destruirlo. ¡Protégelo!", 14);

        // 3. Orbes Recolectables
        Rect sec3 = new Rect(m.x + pad, sec2.yMax + 12, anchoCol, 158);
        DibujarPanel(sec3, new Color(0.09f, 0.14f, 0.22f, 0.92f), new Color(0.25f, 0.45f, 0.6f, 1f));
        DibujarTitulo(new Rect(sec3.x + 14, sec3.y + 6, sec3.width - 28, 24), "3. ORBES DE ENERGÍA (SUELTAN LOS ROBOTS AL SER DESTRUIDOS)", 15, new Color(0.3f, 1f, 0.65f));

        float anchoTarjeta = (sec3.width - 56f) / 3f;
        float yTarjeta3 = sec3.y + 34f;
        float altoTarjeta = 112f;

        DibujarTarjeta(new Rect(sec3.x + 14f, yTarjeta3, anchoTarjeta, altoTarjeta),
            new Color(0.15f, 1f, 0.35f), "ORBE VERDE", "REPARACIÓN (+30 HP)",
            "Repara tu Mech. Si estás cerca de la Estrella en frenesí, también repara el núcleo.");

        DibujarTarjeta(new Rect(sec3.x + 28f + anchoTarjeta, yTarjeta3, anchoTarjeta, altoTarjeta),
            new Color(0.1f, 0.9f, 1f), "ORBE CIAN", "REFRIGERACIÓN CERO",
            "Baja el calor a 0% al instante y permite fuego infinito sin sobrecalentarse por 6.5s.");

        DibujarTarjeta(new Rect(sec3.x + 42f + anchoTarjeta * 2f, yTarjeta3, anchoTarjeta, altoTarjeta),
            new Color(0.85f, 0.25f, 1f), "ORBE MORADO", "DISPARO TRIPLE",
            "Multiplica tus proyectiles creando un abanico devastador durante 6.5s.");

        // 4. Variantes de Soldados
        Rect sec4 = new Rect(m.x + pad, sec3.yMax + 12, anchoCol, 158);
        DibujarPanel(sec4, new Color(0.09f, 0.14f, 0.22f, 0.92f), new Color(0.25f, 0.45f, 0.6f, 1f));
        DibujarTitulo(new Rect(sec4.x + 14, sec4.y + 6, sec4.width - 28, 24), "4. UNIDADES ENEMIGAS DE AEGIS (INTELIGENCIA TÁCTICA)", 15, new Color(1f, 0.45f, 0.35f));

        float yTarjeta4 = sec4.y + 34f;
        DibujarTarjeta(new Rect(sec4.x + 14f, yTarjeta4, anchoTarjeta, altoTarjeta),
            new Color(0.95f, 0.25f, 0.25f), "CENTINELA ESTÁNDAR", "INFANTERÍA DE ASALTO",
            "Unidad base equilibrada. Ataca en grupos para flanquear tu posición.");

        DibujarTarjeta(new Rect(sec4.x + 28f + anchoTarjeta, yTarjeta4, anchoTarjeta, altoTarjeta),
            new Color(1f, 0.5f, 0.1f), "CORREDOR KAMIKAZE", "DETONADOR VELOZ",
            "Pequeño y muy rápido. Estalla al tocar tu Mech o el Núcleo Estrella. ¡Frénalo con EMP!");

        DibujarTarjeta(new Rect(sec4.x + 42f + anchoTarjeta * 2f, yTarjeta4, anchoTarjeta, altoTarjeta),
            new Color(0.72f, 0.2f, 1f), "COLOSO BLINDADO", "TANQUE PESADO (3X HP)",
            "Chasis gigante con triple resistencia. Concentra fuego en él antes de que llegue al núcleo.");

        // Botón Volver con amplio margen inferior libre
        if (DibujarBoton(new Rect(m.x + (m.width - 260f) / 2f, m.yMax - 62f, 260f, 44f), "VOLVER", new Color(0.15f, 0.72f, 0.42f)))
        {
            CambiarEstado(estadoAnteriorAlManual);
        }
    }

    private void DibujarTarjeta(Rect r, Color colorIdentidad, string nombre, string subtitulo, string descripcion)
    {
        DibujarPanel(r, new Color(0.05f, 0.08f, 0.13f, 0.95f), colorIdentidad);

        GUI.color = colorIdentidad;
        GUI.DrawTexture(new Rect(r.x + 12f, r.y + 12f, 16f, 16f), texBlanca);
        GUI.color = Color.white;

        GUIStyle sTit = new GUIStyle(GUI.skin.label);
        sTit.fontStyle = FontStyle.Bold;
        sTit.fontSize = 13;
        sTit.normal.textColor = colorIdentidad;
        GUI.Label(new Rect(r.x + 34f, r.y + 9f, r.width - 42f, 22f), nombre, sTit);

        GUIStyle sEf = new GUIStyle(GUI.skin.label);
        sEf.fontStyle = FontStyle.Bold;
        sEf.fontSize = 11;
        sEf.normal.textColor = Color.white;
        GUI.Label(new Rect(r.x + 12f, r.y + 30f, r.width - 24f, 18f), subtitulo, sEf);

        GUIStyle sDesc = new GUIStyle(GUI.skin.label);
        sDesc.fontSize = 12;
        sDesc.wordWrap = true;
        sDesc.normal.textColor = new Color(0.86f, 0.92f, 0.98f);
        GUI.Label(new Rect(r.x + 12f, r.y + 50f, r.width - 24f, 56f), descripcion, sDesc);
    }

    private void DibujarPantallaFinal(Rect c, string titulo, Color colorTitulo, string subtitulo)
    {
        DibujarTitulo(new Rect(c.x, c.y + 30, c.width, 52), titulo, 36, colorTitulo);
        DibujarSubtitulo(new Rect(c.x + 25, c.y + 88, c.width - 50, 30), subtitulo, 16);

        if (esNuevoRecord)
        {
            DibujarTitulo(new Rect(c.x + 25, c.y + 122, c.width - 50, 28),
                "★ ¡NUEVO RÉCORD PERSONAL ESTABLECIDO! ★", 17, new Color(1f, 0.85f, 0.2f));
        }

        Rect cajaPuntos = new Rect(c.x + 55, c.y + 162, c.width - 110, 175);
        DibujarPanel(cajaPuntos, new Color(0.04f, 0.07f, 0.11f, 0.95f), new Color(0.3f, 0.45f, 0.6f, 1f));

        DibujarSubtitulo(new Rect(cajaPuntos.x, cajaPuntos.y + 18, cajaPuntos.width, 145),
            $"ENEMIGOS ELIMINADOS: {enemigosEliminados}  (+{enemigosEliminados * puntosPorEnemigo} pts)\n" +
            $"ESTRELLA ACTIVADA: {(estrellaActivada ? "SÍ (+500 pts)" : "NO (0 pts)")}\n" +
            $"PUNTUACIÓN DE MISIÓN: {puntuacionTotal} PTS\n\n" +
            $"RÉCORD HISTÓRICO: {mejorPuntajeGuardado} PTS   |   BAJAS TOTALES: {totalBajasHistoricas}", 16);

        float anchoBoton = 300f;
        float xBoton = c.x + (c.width - anchoBoton) / 2f;

        if (DibujarBoton(new Rect(xBoton, c.y + 368, anchoBoton, 54), "JUGAR DE NUEVO", new Color(0.15f, 0.75f, 0.4f)))
        {
            ReiniciarPartidaDirecta();
        }

        if (DibujarBoton(new Rect(xBoton, c.y + 438, anchoBoton, 48), "MENÚ DE INICIO", new Color(0.3f, 0.35f, 0.42f)))
        {
            VolverAlMenuPrincipal();
        }
    }

    private void DibujarPanel(Rect r, Color fondo, Color borde)
    {
        GUI.color = borde;
        GUI.DrawTexture(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), texBlanca);
        GUI.color = fondo;
        GUI.DrawTexture(r, texBlanca);
        GUI.color = Color.white;
    }

    private void DibujarTitulo(Rect r, string texto, int tamano, Color color)
    {
        GUIStyle s = new GUIStyle(GUI.skin.label);
        s.alignment = TextAnchor.MiddleCenter;
        s.fontStyle = FontStyle.Bold;
        s.fontSize = tamano;
        s.normal.textColor = color;
        GUI.Label(r, texto, s);
    }

    private void DibujarSubtitulo(Rect r, string texto, int tamano)
    {
        GUIStyle s = new GUIStyle(GUI.skin.label);
        s.alignment = TextAnchor.MiddleCenter;
        s.fontSize = tamano;
        s.normal.textColor = new Color(0.9f, 0.93f, 0.98f);
        GUI.Label(r, texto, s);
    }

    private void DibujarTextoIzquierda(Rect r, string texto, int tamano)
    {
        GUIStyle s = new GUIStyle(GUI.skin.label);
        s.alignment = TextAnchor.UpperLeft;
        s.fontSize = tamano;
        s.wordWrap = true;
        s.normal.textColor = new Color(0.9f, 0.94f, 0.98f);
        GUI.Label(r, texto, s);
    }

    private bool DibujarBoton(Rect r, string texto, Color colorFondo)
    {
        GUI.color = colorFondo;
        GUI.DrawTexture(r, texBlanca);
        GUI.color = Color.white;

        GUIStyle s = new GUIStyle(GUI.skin.button);
        s.fontStyle = FontStyle.Bold;
        s.fontSize = 16;
        s.normal.background = null;
        s.hover.background = null;
        s.active.background = null;
        s.normal.textColor = Color.white;
        s.hover.textColor = new Color(1f, 0.95f, 0.4f);

        return GUI.Button(r, texto, s);
    }
}