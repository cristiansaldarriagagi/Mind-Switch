using UnityEngine;

public class RobotBase : MonoBehaviour
{
    [Header("Identificación del Robot")]
    public string nombreRobot = "Robot Genérico";

    [Header("Cámara del Robot")]
    public Camera camaraRobot;

    [HideInInspector] public bool estaPoseido = false;

    protected virtual void Start()
    {
        ActualizarEstadoPossesion(estaPoseido);
    }

    public virtual void Poseer()
    {
        estaPoseido = true;
        ActualizarEstadoPossesion(true);
        Debug.Log("Poseyendo a: " + nombreRobot);
    }

    public virtual void Desposeer()
    {
        estaPoseido = false;
        ActualizarEstadoPossesion(false);
    }

    // Método para mantener compatibilidad total con PossessionSystem.cs
    public virtual void EstablecerPoseido(bool poseido)
    {
        if (poseido)
        {
            Poseer();
        }
        else
        {
            Desposeer();
        }
    }

    protected virtual void ActualizarEstadoPossesion(bool poseido)
    {
        if (camaraRobot != null)
        {
            camaraRobot.gameObject.SetActive(poseido);
        }
    }
}