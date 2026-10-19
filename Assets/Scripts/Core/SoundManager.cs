using UnityEngine;

/// Sonido global: ambiente (zumbido de fluorescentes + dron), latido según la tensión y efectos 2D.
public class SoundManager : MonoBehaviour
{
    private AudioSource hum, drone, heart, sfx, ui, music, gameMusic;
    private float tension;          // 0..1
    private float ambienceTarget;

    private void Awake()
    {
        hum = Loop("amb_hum", 0f);
        drone = Loop("amb_drone", 0f);
        heart = Loop("heartbeat", 0f);
        music = Loop("menu_music", 0f);
        gameMusic = Loop("game_music", 0f);
        music.ignoreListenerPause = true;
        sfx = NewSource();
        ui = NewSource();
        ui.ignoreListenerPause = true;      // los clics del menú suenan aunque el juego esté en pausa
        Options.Apply();
    }

    private AudioSource NewSource()
    {
        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.spatialBlend = 0f;
        return s;
    }

    private AudioSource Loop(string clip, float volume)
    {
        AudioSource s = NewSource();
        s.clip = GameAssets.Clip(clip);
        s.loop = true;
        s.volume = volume;
        if (s.clip != null) s.Play();
        return s;
    }

    /// Ambiente encendido (partida) o apagado (menú).
    public void SetAmbience(bool on) { ambienceTarget = on ? 1f : 0.35f; menuMusic = !on; }

    private bool menuMusic = true;

    /// 0 = calma, 1 = a punto de perder.
    public void SetTension(float t) { tension = Mathf.Clamp01(t); }

    public void Play(string clip, float volume = 1f, float pitch = 1f)
    {
        AudioClip c = GameAssets.Clip(clip);
        if (c == null) return;
        sfx.pitch = pitch;
        sfx.PlayOneShot(c, volume);
    }

    public void PlayUI(string clip, float volume = 1f)
    {
        AudioClip c = GameAssets.Clip(clip);
        if (c != null) ui.PlayOneShot(c, volume);
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        music.volume = Mathf.MoveTowards(music.volume, menuMusic ? 0.55f : 0f, dt * 0.6f);
        // Música de fondo de la partida: muy baja
        gameMusic.volume = Mathf.MoveTowards(gameMusic.volume, menuMusic ? 0f : 0.1f, dt * 0.2f);
        hum.volume = Mathf.MoveTowards(hum.volume, 0.32f * ambienceTarget, dt * 0.5f);
        drone.volume = Mathf.MoveTowards(drone.volume, (0.12f + 0.45f * tension) * ambienceTarget, dt * 0.3f);
        drone.pitch = Mathf.MoveTowards(drone.pitch, 1f - 0.15f * tension, dt * 0.2f);
        heart.volume = Mathf.MoveTowards(heart.volume, tension > 0.3f ? (tension - 0.3f) * 1.2f * ambienceTarget : 0f, dt * 0.5f);
        heart.pitch = 0.9f + tension * 0.5f;
    }
}
