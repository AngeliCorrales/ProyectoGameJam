using UnityEngine;

/// Anomalía "Otro vigilante": al fondo hay un vigilante idéntico a ti. Cuando caminas, él camina hacia ti
/// con tus mismos pasos, como un reflejo (izquierda y derecha invertidas). Cuando te paras, se para.
public class MirrorGuardAnomaly : Anomaly, IAvatarDriver
{
    public float minDistance = 2.6f;
    public float maxZ = 28.6f;

    private Renderer[] renderers;
    private PlayerController player;
    private float lastPlayerZ;
    private float speed;

    public bool ThirdPerson { get { return true; } }
    public float Speed { get { return speed; } }
    public float WalkPhase { get { return player != null ? player.WalkPhase : 0f; } }
    public float Pitch { get { return 0f; } }

    /// Llamar después de construir el cuerpo (empieza oculto).
    public void Init()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        SetVisible(false);
        if (zone != null) zone.enabled = false;
    }

    protected override void OnActivate()
    {
        SetVisible(true);
        if (zone != null) zone.enabled = true;
        player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null) lastPlayerZ = player.transform.position.z;
    }

    protected override void OnReported()
    {
        SetVisible(false);
        if (zone != null) zone.enabled = false;
    }

    private void SetVisible(bool v)
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = v;
    }

    protected override void Update()
    {
        base.Update();
        speed = 0f;
        if (!IsActive || player == null || Time.deltaTime <= 0f) return;

        Vector3 p = player.transform.position;
        float dz = p.z - lastPlayerZ;
        lastPlayerZ = p.z;

        Vector3 me = transform.position;
        float newZ = Mathf.Clamp(me.z - dz, p.z + minDistance, maxZ);   // tú avanzas -> él avanza hacia ti
        float newX = Mathf.Clamp(-p.x, -1.4f, 1.4f);                   // espejo
        speed = Mathf.Abs(newZ - me.z) / Time.deltaTime;
        transform.position = new Vector3(Mathf.MoveTowards(me.x, newX, Time.deltaTime * 3f), me.y, newZ);
        transform.rotation = Quaternion.Euler(0f, 180f, 0f);
    }
}
