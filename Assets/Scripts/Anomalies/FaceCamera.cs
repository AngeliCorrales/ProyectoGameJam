using UnityEngine;

/// Gira el objeto (solo en horizontal) para que siempre mire a la cámara del jugador. Lo usa la figura del fondo.
public class FaceCamera : MonoBehaviour
{
    public Camera target;
    public float turnSpeed = 90f;   // grados por segundo: gira despacio, da más miedo

    private void Update()
    {
        if (target == null) return;
        Vector3 d = target.transform.position - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude < 0.0001f) return;
        Quaternion want = Quaternion.LookRotation(d.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
    }
}
