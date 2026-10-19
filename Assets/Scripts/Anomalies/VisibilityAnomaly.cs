using UnityEngine;

/// Algo desaparece (Hide) o aparece (Show).
///  - Hide: la zona señalable se mantiene, así puedes reportar el hueco vacío.
///  - Show: el objeto está oculto y sin zona hasta que aparece (no se puede reportar el aire antes de tiempo).
public class VisibilityAnomaly : Anomaly
{
    public enum Mode { Hide, Show }
    public Mode mode = Mode.Hide;

    private Renderer[] renderers;
    private Collider[] solids;

    /// Llamar después de construir el objeto (LevelBuilder lo hace).
    public void Init()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        Collider[] all = GetComponentsInChildren<Collider>(true);
        int n = 0;
        for (int i = 0; i < all.Length; i++) if (!all[i].isTrigger) n++;
        solids = new Collider[n];
        n = 0;
        for (int i = 0; i < all.Length; i++) if (!all[i].isTrigger) solids[n++] = all[i];

        SetPresent(mode == Mode.Hide);
        if (zone != null) zone.enabled = mode == Mode.Hide;
    }

    protected override void OnActivate()
    {
        if (renderers == null) Init();
        if (mode == Mode.Hide)
        {
            SetPresent(false);
        }
        else
        {
            SetPresent(true);
            if (zone != null) zone.enabled = true;
        }
    }

    protected override void OnReported()
    {
        if (mode == Mode.Hide)
        {
            SetPresent(true);
        }
        else
        {
            SetPresent(false);
            if (zone != null) zone.enabled = false;
        }
    }

    private void SetPresent(bool present)
    {
        if (renderers != null) for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = present;
        if (solids != null) for (int i = 0; i < solids.Length; i++) solids[i].enabled = present;
    }
}
