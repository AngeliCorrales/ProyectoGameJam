using UnityEngine;

/// Crea materiales de colores sin depender de ningún asset. Copia el material por defecto
/// de un cubo, así funciona tanto en Built-in como en URP (evita el "rosa" por shader incorrecto).
public static class MaterialUtil
{
    private static Material template;

    public static Material Make(Color color)
    {
        if (template == null)
        {
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            template = tmp.GetComponent<Renderer>().sharedMaterial;
            Object.Destroy(tmp);
        }
        Material m = new Material(template);
        Tint(m, color);
        return m;
    }

    public static void Tint(Material m, Color color)
    {
        if (m == null) return;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);   // URP
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);           // Built-in
    }
}
