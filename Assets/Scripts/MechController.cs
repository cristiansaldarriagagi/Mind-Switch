using UnityEngine;
using UnityEngine.InputSystem;
using System.Reflection;

[DefaultExecutionOrder(1000)]
public class MechController : MonoBehaviour
{
    [Header("Identificación del Robot")]
    public string nombreRobot = "Robot Genérico";

    [Header("Cámara Primera Persona (Cabina del Mech)")]
    public Camera camaraRobot;
    public float sensibilidadRaton = 0.22f;
    public float alturaOjos = 1.48f;
    public float adelanteCabina = 0.62f;
    public float limiteMirarArribaAbajo = 55f;
    public bool bloquearCursorEnJuego = true;

    [Header("Movimiento")]
    public float velocidad = 4.0f;
    public float velocidadRotacion = 20f;
    public Animator anim;

    [Header("Propulsores / Dash (Shift o Espacio)")]
    public float velocidadDash = 12f;
    public float duracionDash = 0.16f;
    public float enfriamientoDash = 1.5f;

    [Header("Sistema de Disparo")]
    public GameObject prefabBala;
    public Transform[] puntosDisparo;
    public float cadenciaDisparo = 0.15f;
    public float velocidadBala = 30f;

    private Rigidbody rb;
    private Collider[] misColliders;
    private float anguloYaw = 0f;
    private float anguloPitch = 0f;
    private float timerDisparo = 0f;
    private int indicePuntoDisparo = 0;

    private float timerDash = 0f;
    private float timerCooldownDash = 0f;
    private Vector3 direccionDash = Vector3.zero;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        misColliders = GetComponentsInChildren<Collider>(true);

        if (camaraRobot == null) camaraRobot = Camera.main;
        if (anim == null) anim = GetComponentInChildren<Animator>();

        anguloYaw = transform.eulerAngles.y;
        anguloPitch = 0f;

        if (camaraRobot != null)
        {
            camaraRobot.nearClipPlane = 0.15f;
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f)
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            return;
        }

        if (bloquearCursorEnJuego && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (timerDisparo > 0f) timerDisparo -= Time.deltaTime;
        if (timerCooldownDash > 0f) timerCooldownDash -= Time.deltaTime;

        // 1. Giro de vista en primera persona con el ratón
        Vector2 deltaRaton = Vector2.zero;
        if (Mouse.current != null)
            deltaRaton = Mouse.current.delta.ReadValue() * sensibilidadRaton;
        else
            deltaRaton = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * (sensibilidadRaton * 12f);

        anguloYaw += deltaRaton.x;
        anguloPitch = Mathf.Clamp(anguloPitch - deltaRaton.y, -limiteMirarArribaAbajo, limiteMirarArribaAbajo);

        // 2. Direcciones horizontales: W = Adelante, S = Atrás, A = Izquierda, D = Derecha
        Quaternion rotHorizontal = Quaternion.Euler(0f, anguloYaw, 0f);
        Vector3 frente = rotHorizontal * Vector3.forward;
        Vector3 derecha = rotHorizontal * Vector3.right;

        float inputAdelante = 0f;
        float inputDerecha = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputAdelante += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputAdelante -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputDerecha += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputDerecha -= 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) inputAdelante += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) inputAdelante -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) inputDerecha += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) inputDerecha -= 1f;
        }

        Vector3 dirMovimiento = (frente * inputAdelante + derecha * inputDerecha);
        if (dirMovimiento.sqrMagnitude > 1f) dirMovimiento.Normalize();

        // 3. Dash táctico con Shift o Espacio
        bool pulsarDash = Keyboard.current != null
            ? (Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
            : (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.Space));

        if (pulsarDash && timerCooldownDash <= 0f && timerDash <= 0f)
        {
            direccionDash = dirMovimiento.sqrMagnitude > 0.01f ? dirMovimiento : frente;
            timerDash = duracionDash;
            timerCooldownDash = enfriamientoDash;
        }

        // 4. Movimiento físico
        Vector3 velocidadFinal = timerDash > 0f ? direccionDash * velocidadDash : dirMovimiento * velocidad;
        if (timerDash > 0f) timerDash -= Time.deltaTime;

        if (rb != null && !rb.isKinematic)
            rb.linearVelocity = new Vector3(velocidadFinal.x, rb.linearVelocity.y, velocidadFinal.z);
        else
            transform.position += velocidadFinal * Time.deltaTime;

        // 5. Rotación del cuerpo del Mech alineada con la mirada
        transform.rotation = Quaternion.Slerp(transform.rotation, rotHorizontal, velocidadRotacion * Time.deltaTime);

        // 6. Animación de pasos en SK_ISO_Mech
        if (anim != null && anim.isActiveAndEnabled)
        {
            bool caminando = velocidadFinal.magnitude > 0.15f;
            if (anim.parameterCount == 0)
                anim.speed = caminando ? 1f : 0f;
            else
            {
                foreach (var p in anim.parameters)
                {
                    string n = p.name.ToLower();
                    if (p.type == AnimatorControllerParameterType.Float && (n.Contains("speed") || n.Contains("vel")))
                        anim.SetFloat(p.name, caminando ? 1f : 0f);
                    else if (p.type == AnimatorControllerParameterType.Bool && (n.Contains("walk") || n.Contains("move") || n.Contains("run")))
                        anim.SetBool(p.name, caminando);
                }
            }
        }

        // 7. Disparo frontal desde los cañones del Mech
        bool disparar = Mouse.current != null ? Mouse.current.leftButton.isPressed : Input.GetMouseButton(0);
        if (disparar && timerDisparo <= 0f)
        {
            Disparar(frente);
            timerDisparo = cadenciaDisparo;
        }
    }

    private void Disparar(Vector3 direccionDisparo)
    {
        if (prefabBala == null) return;

        Vector3 dirPlana = new Vector3(direccionDisparo.x, 0f, direccionDisparo.z).normalized;
        float alturaCanon = transform.position.y + 1.15f;
        Vector3 origen = transform.position + dirPlana * 1.55f;
        origen.y = alturaCanon;

        if (puntosDisparo != null && puntosDisparo.Length > 0)
        {
            Transform fp = puntosDisparo[indicePuntoDisparo % puntosDisparo.Length];
            indicePuntoDisparo++;
            if (fp != null)
            {
                origen = fp.position + dirPlana * 0.95f;
                alturaCanon = origen.y;
            }
        }

        GameObject bala = Instantiate(prefabBala, origen, Quaternion.LookRotation(dirPlana));
        bala.tag = "Player";

        foreach (Collider cBala in bala.GetComponentsInChildren<Collider>(true))
        {
            if (cBala == null) continue;
            foreach (Collider cMech in misColliders)
                if (cMech != null) Physics.IgnoreCollision(cBala, cMech, true);
        }

        Bala scriptBala = bala.GetComponent<Bala>();
        if (scriptBala != null)
        {
            FieldInfo[] campos = typeof(Bala).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo f in campos)
            {
                string nombreCampo = f.Name.ToLower();
                if (f.FieldType == typeof(GameObject))
                    f.SetValue(scriptBala, gameObject);
                else if (f.FieldType == typeof(Transform))
                    f.SetValue(scriptBala, transform);
                else if (f.FieldType == typeof(string) && (nombreCampo.Contains("tag") || nombreCampo.Contains("bando") || nombreCampo.Contains("owner")))
                    f.SetValue(scriptBala, "Player");
                else if (f.FieldType == typeof(bool) && (nombreCampo.Contains("jugador") || nombreCampo.Contains("player") || nombreCampo.Contains("aliado")))
                    f.SetValue(scriptBala, true);
                else if (f.FieldType == typeof(float) && (nombreCampo.Contains("vel") || nombreCampo.Contains("speed")))
                    f.SetValue(scriptBala, 0f);
            }
        }

        Rigidbody rbBala = bala.GetComponent<Rigidbody>();
        if (rbBala != null)
        {
            rbBala.useGravity = false;
            rbBala.isKinematic = true;
        }

        var mover = bala.AddComponent<ImpulsoBalaKinematic>();
        mover.direccion = dirPlana;
        mover.velocidad = velocidadBala;
        mover.alturaFija = alturaCanon;
    }

    void LateUpdate()
    {
        if (camaraRobot == null || Time.timeScale == 0f) return;

        Quaternion rotHorizontal = Quaternion.Euler(0f, anguloYaw, 0f);
        Vector3 frente = rotHorizontal * Vector3.forward;

        // Posiciona la cámara exactamente en la cabina frontal del Mech
        Vector3 posCabina = transform.position + Vector3.up * alturaOjos + frente * adelanteCabina;
        camaraRobot.transform.position = posCabina;
        camaraRobot.transform.rotation = Quaternion.Euler(anguloPitch, anguloYaw, 0f);
    }
}

[DefaultExecutionOrder(1005)]
public class ImpulsoBalaKinematic : MonoBehaviour
{
    public Vector3 direccion;
    public float velocidad = 30f;
    public float alturaFija;

    void LateUpdate()
    {
        Vector3 sigPos = transform.position + direccion * velocidad * Time.deltaTime;
        sigPos.y = alturaFija;
        transform.position = sigPos;
    }
}