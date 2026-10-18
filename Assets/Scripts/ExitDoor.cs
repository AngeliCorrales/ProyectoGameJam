using UnityEngine;

/// Puerta de salida con bisagra. Está bloqueada hasta reportar todas las anomalías.
public class ExitDoor : MonoBehaviour
{
    public bool Unlocked;
    public Collider leafCollider;
    public float openAngle = -95f;
    public float openSeconds = 1.5f;

    public bool IsOpen { get; private set; }

    private bool opening;
    private float t;

    public void Open()
    {
        if (!Unlocked || IsOpen) return;
        IsOpen = true;
        opening = true;
        if (leafCollider != null) leafCollider.enabled = false;
    }

    private void Update()
    {
        if (!opening) return;
        t = Mathf.Min(1f, t + Time.deltaTime / openSeconds);
        transform.localRotation = Quaternion.Euler(0f, openAngle * Mathf.SmoothStep(0f, 1f, t), 0f);
        if (t >= 1f) opening = false;
    }
}
