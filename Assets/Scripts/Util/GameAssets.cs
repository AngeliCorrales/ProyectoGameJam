using System.Collections.Generic;
using UnityEngine;

/// Acceso centralizado a los assets de Assets/Resources (texturas, imágenes, audio, fuentes, modelos).
/// Si falta alguno, devuelve null y el juego sigue funcionando (sin ese detalle).
public static class GameAssets
{
    private static readonly Dictionary<string, Object> cache = new Dictionary<string, Object>();

    private static T Load<T>(string path) where T : Object
    {
        Object o;
        if (cache.TryGetValue(path, out o)) return o as T;   // también recuerda los que faltan (sin repetir el aviso)
        T asset = Resources.Load<T>(path);
        if (asset == null) Debug.LogWarning("[NoMiresDosVeces] Falta el asset Resources/" + path);
        cache[path] = asset;
        return asset;
    }

    /// Texturas de materiales: Resources/Textures
    public static Texture2D Texture(string name) { return Load<Texture2D>("Textures/" + name); }

    /// Imágenes 2D (cuadros, carteles, interfaz): Resources/Sprites
    public static Texture2D Sprite(string name) { return Load<Texture2D>("Sprites/" + name); }

    /// Efectos de sonido y ambientes: Resources/Audio
    public static AudioClip Clip(string name) { return Load<AudioClip>("Audio/" + name); }

    /// Fuentes: Resources/Fonts
    public static Font Font(string name) { return Load<Font>("Fonts/" + name); }

    /// Datos binarios (.bytes): Resources/Models
    public static TextAsset Model(string name) { return Load<TextAsset>("Models/" + name); }

    public static void ClearCache() { cache.Clear(); }
}
