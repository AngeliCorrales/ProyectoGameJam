using System;
using UnityEngine;

/// Tecla E: reporta lo que el jugador tiene delante.
///  - Anomalía activa y no reportada -> acierto (cuenta UNA sola vez).
///  - Anomalía ya reportada          -> aviso, no cuenta nada.
///  - Anomalía aún no activa u objeto normal (DecoyReportable) -> error.
///  - Puerta de salida               -> se abre si todas las anomalías están reportadas.
public class AnomalyReporter : MonoBehaviour
{
    public Camera playerCamera;
    public float reach = GameSettings.ReportReach;

    public bool InputEnabled { get; set; }
    public string HintText { get; private set; } = "";

    public event Action<Anomaly> CorrectReport;
    public event Action<string> WrongReport;
    public event Action<string> Notice;
    public event Action DoorOpened;

    private float nextAllowedTime;

    private void Update()
    {
        HintText = "";
        if (!InputEnabled || playerCamera == null) return;

        Anomaly anomaly = null;
        DecoyReportable decoy = null;
        ExitDoor door = null;

        Transform camT = playerCamera.transform;
        if (Physics.Raycast(camT.position, camT.forward, out RaycastHit hit, reach,
                            ViewUtil.IgnoreMask, QueryTriggerInteraction.Collide))
        {
            anomaly = hit.collider.GetComponentInParent<Anomaly>();
            if (anomaly == null) decoy = hit.collider.GetComponentInParent<DecoyReportable>();
            if (anomaly == null && decoy == null) door = hit.collider.GetComponentInParent<ExitDoor>();
        }

        if (anomaly != null)
        {
            if (!anomaly.IsReported) HintText = "[E] Reportar anomalía";
        }
        else if (decoy != null)
        {
            HintText = "[E] Reportar anomalía";
        }
        else if (door != null && !door.IsOpen)
        {
            HintText = door.Unlocked ? "[E] Abrir la puerta" : "[E] Puerta cerrada";
        }

        if (!GameInput.InteractPressed) return;
        if (anomaly == null && decoy == null && door == null) return;
        if (Time.unscaledTime < nextAllowedTime) return;
        nextAllowedTime = Time.unscaledTime + GameSettings.ReportCooldown;

        if (anomaly != null)
        {
            if (anomaly.IsReported)
                Notice?.Invoke("Ya has reportado esto.");
            else if (anomaly.TryReport())
                CorrectReport?.Invoke(anomaly);
            else
                WrongReport?.Invoke("Aquí no pasa nada raro.");
        }
        else if (decoy != null)
        {
            WrongReport?.Invoke("Eso es normal. No hay nada raro aquí.");
        }
        else if (door != null)
        {
            if (door.IsOpen) return;
            if (door.Unlocked)
            {
                door.Open();
                DoorOpened?.Invoke();
            }
            else
            {
                Notice?.Invoke("La puerta no se abre. Todavía hay algo raro en el pasillo.");
            }
        }
    }
}
