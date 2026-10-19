using UnityEngine;

/// Opciones del jugador guardadas en PlayerPrefs (sensibilidad del ratón y volumen).
public static class Options
{
    private const string SensKey = "nmdv_sensitivity";
    private const string VolKey = "nmdv_volume";
    private const string InvertKey = "nmdv_invert_y";
    private const string BrightKey = "nmdv_brightness_v4";
    private const string ViewKey = "nmdv_third_person";

    /// Cámara en tercera persona (se cambia con V).
    public static bool ThirdPerson
    {
        get { return PlayerPrefs.GetInt(ViewKey, 0) == 1; }
        set { PlayerPrefs.SetInt(ViewKey, value ? 1 : 0); }
    }

    /// Multiplicador de brillo (luces y luz ambiente).
    public static float Brightness
    {
        get { return PlayerPrefs.GetFloat(BrightKey, GameSettings.Brightness); }
        set { PlayerPrefs.SetFloat(BrightKey, Mathf.Clamp(value, 0.5f, 2.5f)); }
    }

    public static float Sensitivity
    {
        get { return PlayerPrefs.GetFloat(SensKey, 1f); }
        set { PlayerPrefs.SetFloat(SensKey, Mathf.Clamp(value, 0.2f, 3f)); }
    }

    public static float Volume
    {
        get { return PlayerPrefs.GetFloat(VolKey, 0.8f); }
        set
        {
            float v = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolKey, v);
            AudioListener.volume = v;
        }
    }

    public static bool InvertY
    {
        get { return PlayerPrefs.GetInt(InvertKey, 0) == 1; }
        set { PlayerPrefs.SetInt(InvertKey, value ? 1 : 0); }
    }

    public static void Apply()
    {
        AudioListener.volume = Volume;
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }
}
