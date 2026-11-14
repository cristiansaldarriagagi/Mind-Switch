using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class EnemigoIA : MonoBehaviour
{
    [Header("--- PERSECUCIÓN Y ATAQUE ---")]
    public float velocidad = 4.5f;
    public float rangoAtaque = 3.5f;
    public float danoPorAtaque = 15f;
    public float cadenciaAtaque = 1.2f;

    private NavMeshAgent agente;
    private Animator anim;
    private Transform objetivoActual;
    private float tiempoSiguienteAtaque;

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        agente.speed = velocidad;
        agente.stoppingDistance = rangoAtaque * 0.8f;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void Update()
    {
        BuscarJugadorActivo();

        if (objetivoActual == null || !agente.isOnNavMesh) return;

        // Perseguir siempre al jugador
        agente.SetDestination(objetivoActual.position);

        // Actualizar animación de correr según la velocidad real del agente
        if (anim != null)
        {
            bool estaMoviendose = agente.velocity.sqrMagnitude > 0.1f;
            anim.SetBool("Corriendo", estaMoviendose);
        }

        // Mirar hacia el jugador suavemente
        Vector3 direccion = (objetivoActual.position - transform.position).normalized;
        direccion.y = 0;
        if (direccion != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direccion), Time.deltaTime * 8f);
        }

        // Comprobar si está en rango para atacar
        float distancia = Vector3.Distance(transform.position, objetivoActual.position);
        if (distancia <= rangoAtaque && Time.time >= tiempoSiguienteAtaque)
        {
            Atacar();
            tiempoSiguienteAtaque = Time.time + cadenciaAtaque;
        }
    }

    void BuscarJugadorActivo()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            objetivoActual = player.transform;
        }
    }

    void Atacar()
    {
        if (anim != null)
        {
            anim.SetTrigger("Atacar");
        }

        objetivoActual.SendMessage("RecibirDano", danoPorAtaque, SendMessageOptions.DontRequireReceiver);
    }
}