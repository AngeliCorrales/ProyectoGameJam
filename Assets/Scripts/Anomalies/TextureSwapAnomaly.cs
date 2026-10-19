using UnityEngine;

/// La imagen de un objeto cambia: un cuadro, un póster, la pantalla de un monitor, el cartel de salida, una placa.
public class TextureSwapAnomaly : Anomaly
{
    public Renderer target;
    public Texture normalTexture;
    public Texture anomalyTexture;
    public bool glow;                         // la imagen también emite luz (pantallas y carteles)
    public Color glowNormal = new Color(0.6f, 0.6f, 0.6f);
    public Color glowAnomaly = new Color(1.4f, 1.4f, 1.4f);

    protected override void OnActivate() { Apply(anomalyTexture, glowAnomaly); }
    protected override void OnReported() { Apply(normalTexture, glowNormal); }

    private void Apply(Texture tex, Color glowColor)
    {
        if (target == null || tex == null) return;
        Material m = target.sharedMaterial;          // LevelBuilder ya le dio una instancia propia
        MaterialLibrary.SetMainTexture(m, tex);
        if (glow) MaterialLibrary.SetEmission(m, glowColor, tex);
    }
}
