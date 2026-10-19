using UnityEngine;

/// Anomalía de posición / giro / tamaño: un cuadro se ladea, una silla se gira, una puerta queda entreabierta,
/// una planta crece, un cajón se abre... El cambio ocurre de golpe mientras no lo miras.
public class TransformAnomaly : Anomaly
{
    public Transform target;                      // lo que se mueve (por defecto, este mismo objeto)
    public Vector3 positionOffset;                // en ejes locales del padre del target
    public Vector3 rotationOffset;                // grados, se suma a la rotación local
    public float scaleMultiplier = 1f;

    private Vector3 originalPos;
    private Quaternion originalRot;
    private Vector3 originalScale;

    protected override void OnActivate()
    {
        if (target == null) target = transform;
        originalPos = target.localPosition;
        originalRot = target.localRotation;
        originalScale = target.localScale;

        target.localPosition = originalPos + positionOffset;
        target.localRotation = originalRot * Quaternion.Euler(rotationOffset);
        target.localScale = originalScale * scaleMultiplier;
    }

    protected override void OnReported()
    {
        if (target == null) return;
        target.localPosition = originalPos;
        target.localRotation = originalRot;
        target.localScale = originalScale;
    }
}
