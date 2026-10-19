using System;
using UnityEngine;

/// Lo que el jugador tiene delante y la tecla E (o clic izquierdo):
///  - Objeto señalable (Reportable / Anomaly) -> se envía como reporte al GameManager.
///  - Puerta (Interactable)                   -> se usa.
public class AnomalyReporter : MonoBehaviour
{
    public Camera playerCamera;
    public PlayerController player;      // en tercera persona el rayo sale desde más atrás
    public float reach = GameSettings.ReportReach;

    public bool InputEnabled { get; set; }
    public string HintText { get; private set; }
    public bool AimingAtReportable { get; private set; }

    public event Action<Reportable> ReportPressed;
    public event Action<Interactable> InteractPressed;

    private float nextAllowedTime;

    private void Update()
    {
        HintText = "";
        AimingAtReportable = false;
        if (!InputEnabled || playerCamera == null) return;

        Reportable reportable = null;
        Interactable interactable = null;

        Transform camT = playerCamera.transform;
        RaycastHit hit;
        float maxDist = reach + (player != null ? player.CameraExtraReach : 0f);
        if (Physics.Raycast(camT.position, camT.forward, out hit, maxDist, ViewUtil.RayMask, QueryTriggerInteraction.Collide))
        {
            reportable = hit.collider.GetComponentInParent<Reportable>();
            if (reportable == null) interactable = hit.collider.GetComponentInParent<Interactable>();
        }

        if (reportable != null)
        {
            Anomaly a = reportable as Anomaly;
            AimingAtReportable = true;
            HintText = (a != null && a.IsReported) ? "Ya reportado" : "[E] Reportar anomalía";
        }
        else if (interactable != null)
        {
            HintText = interactable.Prompt;
        }

        if (!GameInput.InteractPressed) return;
        if (reportable == null && interactable == null) return;
        if (Time.unscaledTime < nextAllowedTime) return;
        nextAllowedTime = Time.unscaledTime + GameSettings.ReportCooldown;

        if (reportable != null)
        {
            if (ReportPressed != null) ReportPressed(reportable);
        }
        else
        {
            if (InteractPressed != null) InteractPressed(interactable);
        }
    }
}
