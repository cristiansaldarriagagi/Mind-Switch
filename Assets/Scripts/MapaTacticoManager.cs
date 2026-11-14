using UnityEngine;
using System.Collections.Generic;

public class MapaTacticoManager : MonoBehaviour
{
    [Header("--- CONTROLES DEL MAPA ---")]
    public KeyCode teclaMapa1 = KeyCode.M;
    public KeyCode teclaMapa2 = KeyCode.Tab;

    private bool mapaAbierto = false;
    private static Texture2D texBlanca;

    void Awake()
    {
        if (texBlanca == null)
        {
            texBlanca = new Texture2D(1, 1);
            texBlanca.SetPixel(0, 0, Color.white);
            texBlanca.Apply();
        }
    }

    void Update()
    {
        // Solo permite abrir/cerrar el mapa durante el combate activo (sin pausar el tiempo)
        if (Time.timeScale == 0f)
        {
            mapaAbierto = false;
            return;
        }

        if (Input.GetKeyDown(teclaMapa1) || Input.GetKeyDown(teclaMapa2))
        {
            mapaAbierto = !mapaAbierto;
        }
    }

    void OnGUI()
    {
        if (!mapaAbierto || Time.timeScale == 0f || texBlanca == null) return;

        UbicadorEstrellaAleatorio ubicador = UbicadorEstrellaAleatorio.Instancia;
        if (ubicador == null || ubicador.salasDetectadas.Count == 0 || ubicador.jugador == null) return;

        float w = Screen.width;
        float h = Screen.height;

        // Fondo semitransparente para poder seguir viendo de reojo si algo se acerca
        GUI.color = new Color(0.02f, 0.04f, 0.08f, 0.78f);
        GUI.DrawTexture(new Rect(0, 0, w, h), texBlanca);

        float tamanoPanel = Mathf.Min(w * 0.82f, h * 0.82f);
        Rect panel = new Rect((w - tamanoPanel) / 2f, (h - tamanoPanel) / 2f, tamanoPanel, tamanoPanel);

        // Borde y fondo del visor táctico
        GUI.color = new Color(0.15f, 0.85f, 1f, 0.9f);
        GUI.DrawTexture(new Rect(panel.x - 2, panel.y - 2, panel.width + 4, panel.height + 4), texBlanca);
        GUI.color = new Color(0.04f, 0.08f, 0.13f, 0.94f);
        GUI.DrawTexture(panel, texBlanca);

        // Encabezado con advertencia en tiempo real
        GUIStyle estiloTitulo = new GUIStyle(GUI.skin.label);
        estiloTitulo.alignment = TextAnchor.MiddleCenter;
        estiloTitulo.fontStyle = FontStyle.Bold;
        estiloTitulo.fontSize = 15;
        estiloTitulo.normal.textColor = new Color(0.2f, 0.9f, 1f);
        GUI.color = Color.white;
        GUI.Label(new Rect(panel.x, panel.y + 8f, panel.width, 24f),
            "PLANO TÁCTICO DE LA ESTACIÓN  [EN VIVO - ¡CUIDADO CON LOS ENEMIGOS!]", estiloTitulo);

        // Área interior donde se dibujan las habitaciones
        Rect areaDibujo = new Rect(panel.x + 28f, panel.y + 42f, panel.width - 56f, panel.height - 86f);
        Bounds limites = ubicador.limitesGlobalesMapa;

        float rangoX = Mathf.Max(1f, limites.size.x);
        float rangoZ = Mathf.Max(1f, limites.size.z);
        float escala = Mathf.Min(areaDibujo.width / rangoX, areaDibujo.height / rangoZ);

        float offsetX = areaDibujo.x + (areaDibujo.width - rangoX * escala) / 2f;
        float offsetY = areaDibujo.y + (areaDibujo.height - rangoZ * escala) / 2f;

        // Función local para convertir coordenadas 3D (X, Z) a coordenadas 2D del mapa
        System.Func<Vector3, Vector2> MundoAMapa = (pos3D) =>
        {
            float px = offsetX + (pos3D.x - limites.min.x) * escala;
            // En pantalla Y crece hacia abajo, por eso invertimos Z
            float py = offsetY + (limites.max.z - pos3D.z) * escala;
            return new Vector2(px, py);
        };

        // 1. Dibuja todas las salas y pasillos del laberinto
        foreach (Bounds sala in ubicador.salasDetectadas)
        {
            Vector2 topLeft = MundoAMapa(new Vector3(sala.min.x, 0, sala.max.z));
            Vector2 bottomRight = MundoAMapa(new Vector3(sala.max.x, 0, sala.min.z));
            Rect rSala = new Rect(topLeft.x + 1f, topLeft.y + 1f,
                                  Mathf.Max(3f, bottomRight.x - topLeft.x - 2f),
                                  Mathf.Max(3f, bottomRight.y - topLeft.y - 2f));

            GUI.color = new Color(0.14f, 0.24f, 0.34f, 0.85f);
            GUI.DrawTexture(rSala, texBlanca);
        }

        // 2. Dibuja en ROJO PULSANTE todos los enemigos activos en tiempo real
        float pulso = 1f + Mathf.Sin(Time.time * 8f) * 0.25f;
        Salud[] entidades = FindObjectsByType<Salud>(FindObjectsSortMode.None);
        foreach (Salud s in entidades)
        {
            if (s == null || s.CompareTag("Player") || s.VidaActual <= 0f) continue;

            Vector2 posEnemigo = MundoAMapa(s.transform.position);
            float tamEnemigo = 8f * pulso;
            GUI.color = new Color(1f, 0.2f, 0.2f, 0.95f);
            GUI.DrawTexture(new Rect(posEnemigo.x - tamEnemigo / 2f, posEnemigo.y - tamEnemigo / 2f, tamEnemigo, tamEnemigo), texBlanca);
        }

        // 3. Dibuja en AMARILLO BRILLANTE el Núcleo Estrella
        if (ubicador.estrellaObjetivo != null)
        {
            Vector2 posEstrella = MundoAMapa(ubicador.estrellaObjetivo.position);
            float tamAura = 18f * pulso;
            GUI.color = new Color(1f, 0.85f, 0.1f, 0.35f);
            GUI.DrawTexture(new Rect(posEstrella.x - tamAura / 2f, posEstrella.y - tamAura / 2f, tamAura, tamAura), texBlanca);

            GUI.color = new Color(1f, 0.92f, 0.2f, 1f);
            GUI.DrawTexture(new Rect(posEstrella.x - 5f, posEstrella.y - 5f, 10f, 10f), texBlanca);
        }

        // 4. Dibuja en CIAN al Jugador (UNIT-09)
        Vector2 posJugador = MundoAMapa(ubicador.jugador.position);
        GUI.color = new Color(0.1f, 1f, 0.95f, 1f);
        GUI.DrawTexture(new Rect(posJugador.x - 6f, posJugador.y - 6f, 12f, 12f), texBlanca);

        // 5. Leyenda inferior
        GUI.color = Color.white;
        GUIStyle estiloLeyenda = new GUIStyle(GUI.skin.label);
        estiloLeyenda.alignment = TextAnchor.MiddleCenter;
        estiloLeyenda.fontStyle = FontStyle.Bold;
        estiloLeyenda.fontSize = 12;
        estiloLeyenda.normal.textColor = new Color(0.88f, 0.93f, 1f);

        GUI.Label(new Rect(panel.x, panel.yMax - 34f, panel.width, 26f),
            "[CIAN] Tú (UNIT-09)    |    [ROJO] Enemigos en movimiento    |    [AMARILLO] Estrella    |    [M / TAB] Cerrar",
            estiloLeyenda);
    }
}