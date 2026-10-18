using UnityEngine;

/// Anomalía 1: un cuadro cambia de posición y se ladea cuando el jugador no lo está mirando.
public class PaintingAnomaly : Anomaly
{
    public float activateAfter = GameSettings.PaintingAfter;
    public Collider targetCollider;                                   // collider del marco
    public Vector3 positionOffset = new Vector3(0.55f, -0.12f, 0f);   // en el eje local del cuadro
    public Vector3 rotationOffset = new Vector3(0f, 0f, 9f);          // giro dentro del plano de la pared
    public float maxSeenDistance = 30f;

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    private void Awake()
    {
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
    }

    protected override bool ShouldActivate()
    {
        if (Elapsed < activateAfter) return false;
        // Solo cambia cuando el jugador NO lo está viendo.
        return !ViewUtil.IsSeen(PlayerCamera, targetCollider, transform, maxSeenDistance);
    }

    protected override void OnActivate()
    {
        transform.localPosition = originalLocalPosition + originalLocalRotation * positionOffset;
        transform.localRotation = originalLocalRotation * Quaternion.Euler(rotationOffset);
    }

    protected override void OnReported()
    {
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
    }
}
