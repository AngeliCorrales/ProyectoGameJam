using System.Collections;
using UnityEngine;

/// Anomalía 2: una luz parpadea y se apaga de forma inesperada.
public class LightAnomaly : Anomaly
{
    public float activateAfter = GameSettings.LightAfter;
    public Light lightSource;
    public Renderer panel;
    public AudioSource audioSource;
    public Color onColor = new Color(1f, 0.97f, 0.85f);
    public Color offColor = new Color(0.06f, 0.06f, 0.06f);

    private Coroutine routine;

    protected override bool ShouldActivate()
    {
        return Elapsed >= activateAfter;
    }

    protected override void OnActivate()
    {
        routine = StartCoroutine(FlickerThenOff());
    }

    protected override void OnReported()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        if (audioSource != null) audioSource.Stop();
        SetLamp(true);
    }

    private IEnumerator FlickerThenOff()
    {
        if (audioSource != null)
        {
            audioSource.clip = ProceduralAudio.Flicker;
            audioSource.Play();
        }
        for (int i = 0; i < 6; i++)
        {
            SetLamp(i % 2 == 1);
            yield return new WaitForSeconds(Random.Range(0.05f, 0.14f));
        }
        SetLamp(false);
        routine = null;
    }

    private void SetLamp(bool on)
    {
        if (lightSource != null) lightSource.enabled = on;
        if (panel != null) MaterialUtil.Tint(panel.material, on ? onColor : offColor);
    }
}
