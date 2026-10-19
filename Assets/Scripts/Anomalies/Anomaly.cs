using UnityEngine;

/// Base de todas las anomalías.
///  - Hasta que el director la activa, es un objeto normal (reportarla = error).
///  - Al activarse cambia algo (normalmente solo cuando el jugador NO la está mirando).
///  - Al reportarla vuelve a la normalidad y cuenta UNA sola vez.
public abstract class Anomaly : Reportable
{
    [Tooltip("Nombre que se muestra al reportarla")]
    public string anomalyName = "Anomalía";
    [Tooltip("Solo puede ocurrir si el jugador no la está viendo")]
    public bool requiresUnseen = true;
    [Tooltip("Distancia mínima al jugador para poder ocurrir (0 = sin límite)")]
    public float minPlayerDistance = 0f;
    public float maxSeenDistance = 28f;

    public bool IsActive { get; private set; }
    public bool IsReported { get; private set; }
    public float ActiveSeconds { get; private set; }

    protected Camera PlayerCamera { get; private set; }

    public void Bind(Camera playerCamera)
    {
        PlayerCamera = playerCamera;
    }

    /// ¿Puede ocurrir ahora mismo?
    public bool CanActivate()
    {
        if (IsActive || IsReported) return false;
        if (PlayerCamera != null && minPlayerDistance > 0f)
        {
            Vector3 p = PlayerCamera.transform.position; p.y = 0f;
            Vector3 me = WorldBounds.center; me.y = 0f;
            if (Vector3.Distance(p, me) < minPlayerDistance) return false;
        }
        if (requiresUnseen && ViewUtil.IsSeen(PlayerCamera, WorldBounds, transform, maxSeenDistance)) return false;
        return true;
    }

    public void Activate()
    {
        if (IsActive || IsReported) return;
        IsActive = true;
        ActiveSeconds = 0f;
        OnActivate();
    }

    /// Devuelve true solo la primera vez que se reporta una anomalía activa.
    public bool TryReport()
    {
        if (!IsActive || IsReported) return false;
        IsReported = true;
        IsActive = false;
        OnReported();
        return true;
    }

    protected virtual void Update()
    {
        if (IsActive) ActiveSeconds += Time.deltaTime;
    }

    protected abstract void OnActivate();
    protected abstract void OnReported();
}
