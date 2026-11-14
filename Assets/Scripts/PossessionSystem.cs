using UnityEngine;

public class PossessionSystem : MonoBehaviour
{
    [Header("Configuración de Posesión")]
    [SerializeField] private RobotBase[] listaRobots;
    [SerializeField] private int indiceInicial = 0;
    [SerializeField] private float distanciaMaximaPosesion = 20f; // Distancia máxima en metros

    private int indiceActual = 0;

    private void Start()
    {
        indiceActual = indiceInicial;

        for (int i = 0; i < listaRobots.Length; i++)
        {
            if (listaRobots[i] != null)
            {
                listaRobots[i].EstablecerPoseido(i == indiceActual);
            }
        }
    }

    private void Update()
    {
        // Cambiar con teclas numéricas (validando rango)
        if (Input.GetKeyDown(KeyCode.Alpha1)) IntentarCambiarARobot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) IntentarCambiarARobot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) IntentarCambiarARobot(2);

        // Cambiar al robot más cercano dentro del rango con Q
        if (Input.GetKeyDown(KeyCode.Q))
        {
            PoseerRobotMasCercano();
        }
    }

    public void IntentarCambiarARobot(int nuevoIndice)
    {
        if (nuevoIndice < 0 || nuevoIndice >= listaRobots.Length) return;
        if (listaRobots[nuevoIndice] == null || nuevoIndice == indiceActual) return;

        RobotBase robotActual = listaRobots[indiceActual];
        RobotBase robotObjetivo = listaRobots[nuevoIndice];

        float distancia = Vector3.Distance(robotActual.transform.position, robotObjetivo.transform.position);

        if (distancia <= distanciaMaximaPosesion)
        {
            CambiarARobot(nuevoIndice);
        }
        else
        {
            Debug.LogWarning($"Robot muy lejano ({distancia:F1}m). Límite de posesión: {distanciaMaximaPosesion}m");
        }
    }

    private void CambiarARobot(int nuevoIndice)
    {
        if (listaRobots[indiceActual] != null)
        {
            listaRobots[indiceActual].EstablecerPoseido(false);
        }

        indiceActual = nuevoIndice;
        listaRobots[indiceActual].EstablecerPoseido(true);
    }

    public void PoseerRobotMasCercano()
    {
        if (listaRobots == null || listaRobots.Length <= 1) return;

        RobotBase robotActual = listaRobots[indiceActual];
        int mejorIndice = -1;
        float menorDistancia = distanciaMaximaPosesion;

        for (int i = 0; i < listaRobots.Length; i++)
        {
            if (i == indiceActual || listaRobots[i] == null) continue;

            float dist = Vector3.Distance(robotActual.transform.position, listaRobots[i].transform.position);
            if (dist < menorDistancia)
            {
                menorDistancia = dist;
                mejorIndice = i;
            }
        }

        if (mejorIndice != -1)
        {
            CambiarARobot(mejorIndice);
        }
        else
        {
            Debug.Log("No hay ningún robot dentro del rango de posesión.");
        }
    }
}