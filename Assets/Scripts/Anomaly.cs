using System;
using UnityEngine;

/// Base de todas las anomalías. Garantiza que cada una se pueda reportar con éxito UNA sola vez.
public abstract class Anomaly : MonoBehaviour
{
    public string AnomalyName { get; private set; } = "Anomalía";
    public bool IsActive { get; private set; }
    public bool IsReported { get; private set; }

    protected Camera PlayerCamera { get; private set; }
    protected float Elapsed { get; private set; }

    public event Action<Anomaly> Activated;

    public void Configure(string displayName, Camera playerCamera)
    {
        AnomalyName = displayName;
        PlayerCamera = playerCamera;
    }

    private void Update()
    {
        if (IsReported) return;
        Elapsed += Time.deltaTime;                    // no avanza en pausa (timeScale = 0)
        if (!IsActive && ShouldActivate()) Activate();
    }

    /// Atajo de pruebas: activa la anomalía ya (F1 en el Editor).
    public void ForceActivate()
    {
        if (!IsReported) Activate();
    }

    private void Activate()
    {
        if (IsActive || IsReported) return;
        IsActive = true;
        OnActivate();
        Activated?.Invoke(this);
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

    protected abstract bool ShouldActivate();
    protected abstract void OnActivate();
    protected abstract void OnReported();
}
