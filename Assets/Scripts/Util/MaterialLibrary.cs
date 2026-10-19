using System.Collections.Generic;
using UnityEngine;

/// Definición de un material del juego (texturas de Resources/Textures + parámetros).
public class MatDef
{
    public string key;
    public string albedo;            // nombre en Resources/Textures (null = sin textura)
    public string normal;
    public Color color = Color.white;
    public Vector2 tiling = Vector2.one;   // las cajas tienen UV en metros: tiling = 1 / tamaño del motivo en metros
    public float smoothness = 0.2f;
    public float metallic;
    public Color emission = Color.black;
    public string emissionMap;
    public bool cutout;

    public MatDef(string key) { this.key = key; }
}

/// Crea y reparte los materiales. Primero busca Resources/Materials/<clave>.mat (los genera el menú del
/// Editor con el shader del pipeline activo: así entran en la compilación con sus variantes).
/// Si no existen, los crea en tiempo de ejecución a partir del material por defecto (funciona en URP y Built-in).
public static class MaterialLibrary
{
    public static readonly List<MatDef> Definitions = new List<MatDef>();
    private static readonly Dictionary<string, MatDef> defs = new Dictionary<string, MatDef>();
    private static readonly Dictionary<string, Material> shared = new Dictionary<string, Material>();
    private static readonly List<Material> instances = new List<Material>();
    private static Material template;

    static MaterialLibrary()
    {
        Add(new MatDef("Wall") { albedo = "wall_albedo", normal = "wall_normal", tiling = new Vector2(1f / 1.5f, 1f / 1.5f), smoothness = 0.15f });
        Add(new MatDef("Carpet") { albedo = "carpet_albedo", normal = "carpet_normal", tiling = new Vector2(1f, 1f), smoothness = 0.02f });
        Add(new MatDef("Ceiling") { albedo = "ceiling_albedo", normal = "ceiling_normal", tiling = new Vector2(1f / 0.6f, 1f / 0.6f), smoothness = 0.1f });
        Add(new MatDef("LampPanel") { albedo = "lamp_albedo", tiling = new Vector2(1f / 1.2f, 1f / 0.6f), smoothness = 0.5f, emission = new Color(1.1f, 1.03f, 0.86f), emissionMap = "lamp_emission" });
        Add(new MatDef("Wood") { albedo = "wood_albedo", normal = "wood_normal", tiling = new Vector2(1f / 1.2f, 1f / 1.2f), smoothness = 0.35f });
        Add(new MatDef("DarkWood") { albedo = "wood_albedo", normal = "wood_normal", color = new Color(0.55f, 0.5f, 0.48f), tiling = new Vector2(1f / 0.9f, 1f / 0.9f), smoothness = 0.4f });
        Add(new MatDef("DoorPaint") { albedo = "door_albedo", normal = "door_normal", tiling = new Vector2(1f / 1.1f, 1f / 1.1f), smoothness = 0.3f });
        Add(new MatDef("Metal") { albedo = "metal_albedo", normal = "metal_normal", tiling = new Vector2(1f / 0.8f, 1f / 0.8f), smoothness = 0.55f, metallic = 0.6f });
        Add(new MatDef("Plastic") { albedo = "plastic_albedo", smoothness = 0.45f });
        Add(new MatDef("PlasticDark") { albedo = "plastic_dark_albedo", smoothness = 0.5f });
        Add(new MatDef("Baseboard") { albedo = "baseboard_albedo", smoothness = 0.35f });
        Add(new MatDef("Tiles") { albedo = "tiles_albedo", normal = "tiles_normal", tiling = new Vector2(1f / 1.2f, 1f / 1.2f), smoothness = 0.6f });
        Add(new MatDef("WhiteWall") { color = new Color(0.92f, 0.92f, 0.9f), smoothness = 0.1f, emission = new Color(0.25f, 0.25f, 0.25f) });
        Add(new MatDef("Leaf") { albedo = "leaf_albedo", tiling = new Vector2(2f, 2f), smoothness = 0.3f });
        Add(new MatDef("Pot") { albedo = "pot_albedo", tiling = new Vector2(2f, 2f), smoothness = 0.2f });
        Add(new MatDef("Water") { albedo = "water_albedo", smoothness = 0.85f });
        Add(new MatDef("Chair") { albedo = "chair_albedo", normal = "chair_normal", color = new Color(0.85f, 0.83f, 0.78f), smoothness = 0.45f });
        Add(new MatDef("Black") { color = new Color(0.01f, 0.01f, 0.01f), smoothness = 0f });
        Add(new MatDef("Figure") { color = new Color(0.015f, 0.012f, 0.012f), smoothness = 0.05f });
        Add(new MatDef("Glow") { color = Color.black, emission = new Color(2.2f, 2.0f, 1.8f) });
        // Personaje (vigilante nocturno). Relieve de tela: Poly Haven fabric_pattern_07 (CC0).
        Add(new MatDef("Uniform") { normal = "fabric_normal", color = new Color(0.13f, 0.17f, 0.30f), tiling = new Vector2(4f, 4f), smoothness = 0.12f });
        Add(new MatDef("Pants") { normal = "fabric_normal", color = new Color(0.09f, 0.09f, 0.11f), tiling = new Vector2(4f, 4f), smoothness = 0.1f });
        Add(new MatDef("Skin") { color = new Color(0.82f, 0.62f, 0.50f), smoothness = 0.3f });
        Add(new MatDef("Shirt") { normal = "fabric_normal", color = new Color(0.86f, 0.86f, 0.82f), tiling = new Vector2(4f, 4f), smoothness = 0.15f });
        Add(new MatDef("Tie") { color = new Color(0.55f, 0.06f, 0.06f), smoothness = 0.35f });
        Add(new MatDef("Duck") { color = new Color(1f, 0.82f, 0.08f), smoothness = 0.75f });
        Add(new MatDef("Beak") { color = new Color(1f, 0.45f, 0.05f), smoothness = 0.6f });
        Add(new MatDef("Hair") { color = new Color(0.12f, 0.08f, 0.05f), smoothness = 0.25f });
        Add(new MatDef("Shoe") { color = new Color(0.03f, 0.03f, 0.03f), smoothness = 0.7f });
        Add(new MatDef("Badge") { color = new Color(0.95f, 0.75f, 0.3f), smoothness = 0.8f, metallic = 0.9f });
        Add(new MatDef("Image") { smoothness = 0.15f });                          // cuadros, carteles, placas
        Add(new MatDef("ImageGlow") { smoothness = 0.6f, emission = Color.white, emissionMap = "" });  // pantallas y cartel de salida
        Add(new MatDef("ImageCutout") { smoothness = 0.1f, cutout = true });       // reloj y huellas (con transparencia)
    }

    private static void Add(MatDef d)
    {
        Definitions.Add(d);
        defs[d.key] = d;
    }

    public static MatDef Def(string key)
    {
        MatDef d;
        return defs.TryGetValue(key, out d) ? d : null;
    }

    /// Material compartido (no modificar en tiempo de ejecución).
    public static Material Get(string key)
    {
        Material m;
        if (shared.TryGetValue(key, out m) && m != null) return m;

        m = Resources.Load<Material>("Materials/" + key);
        if (m == null)
        {
            MatDef d = Def(key);
            if (d == null) { Debug.LogWarning("[NoMiresDosVeces] Material desconocido: " + key); d = new MatDef(key); }
            m = new Material(Template());
            m.name = key + " (runtime)";
            Configure(m, d);
            MarkTemporary(m);
        }
        shared[key] = m;
        return m;
    }

    /// Copia propia del material (para cambiar textura, color o brillo de un solo objeto).
    public static Material Instance(string key)
    {
        Material m = new Material(Get(key));
        m.name = key + " (instancia)";
        MarkTemporary(m);
        instances.Add(m);
        return m;
    }

    /// Destruye las copias creadas (al reiniciar la partida).
    public static void ReleaseInstances()
    {
        for (int i = 0; i < instances.Count; i++)
        {
            if (instances[i] == null) continue;
            if (Application.isPlaying) Object.Destroy(instances[i]);
            else Object.DestroyImmediate(instances[i]);
        }
        instances.Clear();
    }

    private static void MarkTemporary(Material m)
    {
        if (!Application.isPlaying) m.hideFlags = HideFlags.DontSave;
    }

    /// Material base: el que Unity pone a un cubo nuevo. Así el shader siempre coincide con el pipeline
    /// del proyecto (URP o Built-in) y nunca sale rosa.
    public static Material Template()
    {
        if (template != null) return template;
        GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        template = tmp.GetComponent<Renderer>().sharedMaterial;
        if (Application.isPlaying) Object.Destroy(tmp); else Object.DestroyImmediate(tmp);
        return template;
    }

    /// Aplica una definición a un material (lo usa también la herramienta del Editor).
    public static void Configure(Material m, MatDef d)
    {
        SetColor(m, d.color);

        Texture2D alb = string.IsNullOrEmpty(d.albedo) ? null : GameAssets.Texture(d.albedo);
        SetMainTexture(m, alb);
        SetTiling(m, d.tiling);

        Texture2D nrm = string.IsNullOrEmpty(d.normal) ? null : GameAssets.Texture(d.normal);
        if (nrm != null && m.HasProperty("_BumpMap"))
        {
            m.SetTexture("_BumpMap", nrm);
            m.SetTextureScale("_BumpMap", d.tiling);
            if (m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", 1f);
            m.EnableKeyword("_NORMALMAP");
        }

        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", d.smoothness);     // URP
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", d.smoothness);     // Built-in
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", d.metallic);

        if (d.emission.maxColorComponent > 0f)
        {
            Texture2D em = string.IsNullOrEmpty(d.emissionMap) ? null : GameAssets.Texture(d.emissionMap);
            SetEmission(m, d.emission, em);
        }

        if (d.cutout)
        {
            if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1f);   // URP
            if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 1f);             // Built-in: Cutout
            if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", 0.5f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetOverrideTag("RenderType", "TransparentCutout");
            m.renderQueue = 2450;
        }
    }

    public static void SetColor(Material m, Color c)
    {
        if (m == null) return;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);   // URP
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);           // Built-in
    }

    public static void SetMainTexture(Material m, Texture t)
    {
        if (m == null) return;
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", t);     // URP
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", t);     // Built-in
    }

    public static void SetTiling(Material m, Vector2 tiling)
    {
        if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", tiling);
        if (m.HasProperty("_MainTex")) m.SetTextureScale("_MainTex", tiling);
    }

    /// Enciende/cambia la emisión. Con color negro la apaga visualmente sin tocar las variantes del shader.
    public static void SetEmission(Material m, Color c, Texture map)
    {
        if (m == null || !m.HasProperty("_EmissionColor")) return;
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        m.SetColor("_EmissionColor", c);
        if (map != null && m.HasProperty("_EmissionMap")) m.SetTexture("_EmissionMap", map);
    }
}
