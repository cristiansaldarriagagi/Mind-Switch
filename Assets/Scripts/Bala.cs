using UnityEngine;

public class Bala : MonoBehaviour
{
    [SerializeField] private float velocidad = 40f;
    [SerializeField] private float tiempoVida = 3f;
    [SerializeField] private float dano = 25f;
    [SerializeField] private float radioImpacto = 0.2f;

    [HideInInspector] public Transform tirador;
    private bool impactoRealizado = false;

    private void Start()
    {
        if (SFXManager.Instancia != null)
        {
            SFXManager.Instancia.ReproducirDisparo(transform.position);
        }

        Destroy(gameObject, tiempoVida);

        // Comprobación inmediata al nacer (por si el cañón ya está tocando al enemigo)
        Collider[] tocandoAlNacer = Physics.OverlapSphere(transform.position, radioImpacto);
        foreach (Collider col in tocandoAlNacer)
        {
            if (ProcesarImpacto(col)) return;
        }
    }

    private void Update()
    {
        if (impactoRealizado) return;

        float distanciaPaso = velocidad * Time.deltaTime;

        // Raycast continuo entre la posición actual y la siguiente para no atravesar enemigos cercanos
        if (Physics.SphereCast(transform.position, radioImpacto, transform.forward, out RaycastHit hit, distanciaPaso, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (ProcesarImpacto(hit.collider))
            {
                transform.position = hit.point;
                return;
            }
        }

        transform.position += transform.forward * distanciaPaso;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (impactoRealizado) return;
        ProcesarImpacto(other);
    }

    private bool ProcesarImpacto(Collider other)
    {
        // 1. Ignora al propio robot que disparó o cualquiera de sus piezas
        if (tirador != null && (other.transform == tirador || other.transform.IsChildOf(tirador)))
        {
            return false;
        }

        // 2. Ignora otros Triggers (como sensores o la Estrella)
        if (other.isTrigger) return false;

        // 3. Busca IDamageable en el objeto golpeado o en su padre
        IDamageable objetivo = other.GetComponentInParent<IDamageable>();
        if (objetivo != null)
        {
            objetivo.RecibirDano(dano);
        }

        if (SFXManager.Instancia != null)
        {
            SFXManager.Instancia.ReproducirImpacto(transform.position);
        }

        impactoRealizado = true;
        Destroy(gameObject);
        return true;
    }
}