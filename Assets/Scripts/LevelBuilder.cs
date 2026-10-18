using System.Collections.Generic;
using UnityEngine;

public class LevelRefs
{
    public readonly List<Anomaly> Anomalies = new List<Anomaly>();
    public ExitDoor Door;
}

/// Construye el pasillo de oficinas solo con cubos. Interior: x en [-2, 2], z en [0, 20], altura 3.
/// El jugador empieza en z = 1.5 mirando hacia +Z; la puerta de salida está en z = 20.
public static class LevelBuilder
{
    public static LevelRefs Build(Transform root, Camera cam)
    {
        LevelRefs refs = new LevelRefs();

        Color wallC = new Color(0.74f, 0.72f, 0.60f);
        Color floorC = new Color(0.16f, 0.17f, 0.16f);
        Color carpetC = new Color(0.20f, 0.24f, 0.28f);
        Color ceilC = new Color(0.80f, 0.80f, 0.78f);
        Color brightC = new Color(0.95f, 0.95f, 0.95f);
        Color woodC = new Color(0.33f, 0.22f, 0.14f);
        Color woodTopC = new Color(0.45f, 0.31f, 0.20f);

        // ---------- Estructura del pasillo ----------
        Box("Floor", root, new Vector3(0f, -0.1f, 10f), new Vector3(4.4f, 0.2f, 20.4f), floorC);
        Box("Carpet", root, new Vector3(0f, 0.01f, 10f), new Vector3(2f, 0.02f, 19.6f), carpetC, false);
        Box("Ceiling", root, new Vector3(0f, 3.1f, 10f), new Vector3(4.4f, 0.2f, 20.4f), ceilC);
        Box("WallLeft", root, new Vector3(-2.1f, 1.5f, 10f), new Vector3(0.2f, 3f, 20.4f), wallC);
        Box("WallRight", root, new Vector3(2.1f, 1.5f, 10f), new Vector3(0.2f, 3f, 20.4f), wallC);
        Box("WallBack", root, new Vector3(0f, 1.5f, -0.1f), new Vector3(4.4f, 3f, 0.2f), wallC);

        // Pared del fondo con hueco para la puerta (ancho 1.4, alto 2.2)
        Box("WallFrontL", root, new Vector3(-1.45f, 1.5f, 20.1f), new Vector3(1.5f, 3f, 0.2f), wallC);
        Box("WallFrontR", root, new Vector3(1.45f, 1.5f, 20.1f), new Vector3(1.5f, 3f, 0.2f), wallC);
        Box("WallFrontTop", root, new Vector3(0f, 2.6f, 20.1f), new Vector3(1.4f, 0.8f, 0.2f), wallC);
        Box("ExitSign", root, new Vector3(0f, 2.45f, 19.97f), new Vector3(0.5f, 0.15f, 0.04f),
            new Color(0.1f, 0.5f, 0.2f), false);

        // ---------- Sala de salida (blanca, tras la puerta) ----------
        Box("ExitFloor", root, new Vector3(0f, -0.1f, 23.1f), new Vector3(4.4f, 0.2f, 6.2f), brightC);
        Box("ExitCeiling", root, new Vector3(0f, 3.1f, 23.1f), new Vector3(4.4f, 0.2f, 6.2f), brightC);
        Box("ExitWallL", root, new Vector3(-2.1f, 1.5f, 23.1f), new Vector3(0.2f, 3f, 6.2f), brightC);
        Box("ExitWallR", root, new Vector3(2.1f, 1.5f, 23.1f), new Vector3(0.2f, 3f, 6.2f), brightC);
        Box("ExitWallBack", root, new Vector3(0f, 1.5f, 26.1f), new Vector3(4.4f, 3f, 0.2f), brightC);
        GameObject exitLight = new GameObject("ExitLight");
        exitLight.transform.SetParent(root, false);
        exitLight.transform.localPosition = new Vector3(0f, 2.4f, 22.5f);
        Light el = exitLight.AddComponent<Light>();
        el.type = LightType.Point;
        el.color = Color.white;
        el.intensity = 2.5f;
        el.range = 10f;
        el.shadows = LightShadows.None;

        // ---------- Puerta de salida ----------
        GameObject doorPivot = new GameObject("ExitDoor");
        doorPivot.transform.SetParent(root, false);
        doorPivot.transform.localPosition = new Vector3(-0.7f, 0f, 20.1f);
        ExitDoor door = doorPivot.AddComponent<ExitDoor>();
        GameObject leaf = Box("Leaf", doorPivot.transform, new Vector3(0.7f, 1.1f, 0f),
                              new Vector3(1.4f, 2.2f, 0.1f), new Color(0.25f, 0.17f, 0.12f));
        Box("Handle", doorPivot.transform, new Vector3(1.2f, 1.05f, -0.08f),
            new Vector3(0.1f, 0.06f, 0.08f), new Color(0.7f, 0.65f, 0.3f), false);
        door.leafCollider = leaf.GetComponent<Collider>();
        refs.Door = door;

        // ---------- Detalles de oficina (puertas planas decorativas) ----------
        Box("OfficeDoorL", root, new Vector3(-1.97f, 1.05f, 3.5f), new Vector3(0.06f, 2.1f, 0.9f),
            new Color(0.2f, 0.16f, 0.12f), false);
        Box("OfficeDoorR", root, new Vector3(1.97f, 1.05f, 3.0f), new Vector3(0.06f, 2.1f, 0.9f),
            new Color(0.2f, 0.16f, 0.12f), false);

        // ---------- Escritorios y sillas ----------
        Desk(root, -1f, 12f, woodC, woodTopC);
        Desk(root, +1f, 7f, woodC, woodTopC);
        Desk(root, +1f, 16.5f, woodC, woodTopC);

        // ANOMALÍA 3: silla (izquierda, z = 12)
        GameObject chair = Chair(root, "Chair_Anomaly", new Vector3(-0.55f, 0f, 12f), -90f);
        ChairAnomaly chairAnomaly = chair.AddComponent<ChairAnomaly>();
        chairAnomaly.targetCollider = chair.GetComponent<Collider>();
        chairAnomaly.Configure("Silla desaparecida", cam);
        refs.Anomalies.Add(chairAnomaly);

        // Sillas normales (señuelos)
        AddDecoy(Chair(root, "Chair_Decoy_1", new Vector3(0.55f, 0f, 7f), 90f));
        AddDecoy(Chair(root, "Chair_Decoy_2", new Vector3(0.55f, 0f, 16.5f), 90f));

        // ---------- Cuadros ----------
        // ANOMALÍA 1: cuadro (pared izquierda, z = 6)
        GameObject painting = Painting(root, "Painting_Anomaly", new Vector3(-1.97f, 1.7f, 6f), -90f,
                                       new Color(0.25f, 0.40f, 0.45f), new Color(0.7f, 0.55f, 0.25f));
        PaintingAnomaly paintingAnomaly = painting.AddComponent<PaintingAnomaly>();
        paintingAnomaly.targetCollider = painting.transform.Find("Frame").GetComponent<Collider>();
        paintingAnomaly.Configure("Cuadro movido", cam);
        refs.Anomalies.Add(paintingAnomaly);

        // Cuadros normales (señuelos)
        AddDecoy(Painting(root, "Painting_Decoy_1", new Vector3(1.97f, 1.7f, 11f), 90f,
                          new Color(0.45f, 0.30f, 0.30f), new Color(0.85f, 0.8f, 0.7f)));
        AddDecoy(Painting(root, "Painting_Decoy_2", new Vector3(-1.97f, 1.7f, 15f), -90f,
                          new Color(0.30f, 0.38f, 0.28f), new Color(0.2f, 0.2f, 0.25f)));

        // ---------- Luces ----------
        // ANOMALÍA 2: lámpara (z = 9)
        Light anomalyLight;
        Renderer anomalyPanel;
        Collider anomalyCollider;
        GameObject lamp = Lamp(root, "Lamp_Anomaly", 9f, out anomalyLight, out anomalyPanel, out anomalyCollider);
        LightAnomaly lightAnomaly = lamp.AddComponent<LightAnomaly>();
        lightAnomaly.lightSource = anomalyLight;
        lightAnomaly.panel = anomalyPanel;
        AudioSource lampAudio = lamp.AddComponent<AudioSource>();
        lampAudio.playOnAwake = false;
        lampAudio.spatialBlend = 1f;
        lampAudio.minDistance = 2f;
        lampAudio.maxDistance = 20f;
        lampAudio.volume = 0.8f;
        lightAnomaly.audioSource = lampAudio;
        lightAnomaly.Configure("Luz apagada", cam);
        refs.Anomalies.Add(lightAnomaly);

        // Lámparas normales (señuelos)
        foreach (float z in new[] { 3f, 14f, 18f })
        {
            Light l; Renderer r; Collider c;
            AddDecoy(Lamp(root, "Lamp_Decoy_" + z, z, out l, out r, out c));
        }

        return refs;
    }

    // ===================== Helpers =====================

    private static void AddDecoy(GameObject go)
    {
        go.AddComponent<DecoyReportable>();
    }

    private static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 scale, Color color,
                                  bool withCollider = true)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = MaterialUtil.Make(color);
        if (!withCollider) Object.Destroy(go.GetComponent<Collider>());
        return go;
    }

    // side = -1 (pared izquierda) o +1 (pared derecha)
    private static void Desk(Transform root, float side, float z, Color wood, Color woodTop)
    {
        GameObject d = new GameObject("DeskSet");
        d.transform.SetParent(root, false);
        float x = side * 1.45f;
        Box("DeskBody", d.transform, new Vector3(x, 0.35f, z), new Vector3(1.0f, 0.7f, 1.8f), wood);
        Box("DeskTop", d.transform, new Vector3(x, 0.73f, z), new Vector3(1.05f, 0.06f, 1.85f), woodTop);
        Box("MonitorStand", d.transform, new Vector3(side * 1.65f, 0.86f, z), new Vector3(0.08f, 0.2f, 0.08f),
            new Color(0.1f, 0.1f, 0.1f), false);
        Box("MonitorScreen", d.transform, new Vector3(side * 1.65f, 1.15f, z), new Vector3(0.04f, 0.36f, 0.6f),
            new Color(0.05f, 0.09f, 0.10f), false);
    }

    private static GameObject Chair(Transform root, string name, Vector3 pos, float yaw)
    {
        GameObject c = new GameObject(name);
        c.transform.SetParent(root, false);
        c.transform.localPosition = pos;
        c.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        Color seat = new Color(0.20f, 0.22f, 0.30f);
        Color leg = new Color(0.07f, 0.07f, 0.07f);
        Box("Seat", c.transform, new Vector3(0f, 0.48f, 0f), new Vector3(0.5f, 0.06f, 0.5f), seat, false);
        Box("Back", c.transform, new Vector3(0f, 0.80f, -0.22f), new Vector3(0.5f, 0.55f, 0.06f), seat, false);
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Box("Leg", c.transform, new Vector3(sx * 0.2f, 0.23f, sz * 0.2f), new Vector3(0.05f, 0.46f, 0.05f), leg, false);

        // Zona señalable (trigger): sigue existiendo aunque la silla desaparezca.
        BoxCollider zone = c.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.center = new Vector3(0f, 0.55f, 0f);
        zone.size = new Vector3(0.65f, 1.15f, 0.65f);
        return c;
    }

    // El lado visible del cuadro es su eje local -Z. yaw = -90 en la pared izquierda, +90 en la derecha.
    private static GameObject Painting(Transform root, string name, Vector3 pos, float yaw, Color canvas, Color accent)
    {
        GameObject p = new GameObject(name);
        p.transform.SetParent(root, false);
        p.transform.localPosition = pos;
        p.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        Box("Frame", p.transform, Vector3.zero, new Vector3(1.3f, 0.9f, 0.06f), new Color(0.22f, 0.14f, 0.08f));
        Box("Canvas", p.transform, new Vector3(0f, 0f, -0.035f), new Vector3(1.15f, 0.75f, 0.02f), canvas, false);
        Box("Subject", p.transform, new Vector3(0.15f, -0.05f, -0.05f), new Vector3(0.35f, 0.4f, 0.02f), accent, false);
        return p;
    }

    private static GameObject Lamp(Transform root, string name, float z, out Light light, out Renderer panel,
                                   out Collider panelCollider)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(0f, 2.95f, z);

        GameObject p = Box("Panel", go.transform, Vector3.zero, new Vector3(1.0f, 0.1f, 0.5f),
                           new Color(1f, 0.97f, 0.85f));
        panel = p.GetComponent<Renderer>();
        panelCollider = p.GetComponent<Collider>();

        GameObject lg = new GameObject("Light");
        lg.transform.SetParent(go.transform, false);
        lg.transform.localPosition = new Vector3(0f, -0.4f, 0f);
        light = lg.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.92f, 0.75f);
        light.intensity = 1.6f;
        light.range = 9f;
        light.shadows = LightShadows.None;
        return go;
    }
}
