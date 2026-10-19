using System;
using UnityEngine;

/// Anomalía "La sombra" (solo a partir del cuarto 3): aparece DETRÁS de ti y te persigue.
/// Si no la miras, corre; si la miras, casi se para. Date la vuelta y repórtala (E) para que desaparezca.
/// Si te alcanza: solo un susto (desaparece). No cuenta como anomalía ni te devuelve al cuarto 0.
public class ShadowChaserAnomaly : Anomaly, IAvatarDriver
{
    public float fastSpeed = 2.3f;     // cuando no la miras (andando a 2,6 m/s apenas te alcanza; si te paras, sí)
    public float slowSpeed = 0.35f;    // cuando la miras
    public float catchDistance = 0.9f;
    public AudioSource voice;

    /// Lo escucha el GameManager: la sombra te ha alcanzado.
    public static event Action Caught;

    private Renderer[] renderers;
    private PlayerController player;
    private float speed, phase;
    private bool caught;

    public bool ThirdPerson { get { return true; } }
    public float Speed { get { return speed; } }
    public float WalkPhase { get { return phase; } }
    public float Pitch { get { return 0f; } }

    public void Init()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        SetVisible(false);
        if (zone != null) zone.enabled = false;
    }

    protected override void OnActivate()
    {
        player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            // Aparece unos metros detrás de ti (nunca fuera del pasillo)
            Vector3 p = player.transform.position;
            Vector3 back = -player.transform.forward; back.y = 0f;
            if (back.sqrMagnitude < 0.01f) back = Vector3.back;
            Vector3 pos = p + back.normalized * 6f;
            pos.x = Mathf.Clamp(pos.x, -1.3f, 1.3f);
            pos.z = Mathf.Clamp(pos.z, 0.6f, 29.4f);
            pos.y = 0f;
            transform.position = pos;
        }
        caught = false;
        SetVisible(true);
        if (zone != null) zone.enabled = true;
        if (voice != null)
        {
            voice.clip = GameAssets.Clip("anomaly_cue");
            voice.loop = true;
            voice.pitch = 0.75f;
            if (voice.clip != null) voice.Play();
        }
    }

    protected override void OnReported()
    {
        SetVisible(false);
        if (zone != null) zone.enabled = false;
        if (voice != null) voice.Stop();
        speed = 0f;
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
        if (!IsActive || player == null || caught || Time.deltaTime <= 0f) return;

        Vector3 to = player.transform.position - transform.position; to.y = 0f;
        float dist = to.magnitude;
        if (dist < catchDistance)
        {
            caught = true;
            if (Caught != null) Caught();
            return;
        }

        bool seen = ViewUtil.IsSeen(PlayerCamera, WorldBounds, transform, 30f);
        float v = seen ? slowSpeed : fastSpeed;
        Vector3 step = to / dist * Mathf.Min(v * Time.deltaTime, dist);
        transform.position += step;
        transform.rotation = Quaternion.LookRotation(to / dist, Vector3.up);
        speed = v;
        phase += Time.deltaTime * v * 2.9f;
    }
}
