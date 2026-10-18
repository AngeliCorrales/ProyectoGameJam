using UnityEngine;

/// Anomalía 3: una silla desaparece cuando el jugador deja de mirarla.
/// El collider (trigger) se mantiene, así se puede reportar el hueco vacío con E.
public class ChairAnomaly : Anomaly
{
    public float activateAfter = GameSettings.ChairAfter;
    public Collider targetCollider;
    public float watchSeconds = GameSettings.ChairWatchSeconds;
    public float fallbackExtra = GameSettings.ChairFallbackExtra;
    public float maxSeenDistance = 14f;

    private Renderer[] renderers;
    private float watched;

    private void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    protected override bool ShouldActivate()
    {
        bool seen = ViewUtil.IsSeen(PlayerCamera, targetCollider, transform, maxSeenDistance);
        if (seen) watched += Time.deltaTime;

        if (Elapsed < activateAfter) return false;
        if (seen) return false;                      // nunca desaparece delante de tus ojos

        // La has mirado un rato y ahora miras a otro lado -> desaparece.
        // Si nunca la miraste, desaparece igualmente pasado un tiempo (siempre fuera de vista).
        return watched >= watchSeconds || Elapsed >= activateAfter + fallbackExtra;
    }

    protected override void OnActivate()
    {
        SetVisible(false);
    }

    protected override void OnReported()
    {
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        if (renderers == null) renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = visible;
    }
}
