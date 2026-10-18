using UnityEngine;

/// Utilidades para saber si el jugador está viendo un objeto.
public static class ViewUtil
{
    private static readonly Plane[] planes = new Plane[6];

    /// El jugador vive en la capa 2 (Ignore Raycast); los rayos de juego la excluyen.
    public static readonly int IgnoreMask = ~(1 << 2);

    /// true si el collider está dentro del campo de visión de la cámara, a menos de
    /// maxDistance y sin nada sólido en medio. "owner" es el objeto al que pertenece el collider.
    public static bool IsSeen(Camera cam, Collider target, Transform owner, float maxDistance)
    {
        if (cam == null || target == null || owner == null || !target.enabled) return false;

        Bounds b = target.bounds;
        GeometryUtility.CalculateFrustumPlanes(cam, planes);
        if (!GeometryUtility.TestPlanesAABB(planes, b)) return false;

        Vector3 origin = cam.transform.position;
        Vector3 toTarget = b.center - origin;
        float dist = toTarget.magnitude;
        if (dist > maxDistance) return false;
        if (dist < 0.05f) return true;

        if (Physics.Raycast(origin, toTarget / dist, out RaycastHit hit, dist, IgnoreMask,
                            QueryTriggerInteraction.Ignore))
        {
            // Si lo primero que toca el rayo es parte del propio objeto, se ve. Si no, hay algo en medio.
            return hit.collider.transform.IsChildOf(owner);
        }
        return true;
    }
}
