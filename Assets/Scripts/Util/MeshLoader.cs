using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// Carga mallas guardadas en Resources/Models/*.bytes (formato "NMDV": vértices, normales, UV, índices).
/// Se usa para la silla de Poly Haven: así no depende de los ajustes de importación de modelos.
public static class MeshLoader
{
    private static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

    public static Mesh Load(string name)
    {
        Mesh cached;
        if (cache.TryGetValue(name, out cached) && cached != null) return cached;

        TextAsset data = GameAssets.Model(name);
        if (data == null) return null;

        Mesh mesh = null;
        try
        {
            using (BinaryReader r = new BinaryReader(new MemoryStream(data.bytes)))
            {
                string magic = new string(r.ReadChars(4));
                if (magic != "NMDV") throw new IOException("Formato desconocido: " + magic);
                int vc = r.ReadInt32();
                int ic = r.ReadInt32();
                Vector3[] pos = new Vector3[vc];
                Vector3[] nrm = new Vector3[vc];
                Vector2[] uv = new Vector2[vc];
                int[] idx = new int[ic];
                for (int i = 0; i < vc; i++) pos[i] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                for (int i = 0; i < vc; i++) nrm[i] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                for (int i = 0; i < vc; i++) uv[i] = new Vector2(r.ReadSingle(), r.ReadSingle());
                for (int i = 0; i < ic; i++) idx[i] = r.ReadInt32();

                mesh = new Mesh();
                mesh.name = name;
                if (vc > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.vertices = pos;
                mesh.normals = nrm;
                mesh.uv = uv;
                mesh.triangles = idx;
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                if (!Application.isPlaying) mesh.hideFlags = HideFlags.DontSave;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[NoMiresDosVeces] No se pudo leer el modelo " + name + ": " + e.Message);
            mesh = null;
        }

        cache[name] = mesh;
        return mesh;
    }
}
