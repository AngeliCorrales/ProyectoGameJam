using UnityEngine;

/// "¿Lo está viendo el jugador?": dentro del campo de visión, a distancia razonable y sin nada sólido en medio.
public static class ViewUtil
{
    private static readonly Plane[] planes = new Plane[6];

    /// El jugador vive en la capa 2 (Ignore Raycast); los rayos de juego la excluyen.
    public const int PlayerLayer = 2;
    public static readonly int RayMask = ~(1 << PlayerLayer);

    /// Caja en el mundo a partir de un BoxCollider (aunque esté desactivado).
    public static Bounds WorldBounds(BoxCollider box)
    {
        Transform t = box.transform;
        Vector3 c = box.center, e = box.size * 0.5f;
        Bounds b = new Bounds(t.TransformPoint(c), Vector3.zero);
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
            b.Encapsulate(t.TransformPoint(corner));
        }
        return b;
    }

    /// true si alguna parte de "bounds" se ve desde la cámara (hasta maxDistance) sin obstáculos.
    /// "owner": los colliders hijos de este transform no cuentan como obstáculo.
    public static bool IsSeen(Camera cam, Bounds bounds, Transform owner, float maxDistance)
    {
        if (cam == null) return false;

        GeometryUtility.CalculateFrustumPlanes(cam, planes);
        if (!GeometryUtility.TestPlanesAABB(planes, bounds)) return false;

        Vector3 origin = cam.transform.position;
        if ((bounds.ClosestPoint(origin) - origin).magnitude > maxDistance) return false;
        if (bounds.Contains(origin)) return true;

        // Centro y cuatro puntos interiores: basta con que uno sea visible.
        Vector3 c = bounds.center, e = bounds.extents * 0.7f;
        if (PointVisible(origin, c, owner)) return true;
        if (PointVisible(origin, c + new Vector3(e.x, e.y, e.z), owner)) return true;
        if (PointVisible(origin, c + new Vector3(-e.x, e.y, -e.z), owner)) return true;
        if (PointVisible(origin, c + new Vector3(e.x, -e.y, -e.z), owner)) return true;
        if (PointVisible(origin, c + new Vector3(-e.x, -e.y, e.z), owner)) return true;
        return false;
    }

    private static bool PointVisible(Vector3 origin, Vector3 point, Transform owner)
    {
        Vector3 d = point - origin;
        float dist = d.magnitude;
        if (dist < 0.05f) return true;
        RaycastHit hit;
        if (Physics.Raycast(origin, d / dist, out hit, dist - 0.02f, RayMask, QueryTriggerInteraction.Ignore))
            return owner != null && hit.collider.transform.IsChildOf(owner);
        return true;
    }
}
