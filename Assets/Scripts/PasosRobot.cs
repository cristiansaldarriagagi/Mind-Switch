using UnityEngine;

public class PasosRobot : MonoBehaviour
{
    public bool esJugador = false;
    [Tooltip("Metros que recorre entre cada pisada")]
    public float distanciaPorPaso = 1.6f;

    private Vector3 ultimaPosicion;
    private float distanciaAcumulada;

    void Start()
    {
        ultimaPosicion = transform.position;
        // Desfase inicial aleatorio en enemigos para que no suenen todos al unísono
        if (!esJugador) distanciaAcumulada = Random.Range(0f, distanciaPorPaso);
    }

    void Update()
    {
        if (Time.timeScale == 0f || SFXManager.Instancia == null) return;

        Vector3 posActual = transform.position;
        float movido = Vector3.Distance(new Vector3(posActual.x, 0, posActual.z), new Vector3(ultimaPosicion.x, 0, ultimaPosicion.z));
        ultimaPosicion = posActual;

        // Ignora teletransportes o micro-vibraciones
        if (movido > 0.001f && movido < 2f)
        {
            distanciaAcumulada += movido;
            if (distanciaAcumulada >= distanciaPorPaso)
            {
                distanciaAcumulada = 0f;
                if (esJugador)
                    SFXManager.Instancia.ReproducirPasoMech(transform.position);
                else
                    SFXManager.Instancia.ReproducirPasoEnemigo(transform.position);
            }
        }
    }
}