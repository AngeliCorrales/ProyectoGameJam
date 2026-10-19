using System.Collections;
using UnityEngine;

/// Una lámpara del techo parpadea con un chisporroteo y se apaga. Al reportarla vuelve a encenderse.
public class LightAnomaly : Anomaly
{
    public Light lightSource;
    public Renderer panel;                     // tiene material propio (instancia)
    public AudioSource audioSource;            // sonido 3D en la lámpara
    public Color onEmission = new Color(1.1f, 1.03f, 0.86f);
    public Color offEmission = new Color(0f, 0f, 0f);

    private Coroutine routine;

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
            AudioClip c = GameAssets.Clip("light_flicker");
            if (c != null) audioSource.PlayOneShot(c, 0.9f);
        }
        for (int i = 0; i < 9; i++)
        {
            SetLamp(i % 2 == 1);
            yield return new WaitForSeconds(Random.Range(0.04f, 0.16f));
        }
        SetLamp(false);
        if (audioSource != null)
        {
            AudioClip off = GameAssets.Clip("light_off");
            if (off != null) audioSource.PlayOneShot(off, 0.8f);
        }
        routine = null;
    }

    private void SetLamp(bool on)
    {
        if (lightSource != null) lightSource.enabled = on;
        if (panel != null)
        {
            Material m = panel.sharedMaterial;
            MaterialLibrary.SetEmission(m, on ? onEmission : offEmission, null);
            MaterialLibrary.SetColor(m, on ? Color.white : new Color(0.35f, 0.35f, 0.33f));
        }
    }
}
