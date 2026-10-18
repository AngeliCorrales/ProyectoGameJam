using UnityEngine;

/// Sonidos generados por código: sin archivos de audio, sin licencias.
public static class ProceduralAudio
{
    private const int SampleRate = 22050;
    private const float TwoPi = Mathf.PI * 2f;

    private static AudioClip hum, chime, buzz, step, creak, flicker, lose;

    public static AudioClip Hum
    {
        get
        {
            if (hum == null)
                hum = Create("hum", 2f, t =>
                    0.16f * Mathf.Sin(TwoPi * 50f * t) +
                    0.09f * Mathf.Sin(TwoPi * 100f * t) +
                    0.04f * Mathf.Sin(TwoPi * 150f * t) +
                    (Random.value - 0.5f) * 0.02f);
            return hum;
        }
    }

    public static AudioClip Chime
    {
        get
        {
            if (chime == null)
                chime = Create("chime", 0.7f, t =>
                    0.35f * Mathf.Exp(-5f * t) *
                    (Mathf.Sin(TwoPi * 660f * t) + 0.6f * Mathf.Sin(TwoPi * 990f * t)));
            return chime;
        }
    }

    public static AudioClip Buzz
    {
        get
        {
            if (buzz == null)
                buzz = Create("buzz", 0.4f, t =>
                {
                    float env = 1f - t / 0.4f;
                    float sq = Mathf.Sin(TwoPi * 95f * t) > 0f ? 1f : -1f;
                    return 0.3f * env * sq + 0.1f * env * (Random.value - 0.5f);
                });
            return buzz;
        }
    }

    public static AudioClip Step
    {
        get
        {
            if (step == null)
                step = Create("step", 0.18f, t =>
                    0.7f * Mathf.Exp(-28f * t) * Mathf.Sin(TwoPi * 75f * t) +
                    0.25f * Mathf.Exp(-70f * t) * (Random.value * 2f - 1f));
            return step;
        }
    }

    public static AudioClip Creak
    {
        get
        {
            if (creak == null)
                creak = Create("creak", 1.3f, t =>
                {
                    float env = Mathf.Sin(Mathf.PI * t / 1.3f);
                    return 0.25f * env * Mathf.Sin(TwoPi * (170f * t + 12f * Mathf.Sin(TwoPi * 4f * t)));
                });
            return creak;
        }
    }

    public static AudioClip Flicker
    {
        get
        {
            if (flicker == null)
                flicker = Create("flicker", 0.9f, t =>
                {
                    float env = 1f - t / 0.9f;
                    float gate = Mathf.Sin(TwoPi * 18f * t) > 0.2f ? 1f : 0f;
                    return gate * env * 0.22f * (Random.value * 2f - 1f) +
                           0.12f * env * Mathf.Sin(TwoPi * 100f * t);
                });
            return flicker;
        }
    }

    public static AudioClip Lose
    {
        get
        {
            if (lose == null)
                lose = Create("lose", 2f, t =>
                    0.4f * Mathf.Exp(-1.5f * t) *
                    (Mathf.Sin(TwoPi * 55f * t) + Mathf.Sin(TwoPi * 58f * t)));
            return lose;
        }
    }

    private static AudioClip Create(string name, float seconds, System.Func<float, float> generator)
    {
        int n = Mathf.CeilToInt(seconds * SampleRate);
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
            data[i] = Mathf.Clamp(generator(i / (float)SampleRate), -1f, 1f);

        AudioClip clip = AudioClip.Create(name, n, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
