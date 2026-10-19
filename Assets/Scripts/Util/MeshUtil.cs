using System.Collections.Generic;
using UnityEngine;

/// Mallas generadas por código: cajas con UV en metros (las texturas no se estiran),
/// quads, cilindros y esferas. Se cachean por tamaño para no repetirlas.
public static class MeshUtil
{
    private static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

    private class Builder
    {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<Vector3> n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<int> t = new List<int>();

        /// Añade un triángulo cuya cara visible mira hacia "facing" (ajusta el orden automáticamente).
        public void Tri(int a, int b, int c, Vector3 facing)
        {
            Vector3 cr = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            if (Vector3.Dot(cr, facing) >= 0f) { t.Add(a); t.Add(b); t.Add(c); }
            else { t.Add(a); t.Add(c); t.Add(b); }
        }

        public int Vert(Vector3 p, Vector3 normal, Vector2 tex)
        {
            v.Add(p); n.Add(normal); uv.Add(tex);
            return v.Count - 1;
        }

        public Mesh Build(string name)
        {
            Mesh m = new Mesh();
            m.name = name;
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            m.RecalculateTangents();
            if (!Application.isPlaying) m.hideFlags = HideFlags.DontSave;
            return m;
        }
    }

    private static Mesh Cached(string key, System.Func<Mesh> make)
    {
        Mesh m;
        if (cache.TryGetValue(key, out m) && m != null) return m;
        m = make();
        cache[key] = m;
        return m;
    }

    /// Caja centrada en el origen. UV = metros, así una textura de 1 m se repite bien en cualquier tamaño.
    public static Mesh Box(Vector3 size)
    {
        string key = "box" + size.x.ToString("F3") + "_" + size.y.ToString("F3") + "_" + size.z.ToString("F3");
        return Cached(key, () =>
        {
            Builder b = new Builder();
            Vector3 h = size * 0.5f;
            Face(b, new Vector3(h.x, 0, 0), Vector3.right, Vector3.up, size.z, size.y, h);
            Face(b, new Vector3(-h.x, 0, 0), Vector3.left, Vector3.up, size.z, size.y, h);
            Face(b, new Vector3(0, 0, h.z), Vector3.forward, Vector3.up, size.x, size.y, h);
            Face(b, new Vector3(0, 0, -h.z), Vector3.back, Vector3.up, size.x, size.y, h);
            Face(b, new Vector3(0, h.y, 0), Vector3.up, Vector3.forward, size.x, size.z, h);
            Face(b, new Vector3(0, -h.y, 0), Vector3.down, Vector3.forward, size.x, size.z, h);
            return b.Build("Box");
        });
    }

    private static void Face(Builder b, Vector3 center, Vector3 normal, Vector3 up, float uLen, float vLen, Vector3 half)
    {
        Vector3 right = Vector3.Cross(normal, up).normalized;       // derecha vista desde fuera
        Vector3 ru = Scale(right, half);
        Vector3 vu = Scale(up, half);
        int i0 = b.Vert(center - ru - vu, normal, new Vector2(0, 0));
        int i1 = b.Vert(center + ru - vu, normal, new Vector2(uLen, 0));
        int i2 = b.Vert(center + ru + vu, normal, new Vector2(uLen, vLen));
        int i3 = b.Vert(center - ru + vu, normal, new Vector2(0, vLen));
        b.Tri(i0, i3, i2, normal);
        b.Tri(i0, i2, i1, normal);
    }

    private static Vector3 Scale(Vector3 axis, Vector3 half)
    {
        return new Vector3(axis.x * half.x, axis.y * half.y, axis.z * half.z);
    }

    /// Plano w x h con UV 0..1. Su cara visible mira hacia -Z (como el Quad de Unity).
    public static Mesh Quad(float w, float h)
    {
        string key = "quad" + w.ToString("F3") + "_" + h.ToString("F3");
        return Cached(key, () =>
        {
            Builder b = new Builder();
            Vector3 n = Vector3.back;
            int i0 = b.Vert(new Vector3(-w / 2, -h / 2, 0), n, new Vector2(0, 0));
            int i1 = b.Vert(new Vector3(w / 2, -h / 2, 0), n, new Vector2(1, 0));
            int i2 = b.Vert(new Vector3(w / 2, h / 2, 0), n, new Vector2(1, 1));
            int i3 = b.Vert(new Vector3(-w / 2, h / 2, 0), n, new Vector2(0, 1));
            b.Tri(i0, i3, i2, n);
            b.Tri(i0, i2, i1, n);
            return b.Build("Quad");
        });
    }

    /// Cilindro vertical (eje Y) de base en y = 0. UV en metros.
    public static Mesh Cylinder(float radius, float height, int segments = 20)
    {
        string key = "cyl" + radius.ToString("F3") + "_" + height.ToString("F3") + "_" + segments;
        return Cached(key, () =>
        {
            Builder b = new Builder();
            float circ = 2f * Mathf.PI * radius;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                float u0 = circ * i / segments, u1 = circ * (i + 1) / segments;
                int s0 = b.Vert(d0 * radius, d0, new Vector2(u0, 0));
                int s1 = b.Vert(d1 * radius, d1, new Vector2(u1, 0));
                int s2 = b.Vert(d1 * radius + Vector3.up * height, d1, new Vector2(u1, height));
                int s3 = b.Vert(d0 * radius + Vector3.up * height, d0, new Vector2(u0, height));
                Vector3 mid = (d0 + d1).normalized;
                b.Tri(s0, s1, s2, mid);
                b.Tri(s0, s2, s3, mid);

                int tc = b.Vert(Vector3.up * height, Vector3.up, new Vector2(0, 0));
                int t0 = b.Vert(d0 * radius + Vector3.up * height, Vector3.up, new Vector2(d0.x * radius, d0.z * radius));
                int t1 = b.Vert(d1 * radius + Vector3.up * height, Vector3.up, new Vector2(d1.x * radius, d1.z * radius));
                b.Tri(tc, t0, t1, Vector3.up);

                int bc = b.Vert(Vector3.zero, Vector3.down, new Vector2(0, 0));
                int b0 = b.Vert(d0 * radius, Vector3.down, new Vector2(d0.x * radius, d0.z * radius));
                int b1 = b.Vert(d1 * radius, Vector3.down, new Vector2(d1.x * radius, d1.z * radius));
                b.Tri(bc, b0, b1, Vector3.down);
            }
            return b.Build("Cylinder");
        });
    }

    /// Esfera centrada en el origen.
    public static Mesh Sphere(float radius, int rings = 10, int segments = 16)
    {
        string key = "sph" + radius.ToString("F3") + "_" + rings + "_" + segments;
        return Cached(key, () =>
        {
            Builder b = new Builder();
            for (int r = 0; r < rings; r++)
            {
                float p0 = Mathf.PI * r / rings, p1 = Mathf.PI * (r + 1) / rings;
                for (int s = 0; s < segments; s++)
                {
                    float a0 = 2 * Mathf.PI * s / segments, a1 = 2 * Mathf.PI * (s + 1) / segments;
                    Vector3 n00 = Dir(p0, a0), n01 = Dir(p0, a1), n10 = Dir(p1, a0), n11 = Dir(p1, a1);
                    int i00 = b.Vert(n00 * radius, n00, new Vector2((float)s / segments, 1 - (float)r / rings));
                    int i01 = b.Vert(n01 * radius, n01, new Vector2((float)(s + 1) / segments, 1 - (float)r / rings));
                    int i10 = b.Vert(n10 * radius, n10, new Vector2((float)s / segments, 1 - (float)(r + 1) / rings));
                    int i11 = b.Vert(n11 * radius, n11, new Vector2((float)(s + 1) / segments, 1 - (float)(r + 1) / rings));
                    Vector3 c = (n00 + n01 + n10 + n11).normalized;
                    if (r > 0) b.Tri(i00, i01, i11, c);
                    if (r < rings - 1) b.Tri(i00, i11, i10, c);
                }
            }
            return b.Build("Sphere");
        });
    }

    private static Vector3 Dir(float polar, float azimuth)
    {
        return new Vector3(Mathf.Sin(polar) * Mathf.Cos(azimuth), Mathf.Cos(polar), Mathf.Sin(polar) * Mathf.Sin(azimuth));
    }
}
