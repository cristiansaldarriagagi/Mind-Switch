using UnityEngine;

public class PuertaAutomatica : MonoBehaviour
{
    [Header("--- HOJAS DE LA PUERTA ---")]
    [Tooltip("Hoja superior (door_2_left)")]
    public Transform hojaSuperior;
    [Tooltip("Hoja inferior (door_2_right)")]
    public Transform hojaInferior;

    [Header("--- CONFIGURACIÓN DE APERTURA ---")]
    public Vector3 desplazamientoSuperior = new Vector3(0f, 1.8f, 0f);
    public Vector3 desplazamientoInferior = new Vector3(0f, -1.8f, 0f);
    public float velocidadApertura = 6f;

    [Header("--- SENSOR DE PROXIMIDAD ---")]
    [Tooltip("Radio en metros alrededor de la puerta que detecta al jugador o enemigos")]
    public float radioDeteccion = 3.2f;
    [Tooltip("Segundos que permanece abierta después de que todos se alejan")]
    public float tiempoEsperaCierre = 1.0f;

    private Vector3 posCerradaSup;
    private Vector3 posAbiertaSup;
    private Vector3 posCerradaInf;
    private Vector3 posAbiertaInf;

    private float temporizadorCierre = 0f;

    void Start()
    {
        // Apaga el Animator original del paquete para que no cierre la puerta a la fuerza
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;

        if (hojaSuperior != null)
        {
            posCerradaSup = hojaSuperior.localPosition;
            posAbiertaSup = posCerradaSup + desplazamientoSuperior;
        }

        if (hojaInferior != null)
        {
            posCerradaInf = hojaInferior.localPosition;
            posAbiertaInf = posCerradaInf + desplazamientoInferior;
        }
    }

    void Update()
    {
        if (HayAlguienCerca())
        {
            temporizadorCierre = tiempoEsperaCierre;
        }
        else if (temporizadorCierre > 0f)
        {
            temporizadorCierre -= Time.deltaTime;
        }

        bool abrir = temporizadorCierre > 0f;

        if (hojaSuperior != null)
        {
            Vector3 destinoSup = abrir ? posAbiertaSup : posCerradaSup;
            hojaSuperior.localPosition = Vector3.Lerp(hojaSuperior.localPosition, destinoSup, Time.deltaTime * velocidadApertura);
        }

        if (hojaInferior != null)
        {
            Vector3 destinoInf = abrir ? posAbiertaInf : posCerradaInf;
            hojaInferior.localPosition = Vector3.Lerp(hojaInferior.localPosition, destinoInf, Time.deltaTime * velocidadApertura);
        }
    }

    private bool HayAlguienCerca()
    {
        // Revisa en un radio de 3.2 metros alrededor de la puerta sin importar su rotación
        Vector3 centro = transform.position + Vector3.up * 1.2f;
        Collider[] cercanos = Physics.OverlapSphere(centro, radioDeteccion);

        foreach (Collider col in cercanos)
        {
            if (col.CompareTag("Player") || col.GetComponentInParent<EnemigoIA>() != null)
            {
                return true;
            }
        }
        return false;
    }

    // Dibuja la esfera amarilla de detección en la vista Scene
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.2f, radioDeteccion);
    }
}