using UnityEngine;

/// Cualquier objeto del pasillo que se puede señalar con E.
/// Si es un objeto normal (o una anomalía que todavía no ha ocurrido), reportarlo cuenta como ERROR.
public class Reportable : MonoBehaviour
{
    public string displayName = "Objeto";
    /// Zona (trigger) que se usa para apuntar y para saber si el jugador lo está viendo.
    public BoxCollider zone;

    public Bounds WorldBounds
    {
        get { return zone != null ? ViewUtil.WorldBounds(zone) : new Bounds(transform.position, Vector3.one * 0.5f); }
    }
}
