using System.Collections.Generic;
using UnityEngine;

/// Referencias que el GameManager necesita del nivel construido.
public class LevelRefs
{
    public readonly List<Anomaly> Pool = new List<Anomaly>();          // las 23 anomalías posibles
    public readonly List<Light> Lamps = new List<Light>();
    public readonly List<float> LampBaseIntensity = new List<float>();
    public ExitDoor ExitDoor;
    public ExitDoor StartDoor;                       // la puerta por la que entraste (normalmente cerrada)
    public Renderer ExitSignRenderer;
    public TextureSwapAnomaly RoomSign;              // cartel "CUARTO N"
    public Vector3 PlayerStart = new Vector3(0f, 0.05f, 1.0f);
}

/// Construye el pasillo de oficinas: estructura, muebles, decoración, luces y las 23 anomalías posibles.
/// Interior del pasillo: x en [-2, 2], z en [0, 30], altura 3 m. El jugador empieza en z = 1.2 mirando a +Z.
/// La puerta de SALIDA está al fondo (z = 30) y detrás hay una sala blanca.
///
/// Convención de los objetos de pared: se crea un grupo pegado a la pared con su eje local -Z
/// apuntando hacia el pasillo (yaw -90 en la pared izquierda, +90 en la derecha).
public static class LevelBuilder
{
    private const float LeftYaw = -90f, RightYaw = 90f;
    private static Camera cam;
    private static LevelRefs refs;

    /// roomNumber: número que muestra el cartel del cuarto. finalRoom: tras la puerta está la sala blanca de salida.
    public static LevelRefs Build(Transform root, Camera playerCamera, int roomNumber = 0, bool finalRoom = false)
    {
        cam = playerCamera;
        refs = new LevelRefs();

        BuildStructure(root);
        BuildExitArea(root, finalRoom);
        BuildRoomSign(root, roomNumber, finalRoom);
        BuildLamps(root);
        BuildLeftWall(root);
        BuildRightWall(root);
        BuildCorridorExtras(root);

        LevelRefs result = refs;
        refs = null;
        return result;
    }

    // =====================================================================
    // Estructura
    // =====================================================================

    private static void BuildStructure(Transform root)
    {
        Transform s = Group("Structure", root, Vector3.zero, 0f).transform;
        // Tramos de 3 m: así cada luz solo afecta a los tramos cercanos (mejor iluminación en URP)
        // y las texturas (motivos de 1 / 1,5 / 0,6 m) encajan sin cortes entre tramos.
        for (int i = 0; i < 10; i++)
        {
            float zc = i * 3f + 1.5f;
            Box("Floor", s, new Vector3(0f, -0.05f, zc), new Vector3(4f, 0.1f, 3f), "Carpet");
            Box("Ceiling", s, new Vector3(0f, 3.05f, zc), new Vector3(4f, 0.1f, 3f), "Ceiling");
            Box("WallL", s, new Vector3(-2.1f, 1.5f, zc), new Vector3(0.2f, 3.2f, 3f), "Wall");
            Box("WallR", s, new Vector3(2.1f, 1.5f, zc), new Vector3(0.2f, 3.2f, 3f), "Wall");
            Box("BaseboardL", s, new Vector3(-1.99f, 0.06f, zc), new Vector3(0.02f, 0.12f, 3f), "Baseboard", false);
            Box("BaseboardR", s, new Vector3(1.99f, 0.06f, zc), new Vector3(0.02f, 0.12f, 3f), "Baseboard", false);
        }
        // Pared de entrada con hueco de puerta y, detrás, un recibidor oscuro
        Box("WallBackL", s, new Vector3(-1.4f, 1.5f, -0.1f), new Vector3(1.6f, 3.2f, 0.2f), "Wall");
        Box("WallBackR", s, new Vector3(1.4f, 1.5f, -0.1f), new Vector3(1.6f, 3.2f, 0.2f), "Wall");
        Box("WallBackTop", s, new Vector3(0f, 2.65f, -0.1f), new Vector3(1.2f, 0.9f, 0.2f), "Wall");
        Box("StartThreshold", s, new Vector3(0f, -0.05f, -0.1f), new Vector3(1.2f, 0.1f, 0.22f), "DarkWood");
        Box("StartTrimL", s, new Vector3(-0.64f, 1.14f, 0.03f), new Vector3(0.08f, 2.28f, 0.06f), "DarkWood", false);
        Box("StartTrimR", s, new Vector3(0.64f, 1.14f, 0.03f), new Vector3(0.08f, 2.28f, 0.06f), "DarkWood", false);
        Box("StartTrimTop", s, new Vector3(0f, 2.24f, 0.03f), new Vector3(1.36f, 0.08f, 0.06f), "DarkWood", false);
        Vestibule(s, -1.7f);

        // Puerta de entrada: bisagra a la izquierda, se abre hacia el recibidor (cerrada salvo giro de guion)
        GameObject sp = Group("StartDoor", s, new Vector3(-0.6f, 0f, -0.1f), 0f);
        GameObject sleaf = Box("Leaf", sp.transform, new Vector3(0.6f, 1.1f, 0f), new Vector3(1.18f, 2.18f, 0.06f), "DoorPaint");
        Box("Handle", sp.transform, new Vector3(1.02f, 1.02f, 0.05f), new Vector3(0.12f, 0.03f, 0.04f), "Metal", false);
        Box("Window", sp.transform, new Vector3(0.6f, 1.65f, 0.031f), new Vector3(0.3f, 0.4f, 0.002f), "Black", false);
        ExitDoor startDoor = sp.AddComponent<ExitDoor>();
        startDoor.leafCollider = sleaf.GetComponent<Collider>();
        startDoor.audioSource = Audio3D(sp, 2f, 20f);
        startDoor.openAngle = 100f;
        startDoor.lockedMessage = "Está cerrada. Solo se puede seguir adelante.";
        startDoor.prompt = "[E] Volver atrás";
        refs.StartDoor = startDoor;

        // Pared del fondo con el hueco de la puerta de salida (1,2 x 2,2 m)
        Box("WallFrontL", s, new Vector3(-1.4f, 1.5f, 30.1f), new Vector3(1.6f, 3.2f, 0.2f), "Wall");
        Box("WallFrontR", s, new Vector3(1.4f, 1.5f, 30.1f), new Vector3(1.6f, 3.2f, 0.2f), "Wall");
        Box("WallFrontTop", s, new Vector3(0f, 2.65f, 30.1f), new Vector3(1.2f, 0.9f, 0.2f), "Wall");
        Box("Threshold", s, new Vector3(0f, -0.05f, 30.1f), new Vector3(1.2f, 0.1f, 0.22f), "DarkWood");
        Box("ExitTrimL", s,new Vector3(-0.64f, 1.14f, 29.97f), new Vector3(0.08f, 2.28f, 0.06f), "DarkWood", false);
        Box("ExitTrimR", s, new Vector3(0.64f, 1.14f, 29.97f), new Vector3(0.08f, 2.28f, 0.06f), "DarkWood", false);
        Box("ExitTrimTop", s, new Vector3(0f, 2.24f, 29.97f), new Vector3(1.36f, 0.08f, 0.06f), "DarkWood", false);
    }

    /// Recibidor oscuro de 3 m (detrás de la puerta de entrada y, en los cuartos normales, de la de salida).
    private static void Vestibule(Transform parent, float zc)
    {
        Box("VestFloor", parent, new Vector3(0f, -0.05f, zc), new Vector3(2.4f, 0.1f, 3f), "Carpet");
        Box("VestCeiling", parent, new Vector3(0f, 3.05f, zc), new Vector3(2.4f, 0.1f, 3f), "Ceiling");
        Box("VestWallL", parent, new Vector3(-1.3f, 1.5f, zc), new Vector3(0.2f, 3.2f, 3f), "Wall");
        Box("VestWallR", parent, new Vector3(1.3f, 1.5f, zc), new Vector3(0.2f, 3.2f, 3f), "Wall");
        float endZ = zc + (zc < 15f ? -1.6f : 1.6f);
        Box("VestEnd", parent, new Vector3(0f, 1.5f, endZ), new Vector3(2.8f, 3.2f, 0.2f), "Black");
    }

    private static void BuildRoomSign(Transform root, int number, bool finalRoom)
    {
        GameObject g = Group("RoomSign", root, new Vector3(-2f, 1.75f, 2.0f), LeftYaw);
        Box("Back", g.transform, new Vector3(0f, 0f, -0.015f), new Vector3(0.98f, 0.53f, 0.03f), "PlasticDark", false);
        Texture2D tex = GameAssets.Sprite(finalRoom ? "room_exit" : "room_" + Mathf.Clamp(number, 0, 9));
        Renderer r = Image("Face", g.transform, new Vector3(0f, 0f, -0.031f), 0.92f, 0.46f, tex, "ImageGlow", new Color(0.35f, 0.35f, 0.35f));
        TextureSwapAnomaly a = MakeAnomaly<TextureSwapAnomaly>(g, "Número del cuarto", new Vector3(0f, 0f, -0.04f), new Vector3(1.05f, 0.6f, 0.12f));
        a.target = r; a.normalTexture = tex; a.glow = true;
        a.glowNormal = new Color(0.35f, 0.35f, 0.35f); a.glowAnomaly = new Color(0.6f, 0.35f, 0.35f);
        a.anomalyTexture = GameAssets.Sprite("room_" + ((number + 4) % 10));
        refs.RoomSign = a;
    }

    private static void BuildExitArea(Transform root, bool finalRoom)
    {
        Transform e = Group("ExitArea", root, Vector3.zero, 0f).transform;

        // Puerta de salida (bisagra a la izquierda, se abre hacia la sala blanca)
        GameObject pivot = Group("ExitDoor", e, new Vector3(-0.6f, 0f, 30.1f), 0f);
        GameObject leaf = Box("Leaf", pivot.transform, new Vector3(0.6f, 1.1f, 0f), new Vector3(1.18f, 2.18f, 0.06f), "DoorPaint");
        Box("PushBar", pivot.transform, new Vector3(0.62f, 1.02f, -0.06f), new Vector3(0.8f, 0.05f, 0.05f), "Metal", false);
        Box("Handle", pivot.transform, new Vector3(1.02f, 1.02f, 0.05f), new Vector3(0.12f, 0.03f, 0.04f), "Metal", false);
        Box("Window", pivot.transform, new Vector3(0.6f, 1.65f, -0.031f), new Vector3(0.3f, 0.4f, 0.002f), "Black", false);
        ExitDoor door = pivot.AddComponent<ExitDoor>();
        door.leafCollider = leaf.GetComponent<Collider>();
        door.audioSource = Audio3D(pivot, 2f, 25f);
        refs.ExitDoor = door;

        // Cartel de SALIDA sobre la puerta (anomalía posible: "NO SALGAS")
        GameObject sign = Group("ExitSign", e, new Vector3(0f, 2.62f, 30f), 0f);
        Box("SignBox", sign.transform, new Vector3(0f, 0f, -0.03f), new Vector3(0.8f, 0.28f, 0.06f), "PlasticDark", false);
        Texture2D signTex = GameAssets.Sprite("exit_sign");
        Renderer signR = Image("SignFace", sign.transform, new Vector3(0f, 0f, -0.061f), 0.74f, 0.25f, signTex, "ImageGlow", new Color(0.9f, 0.9f, 0.9f));
        TextureSwapAnomaly signA = MakeAnomaly<TextureSwapAnomaly>(sign, "Cartel de salida", new Vector3(0f, 0f, -0.05f), new Vector3(0.9f, 0.4f, 0.15f));
        signA.target = signR; signA.normalTexture = signTex; signA.anomalyTexture = GameAssets.Sprite("exit_sign_anomaly");
        signA.glow = true; signA.glowNormal = new Color(0.9f, 0.9f, 0.9f); signA.glowAnomaly = new Color(1.5f, 1.2f, 1.2f);
        refs.ExitSignRenderer = signR;

        if (!finalRoom)
        {
            // Cuarto normal: detrás de la puerta solo hay oscuridad... y otra vez el mismo pasillo.
            Vestibule(e, 31.7f);
            return;
        }

        // Sala blanca de salida (solo en el último cuarto)
        Box("ExitFloor", e, new Vector3(0f, -0.05f, 33.2f), new Vector3(4.4f, 0.1f, 6f), "Tiles");
        Box("ExitCeiling", e, new Vector3(0f, 3.05f, 33.2f), new Vector3(4.4f, 0.1f, 6f), "WhiteWall");
        Box("ExitWallL", e, new Vector3(-2.1f, 1.5f, 33.2f), new Vector3(0.2f, 3.2f, 6f), "WhiteWall");
        Box("ExitWallR", e, new Vector3(2.1f, 1.5f, 33.2f), new Vector3(0.2f, 3.2f, 6f), "WhiteWall");
        Box("ExitWallBack", e, new Vector3(0f, 1.5f, 36.3f), new Vector3(4.4f, 3.2f, 0.2f), "WhiteWall");
        GameObject lgo = Group("ExitLight", e, new Vector3(0f, 2.6f, 33.5f), 0f);
        Light l = lgo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = Color.white;
        l.intensity = 4f;
        l.range = 12f;
        l.shadows = LightShadows.None;
    }

    // =====================================================================
    // Lámparas (una de ellas puede ser la anomalía "Luz apagada")
    // =====================================================================

    private static void BuildLamps(Transform root)
    {
        int anomalyIndex = Random.Range(1, 5);
        for (int k = 0; k < 6; k++)
        {
            float z = 2.5f + k * 5f;
            GameObject g = Group("Lamp_" + k, root, new Vector3(0f, 3f, z), 0f);
            Box("Housing", g.transform, new Vector3(0f, -0.005f, 0f), new Vector3(1.26f, 0.03f, 0.66f), "Metal", false);
            GameObject panel = Part("Panel", g.transform, MeshUtil.Box(new Vector3(1.2f, 0.05f, 0.6f)),
                                    MaterialLibrary.Instance("LampPanel"), new Vector3(0f, -0.025f, 0f), Vector3.zero);

            GameObject lg = Group("Light", g.transform, new Vector3(0f, -0.35f, 0f), 0f);
            Light light = lg.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.93f, 0.8f);
            light.intensity = 2.8f;
            light.range = 9f;
            light.shadows = LightShadows.None;
            refs.Lamps.Add(light);
            refs.LampBaseIntensity.Add(light.intensity);

            Vector3 zc = new Vector3(0f, -0.1f, 0f), zs = new Vector3(1.35f, 0.3f, 0.75f);
            if (k == anomalyIndex)
            {
                LightAnomaly a = MakeAnomaly<LightAnomaly>(g, "Luz apagada", zc, zs);
                a.requiresUnseen = false;            // el parpadeo se puede ver: es parte del susto
                a.lightSource = light;
                a.panel = panel.GetComponent<Renderer>();
                a.audioSource = Audio3D(g, 2f, 22f);
            }
            else
            {
                MakeReportable(g, "Lámpara", zc, zs);
            }
        }
    }

    // =====================================================================
    // Pared izquierda (x = -2)
    // =====================================================================

    private static void BuildLeftWall(Transform root)
    {
        // Puerta 101 -> puede quedar entreabierta
        GameObject d101 = Group("Door_101", root, new Vector3(-2f, 0f, 4f), LeftYaw);
        Transform pivot101 = OfficeDoor(d101.transform);
        TransformAnomaly ajar = MakeAnomaly<TransformAnomaly>(d101, "Puerta entreabierta", new Vector3(0f, 1.07f, -0.1f), new Vector3(1.15f, 2.2f, 0.22f));
        ajar.target = pivot101;
        ajar.rotationOffset = new Vector3(0f, 80f, 0f);
        ajar.minPlayerDistance = 2.5f;                  // la hoja sale al pasillo: nunca encima del jugador
        DoorPlate(root, "101", new Vector3(-2f, 1.55f, 4.85f), LeftYaw, false);

        // Cuadro marino -> se mueve y se ladea
        GameObject sea = Painting(root, "Painting_Sea", new Vector3(-2f, 1.65f, 8f), LeftYaw, GameAssets.Sprite("painting_sea"));
        TransformAnomaly seaA = MakeAnomaly<TransformAnomaly>(sea, "Cuadro torcido", new Vector3(0f, 0f, -0.04f), new Vector3(1.4f, 1f, 0.14f));
        seaA.positionOffset = new Vector3(0f, -0.35f, 0.9f);
        seaA.rotationOffset = new Vector3(0f, 0f, 28f);

        // Planta -> crece
        GameObject plant = Plant(root, new Vector3(-1.62f, 0f, 12.2f));
        TransformAnomaly plantA = MakeAnomaly<TransformAnomaly>(plant, "Planta más grande", new Vector3(0f, 0.75f, 0f), new Vector3(0.65f, 1.5f, 0.65f));
        plantA.scaleMultiplier = 2.1f;
        plantA.minPlayerDistance = 2f;

        // Paisaje -> aparece una figura en el camino
        Texture2D land = GameAssets.Sprite("painting_landscape");
        GameObject landGo = Painting(root, "Painting_Landscape", new Vector3(-2f, 1.65f, 15.5f), LeftYaw, land);
        TextureSwapAnomaly landA = MakeAnomaly<TextureSwapAnomaly>(landGo, "Figura en el cuadro", new Vector3(0f, 0f, -0.04f), new Vector3(1.4f, 1f, 0.14f));
        landA.target = landGo.transform.Find("Canvas").GetComponent<Renderer>();
        landA.normalTexture = land; landA.anomalyTexture = GameAssets.Sprite("painting_landscape_anomaly");

        // Puerta 103 (normal) y su placa -> el número cambia a 666
        GameObject d103 = Group("Door_103", root, new Vector3(-2f, 0f, 19f), LeftYaw);
        OfficeDoor(d103.transform);
        MakeReportable(d103, "Puerta 103", new Vector3(0f, 1.07f, -0.1f), new Vector3(1.15f, 2.2f, 0.22f));
        DoorPlate(root, "103", new Vector3(-2f, 1.55f, 19.85f), LeftYaw, true);

        // Retrato -> ojos vacíos
        Texture2D port = GameAssets.Sprite("painting_portrait");
        GameObject portGo = Painting(root, "Painting_Portrait", new Vector3(-2f, 1.65f, 23f), LeftYaw, port);
        bool youPortrait = Random.value < 0.5f;
        TextureSwapAnomaly portA = MakeAnomaly<TextureSwapAnomaly>(portGo, youPortrait ? "El retrato eres tú" : "Retrato", new Vector3(0f, 0f, -0.04f), new Vector3(1.4f, 1f, 0.14f));
        portA.target = portGo.transform.Find("Canvas").GetComponent<Renderer>();
        portA.normalTexture = port; portA.anomalyTexture = GameAssets.Sprite(youPortrait ? "painting_guard" : "painting_portrait_anomaly");

        // Reloj -> gira hacia atrás
        GameObject clockGo = Clock(root, new Vector3(-2f, 2.2f, 26.5f), LeftYaw);
        ClockAnomaly clockA = MakeAnomaly<ClockAnomaly>(clockGo, "Reloj al revés", new Vector3(0f, 0f, -0.04f), new Vector3(0.6f, 0.6f, 0.12f));
        clockA.clock = clockGo.GetComponent<WallClock>();
    }

    // =====================================================================
    // Pared derecha (x = +2)
    // =====================================================================

    private static void BuildRightWall(Transform root)
    {
        // Escritorio A: la silla puede desaparecer; el monitor es normal
        GameObject deskA = Desk(root, "Desk_A", new Vector3(2f, 0f, 5.5f));
        GameObject chairA = Chair(deskA.transform, "Chair_A", new Vector3(0.1f, 0f, -1.05f), 0f);
        VisibilityAnomaly chairGone = MakeAnomaly<VisibilityAnomaly>(chairA, "Silla desaparecida", new Vector3(0f, 0.5f, 0f), new Vector3(0.75f, 1f, 0.75f));
        chairGone.mode = VisibilityAnomaly.Mode.Hide;
        chairGone.Init();
        Renderer monA;
        GameObject monAGo = Monitor(deskA.transform, out monA);
        MakeReportable(monAGo, "Monitor", new Vector3(0f, 0.33f, 0f), new Vector3(0.65f, 0.6f, 0.3f));

        // Puerta 102 (normal)
        GameObject d102 = Group("Door_102", root, new Vector3(2f, 0f, 8.6f), RightYaw);
        OfficeDoor(d102.transform);
        MakeReportable(d102, "Puerta 102", new Vector3(0f, 1.07f, -0.1f), new Vector3(1.15f, 2.2f, 0.22f));
        DoorPlate(root, "102", new Vector3(2f, 1.55f, 7.75f), RightYaw, false);

        // Póster "Mantén la calma" -> "No mires dos veces"
        Texture2D calm = GameAssets.Sprite("poster_calm");
        GameObject calmGo = Poster(root, "Poster_Calm", new Vector3(2f, 1.6f, 11f), RightYaw, calm);
        TextureSwapAnomaly calmA = MakeAnomaly<TextureSwapAnomaly>(calmGo, "Póster cambiado", new Vector3(0f, 0f, -0.03f), new Vector3(0.72f, 0.97f, 0.1f));
        calmA.target = calmGo.transform.Find("Paper").GetComponent<Renderer>();
        calmA.normalTexture = calm; calmA.anomalyTexture = GameAssets.Sprite("poster_calm_anomaly");

        // Fuente de agua -> desaparece
        GameObject cooler = WaterCooler(root, new Vector3(2f, 0f, 13.6f));
        VisibilityAnomaly coolerGone = MakeAnomaly<VisibilityAnomaly>(cooler, "Fuente de agua desaparecida", new Vector3(0f, 0.75f, -0.22f), new Vector3(0.55f, 1.5f, 0.55f));
        coolerGone.mode = VisibilityAnomaly.Mode.Hide;
        coolerGone.Init();

        // Escritorio B: la silla puede girarse y el monitor encenderse
        GameObject deskB = Desk(root, "Desk_B", new Vector3(2f, 0f, 16.5f));
        GameObject chairB = Chair(deskB.transform, "Chair_B", new Vector3(-0.1f, 0f, -1.05f), 0f);
        TransformAnomaly chairTurn = MakeAnomaly<TransformAnomaly>(chairB, "Silla girada", new Vector3(0f, 0.5f, 0f), new Vector3(0.75f, 1f, 0.75f));
        chairTurn.positionOffset = new Vector3(0.3f, 0f, -0.75f);
        chairTurn.rotationOffset = new Vector3(0f, 180f, 0f);
        chairTurn.minPlayerDistance = 2.5f;
        Renderer monB;
        GameObject monBGo = Monitor(deskB.transform, out monB);
        TextureSwapAnomaly monBA = MakeAnomaly<TextureSwapAnomaly>(monBGo, "Monitor encendido", new Vector3(0f, 0.33f, 0f), new Vector3(0.65f, 0.6f, 0.3f));
        monBA.target = monB; monBA.glow = true;
        monBA.normalTexture = GameAssets.Sprite("monitor_off"); monBA.anomalyTexture = GameAssets.Sprite("monitor_anomaly");
        monBA.glowNormal = new Color(0.5f, 0.5f, 0.5f); monBA.glowAnomaly = new Color(1.3f, 1.3f, 1.3f);

        // Archivador -> el cajón de arriba se abre solo
        Transform drawer;
        GameObject cab = Cabinet(root, new Vector3(2f, 0f, 20.5f), out drawer);
        TransformAnomaly drawerA = MakeAnomaly<TransformAnomaly>(cab, "Cajón abierto", new Vector3(0f, 0.72f, -0.35f), new Vector3(0.65f, 1.45f, 0.9f));
        drawerA.target = drawer;
        drawerA.positionOffset = new Vector3(0f, 0f, -0.55f);
        drawerA.minPlayerDistance = 2f;

        // Póster de seguridad: "127 días sin incidentes" -> "0"
        Texture2D safety = GameAssets.Sprite("poster_safety");
        GameObject safetyGo = Poster(root, "Poster_Safety", new Vector3(2f, 1.6f, 23.4f), RightYaw, safety);
        TextureSwapAnomaly safetyA = MakeAnomaly<TextureSwapAnomaly>(safetyGo, "Días sin incidentes", new Vector3(0f, 0f, -0.03f), new Vector3(0.72f, 0.97f, 0.1f));
        safetyA.target = safetyGo.transform.Find("Paper").GetComponent<Renderer>();
        safetyA.normalTexture = safety; safetyA.anomalyTexture = GameAssets.Sprite("poster_safety_anomaly");

        // Huellas de manos -> aparecen en la pared
        GameObject hands = Group("Handprints", root, new Vector3(2f, 1.45f, 26.3f), RightYaw);
        Image("Decal", hands.transform, new Vector3(0f, 0f, -0.004f), 2.0f, 2.0f, GameAssets.Sprite("handprints"), "ImageCutout");
        VisibilityAnomaly handsA = MakeAnomaly<VisibilityAnomaly>(hands, "Huellas en la pared", new Vector3(0f, 0f, -0.03f), new Vector3(2.1f, 2.1f, 0.1f));
        handsA.mode = VisibilityAnomaly.Mode.Show;
        handsA.minPlayerDistance = 3f;
        handsA.Init();
    }

    // =====================================================================
    // En medio del pasillo
    // =====================================================================

    private static void BuildCorridorExtras(Transform root)
    {
        // Una silla de más aparece en mitad del pasillo, mirándote
        GameObject extra = Chair(root, "Chair_Extra", new Vector3(0.3f, 0f, 21f), 180f);
        VisibilityAnomaly extraA = MakeAnomaly<VisibilityAnomaly>(extra, "Silla de más", new Vector3(0f, 0.5f, 0f), new Vector3(0.75f, 1f, 0.75f));
        extraA.mode = VisibilityAnomaly.Mode.Show;
        extraA.minPlayerDistance = 4f;
        extraA.Init();

        // Alguien al fondo del pasillo
        GameObject fig = Figure(root, new Vector3(0.65f, 0f, 28.4f));
        VisibilityAnomaly figA = MakeAnomaly<VisibilityAnomaly>(fig, "Alguien al fondo", new Vector3(0f, 1.05f, 0f), new Vector3(0.7f, 2.1f, 0.6f));
        figA.mode = VisibilityAnomaly.Mode.Show;
        figA.minPlayerDistance = 6f;
        figA.Init();
        FaceCamera fc = fig.AddComponent<FaceCamera>();
        fc.target = cam;

        // Un pato de goma gigante en mitad del pasillo (sí, de verdad)
        GameObject duck = Duck(root, new Vector3(-0.35f, 0f, 14.2f));
        VisibilityAnomaly duckA = MakeAnomaly<VisibilityAnomaly>(duck, "Pato de goma gigante", new Vector3(0f, 0.7f, 0.1f), new Vector3(1.1f, 1.45f, 1.3f));
        duckA.mode = VisibilityAnomaly.Mode.Show;
        duckA.minPlayerDistance = 3f;
        duckA.Init();

        // Otro vigilante igual que tú, que te imita como un espejo
        GameObject twin = Group("OtroVigilante", root, new Vector3(0f, 0f, 27.5f), 180f);
        MirrorGuardAnomaly twinA = MakeAnomaly<MirrorGuardAnomaly>(twin, "Otro vigilante", new Vector3(0f, 0.9f, 0f), new Vector3(0.7f, 1.85f, 0.6f));
        twinA.minPlayerDistance = 8f;
        PlayerAvatar.Create(twinA, twinA, AvatarStyle.Guard);
        twinA.Init();

        // La sombra que te persigue (el GameManager la activa a partir del cuarto 3)
        GameObject shadow = Group("LaSombra", root, new Vector3(0f, 0f, 0.8f), 0f);
        ShadowChaserAnomaly shadowA = MakeAnomaly<ShadowChaserAnomaly>(shadow, "La sombra", new Vector3(0f, 0.95f, 0f), new Vector3(0.8f, 1.9f, 0.7f));
        shadowA.requiresUnseen = false;
        shadowA.voice = Audio3D(shadow, 1.5f, 14f);
        PlayerAvatar.Create(shadowA, shadowA, AvatarStyle.Shadow);
        shadowA.Init();
    }

    private static GameObject Duck(Transform root, Vector3 pos)
    {
        GameObject g = Group("PatoGigante", root, pos, 160f);
        Transform t = g.transform;
        Part("Body", t, MeshUtil.Sphere(0.5f, 12, 16), MaterialLibrary.Get("Duck"), new Vector3(0f, 0.42f, 0f), Vector3.zero).transform.localScale = new Vector3(1.05f, 0.8f, 1.25f);
        Part("Tail", t, MeshUtil.Sphere(0.18f, 8, 10), MaterialLibrary.Get("Duck"), new Vector3(0f, 0.62f, -0.55f), Vector3.zero);
        Part("Head", t, MeshUtil.Sphere(0.3f, 12, 16), MaterialLibrary.Get("Duck"), new Vector3(0f, 1.0f, 0.35f), Vector3.zero);
        Part("Beak", t, MeshUtil.Sphere(0.14f, 8, 10), MaterialLibrary.Get("Beak"), new Vector3(0f, 0.95f, 0.66f), Vector3.zero).transform.localScale = new Vector3(1.3f, 0.45f, 1.2f);
        Part("EyeL", t, MeshUtil.Sphere(0.04f, 6, 8), MaterialLibrary.Get("Black"), new Vector3(-0.15f, 1.1f, 0.58f), Vector3.zero);
        Part("EyeR", t, MeshUtil.Sphere(0.04f, 6, 8), MaterialLibrary.Get("Black"), new Vector3(0.15f, 1.1f, 0.58f), Vector3.zero);
        BoxCollider solid = g.AddComponent<BoxCollider>();
        solid.center = new Vector3(0f, 0.65f, 0.05f);
        solid.size = new Vector3(1f, 1.3f, 1.2f);
        return g;
    }

    // =====================================================================
    // Objetos
    // =====================================================================

    /// Puerta de oficina (decorativa) en un grupo de pared. Devuelve el pivote de la hoja (bisagra).
    private static Transform OfficeDoor(Transform g)
    {
        Box("Void", g, new Vector3(0f, 1.075f, 0.005f), new Vector3(1f, 2.15f, 0.02f), "Black", false);
        Box("TrimL", g, new Vector3(-0.54f, 1.115f, -0.025f), new Vector3(0.08f, 2.23f, 0.05f), "DarkWood", false);
        Box("TrimR", g, new Vector3(0.54f, 1.115f, -0.025f), new Vector3(0.08f, 2.23f, 0.05f), "DarkWood", false);
        Box("TrimTop", g, new Vector3(0f, 2.19f, -0.025f), new Vector3(1.16f, 0.08f, 0.05f), "DarkWood", false);
        GameObject pivot = Group("Pivot", g, new Vector3(-0.5f, 0f, -0.03f), 0f);
        Box("Leaf", pivot.transform, new Vector3(0.49f, 1.065f, 0f), new Vector3(0.98f, 2.13f, 0.045f), "DoorPaint");
        Box("PanelTop", pivot.transform, new Vector3(0.49f, 1.55f, -0.025f), new Vector3(0.7f, 0.75f, 0.01f), "DarkWood", false);
        Box("PanelBottom", pivot.transform, new Vector3(0.49f, 0.55f, -0.025f), new Vector3(0.7f, 0.75f, 0.01f), "DarkWood", false);
        Box("Handle", pivot.transform, new Vector3(0.86f, 1.0f, -0.055f), new Vector3(0.13f, 0.025f, 0.03f), "Metal", false);
        Box("Rosette", pivot.transform, new Vector3(0.9f, 1.0f, -0.03f), new Vector3(0.05f, 0.08f, 0.01f), "Metal", false);
        return pivot.transform;
    }

    private static void DoorPlate(Transform root, string number, Vector3 pos, float yaw, bool canChange)
    {
        GameObject g = Group("Plate_" + number, root, pos, yaw);
        Box("Back", g.transform, new Vector3(0f, 0f, -0.006f), new Vector3(0.40f, 0.20f, 0.012f), "PlasticDark", false);
        Texture2D tex = GameAssets.Sprite("door_plate_" + number);
        Renderer r = Image("Face", g.transform, new Vector3(0f, 0f, -0.013f), 0.37f, 0.165f, tex, "Image");
        Vector3 zc = new Vector3(0f, 0f, -0.03f), zs = new Vector3(0.46f, 0.26f, 0.1f);
        if (canChange)
        {
            TextureSwapAnomaly a = MakeAnomaly<TextureSwapAnomaly>(g, "Número de puerta", zc, zs);
            a.target = r; a.normalTexture = tex; a.anomalyTexture = GameAssets.Sprite("door_plate_anomaly");
        }
        else
        {
            MakeReportable(g, "Placa " + number, zc, zs);
        }
    }

    private static GameObject Painting(Transform root, string name, Vector3 pos, float yaw, Texture tex)
    {
        GameObject g = Group(name, root, pos, yaw);
        Box("Frame", g.transform, new Vector3(0f, 0f, -0.025f), new Vector3(1.3f, 0.9f, 0.05f), "DarkWood");
        Image("Canvas", g.transform, new Vector3(0f, 0f, -0.0505f), 1.16f, 0.76f, tex, "Image");
        return g;
    }

    private static GameObject Poster(Transform root, string name, Vector3 pos, float yaw, Texture tex)
    {
        GameObject g = Group(name, root, pos, yaw);
        Image("Paper", g.transform, new Vector3(0f, 0f, -0.004f), 0.6f, 0.85f, tex, "Image");
        Vector3[] pins = { new Vector3(-0.27f, 0.39f, -0.008f), new Vector3(0.27f, 0.39f, -0.008f), new Vector3(-0.27f, -0.39f, -0.008f), new Vector3(0.27f, -0.39f, -0.008f) };
        for (int i = 0; i < pins.Length; i++)
            Part("Pin", g.transform, MeshUtil.Sphere(0.012f, 6, 8), MaterialLibrary.Get("Metal"), pins[i], Vector3.zero);
        return g;
    }

    private static GameObject Plant(Transform root, Vector3 pos)
    {
        GameObject g = Group("Plant", root, pos, 0f);
        Part("Pot", g.transform, MeshUtil.Cylinder(0.2f, 0.38f), MaterialLibrary.Get("Pot"), Vector3.zero, Vector3.zero);
        Part("Soil", g.transform, MeshUtil.Cylinder(0.18f, 0.02f), MaterialLibrary.Get("Black"), new Vector3(0f, 0.37f, 0f), Vector3.zero);
        Part("Trunk", g.transform, MeshUtil.Cylinder(0.025f, 0.55f, 8), MaterialLibrary.Get("DarkWood"), new Vector3(0f, 0.38f, 0f), Vector3.zero);
        Vector3[] blobs = { new Vector3(0f, 1.05f, 0f), new Vector3(0.12f, 0.9f, 0.08f), new Vector3(-0.12f, 0.92f, -0.06f),
                            new Vector3(0.05f, 1.2f, -0.08f), new Vector3(-0.07f, 0.8f, 0.1f) };
        float[] radii = { 0.22f, 0.17f, 0.18f, 0.15f, 0.14f };
        for (int i = 0; i < blobs.Length; i++)
            Part("Leaves", g.transform, MeshUtil.Sphere(radii[i], 8, 12), MaterialLibrary.Get("Leaf"), blobs[i], Vector3.zero);
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f;
            GameObject leaf = Part("Leaf", g.transform, MeshUtil.Box(new Vector3(0.09f, 0.42f, 0.01f)), MaterialLibrary.Get("Leaf"),
                                   new Vector3(0f, 0.7f, 0f), new Vector3(0f, a, 0f));
            leaf.transform.localRotation = Quaternion.Euler(0f, a, 0f) * Quaternion.Euler(35f, 0f, 0f);
            leaf.transform.localPosition += leaf.transform.localRotation * new Vector3(0f, 0.21f, 0f);
        }
        BoxCollider solid = g.AddComponent<BoxCollider>();
        solid.center = new Vector3(0f, 0.5f, 0f);
        solid.size = new Vector3(0.42f, 1f, 0.42f);
        return g;
    }

    private static GameObject Desk(Transform root, string name, Vector3 pos)
    {
        GameObject g = Group(name, root, pos, RightYaw);
        Transform t = g.transform;
        Box("Top", t, new Vector3(0f, 0.74f, -0.4f), new Vector3(1.6f, 0.05f, 0.75f), "Wood");
        Box("Modesty", t, new Vector3(0f, 0.48f, -0.07f), new Vector3(1.5f, 0.45f, 0.02f), "Wood", false);
        Box("LegFL", t, new Vector3(-0.75f, 0.36f, -0.73f), new Vector3(0.05f, 0.72f, 0.05f), "Metal", false);
        Box("LegFR", t, new Vector3(0.75f, 0.36f, -0.73f), new Vector3(0.05f, 0.72f, 0.05f), "Metal", false);
        Box("LegBL", t, new Vector3(-0.75f, 0.36f, -0.07f), new Vector3(0.05f, 0.72f, 0.05f), "Metal", false);
        Box("LegBR", t, new Vector3(0.75f, 0.36f, -0.07f), new Vector3(0.05f, 0.72f, 0.05f), "Metal", false);
        Box("Keyboard", t, new Vector3(0.12f, 0.775f, -0.55f), new Vector3(0.45f, 0.02f, 0.15f), "PlasticDark", false);
        GameObject papers = Box("Papers", t, new Vector3(-0.5f, 0.78f, -0.42f), new Vector3(0.21f, 0.03f, 0.29f), "Plastic", false);
        papers.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
        Part("Mug", t, MeshUtil.Cylinder(0.04f, 0.1f, 12), MaterialLibrary.Get("Plastic"), new Vector3(-0.28f, 0.765f, -0.62f), Vector3.zero);
        // Colisión que llega al suelo para que no se pueda pasar por debajo
        BoxCollider body = g.AddComponent<BoxCollider>();
        body.center = new Vector3(0f, 0.38f, -0.4f);
        body.size = new Vector3(1.6f, 0.76f, 0.75f);
        return g;
    }

    private static GameObject Monitor(Transform desk, out Renderer screen)
    {
        GameObject m = Group("Monitor", desk, new Vector3(0.15f, 0.765f, -0.25f), 0f);
        Box("Base", m.transform, new Vector3(0f, 0.01f, 0.02f), new Vector3(0.22f, 0.02f, 0.16f), "PlasticDark", false);
        Box("Neck", m.transform, new Vector3(0f, 0.14f, 0.04f), new Vector3(0.04f, 0.26f, 0.03f), "PlasticDark", false);
        Box("Housing", m.transform, new Vector3(0f, 0.43f, 0f), new Vector3(0.56f, 0.36f, 0.04f), "PlasticDark");
        screen = Image("Screen", m.transform, new Vector3(0f, 0.43f, -0.0205f), 0.51f, 0.31f, GameAssets.Sprite("monitor_off"),
                       "ImageGlow", new Color(0.5f, 0.5f, 0.5f));
        return m;
    }

    private static GameObject Chair(Transform parent, string name, Vector3 localPos, float yaw)
    {
        GameObject g = Group(name, parent, localPos, yaw);
        Mesh mesh = MeshLoader.Load("chair");
        if (mesh != null)
        {
            Part("Mesh", g.transform, mesh, MaterialLibrary.Get("Chair"), Vector3.zero, Vector3.zero);
        }
        else
        {
            // Plan B si falta el modelo: silla hecha con cajas
            Box("Seat", g.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.5f, 0.05f, 0.5f), "Plastic", false);
            Box("Back", g.transform, new Vector3(0f, 0.72f, -0.23f), new Vector3(0.5f, 0.5f, 0.05f), "Plastic", false);
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    Box("Leg", g.transform, new Vector3(sx * 0.21f, 0.22f, sz * 0.21f), new Vector3(0.04f, 0.44f, 0.04f), "Plastic", false);
        }
        BoxCollider solid = g.AddComponent<BoxCollider>();
        solid.center = new Vector3(0f, 0.44f, -0.02f);
        solid.size = new Vector3(0.6f, 0.88f, 0.6f);
        return g;
    }

    private static GameObject WaterCooler(Transform root, Vector3 pos)
    {
        GameObject g = Group("WaterCooler", root, pos, RightYaw);
        Transform t = g.transform;
        Box("Body", t, new Vector3(0f, 0.475f, -0.22f), new Vector3(0.34f, 0.95f, 0.34f), "Plastic", false);
        Part("Bottle", t, MeshUtil.Cylinder(0.14f, 0.42f), MaterialLibrary.Get("Water"), new Vector3(0f, 0.97f, -0.22f), Vector3.zero);
        Part("Neck", t, MeshUtil.Cylinder(0.05f, 0.03f, 12), MaterialLibrary.Get("Water"), new Vector3(0f, 0.95f, -0.22f), Vector3.zero);
        Box("TapBlue", t, new Vector3(-0.07f, 0.78f, -0.4f), new Vector3(0.05f, 0.06f, 0.04f), "Water", false);
        Box("TapRed", t, new Vector3(0.07f, 0.78f, -0.4f), new Vector3(0.05f, 0.06f, 0.04f), "Pot", false);
        Box("Tray", t, new Vector3(0f, 0.62f, -0.42f), new Vector3(0.24f, 0.02f, 0.08f), "PlasticDark", false);
        Part("Cups", t, MeshUtil.Cylinder(0.04f, 0.25f, 12), MaterialLibrary.Get("Plastic"), new Vector3(0.21f, 0.5f, -0.22f), Vector3.zero);
        BoxCollider solid = g.AddComponent<BoxCollider>();
        solid.center = new Vector3(0f, 0.7f, -0.22f);
        solid.size = new Vector3(0.4f, 1.4f, 0.4f);
        return g;
    }

    private static GameObject Cabinet(Transform root, Vector3 pos, out Transform topDrawer)
    {
        GameObject g = Group("FilingCabinet", root, pos, RightYaw);
        Transform t = g.transform;
        Box("Body", t, new Vector3(0f, 0.66f, -0.3f), new Vector3(0.5f, 1.32f, 0.6f), "Metal");
        for (int i = 0; i < 2; i++)
        {
            float y = 0.25f + i * 0.42f;
            Box("DrawerFront", t, new Vector3(0f, y, -0.61f), new Vector3(0.44f, 0.36f, 0.02f), "Metal", false);
            Box("DrawerHandle", t, new Vector3(0f, y + 0.08f, -0.635f), new Vector3(0.14f, 0.025f, 0.03f), "Metal", false);
        }
        GameObject drawer = Group("TopDrawer", t, new Vector3(0f, 1.09f, -0.3f), 0f);
        Box("Front", drawer.transform, new Vector3(0f, 0f, -0.31f), new Vector3(0.44f, 0.36f, 0.02f), "Metal", false);
        Box("Handle", drawer.transform, new Vector3(0f, 0.08f, -0.335f), new Vector3(0.14f, 0.025f, 0.03f), "Metal", false);
        Box("Box", drawer.transform, new Vector3(0f, -0.02f, 0f), new Vector3(0.42f, 0.3f, 0.58f), "Metal", false);
        Box("Folders", drawer.transform, new Vector3(0f, 0.1f, 0f), new Vector3(0.38f, 0.1f, 0.5f), "Plastic", false);
        topDrawer = drawer.transform;
        return g;
    }

    private static GameObject Clock(Transform root, Vector3 pos, float yaw)
    {
        GameObject g = Group("WallClock", root, pos, yaw);
        Transform t = g.transform;
        Part("Rim", t, MeshUtil.Cylinder(0.26f, 0.05f, 28), MaterialLibrary.Get("PlasticDark"), new Vector3(0f, 0f, -0.05f), new Vector3(90f, 0f, 0f));
        Image("Face", t, new Vector3(0f, 0f, -0.0515f), 0.5f, 0.5f, GameAssets.Sprite("clock_face"), "ImageCutout");
        WallClock clock = g.AddComponent<WallClock>();
        clock.hourHand = Hand(t, "Hour", -0.055f, new Vector3(0.025f, 0.13f, 0.006f));
        clock.minuteHand = Hand(t, "Minute", -0.06f, new Vector3(0.016f, 0.19f, 0.006f));
        clock.secondHand = Hand(t, "Second", -0.065f, new Vector3(0.006f, 0.2f, 0.004f));
        Part("Cap", t, MeshUtil.Sphere(0.015f, 6, 8), MaterialLibrary.Get("Black"), new Vector3(0f, 0f, -0.068f), Vector3.zero);
        clock.tickSource = Audio3D(g, 2.5f, 18f);
        return g;
    }

    private static Transform Hand(Transform clock, string name, float z, Vector3 size)
    {
        GameObject pivot = Group(name + "Hand", clock, new Vector3(0f, 0f, z), 0f);
        Box(name, pivot.transform, new Vector3(0f, size.y * 0.42f, 0f), size, "Black", false);
        return pivot.transform;
    }

    private static GameObject Figure(Transform root, Vector3 pos)
    {
        GameObject g = Group("Figure", root, pos, 180f);
        Transform t = g.transform;
        string m = "Figure";
        Box("LegL", t, new Vector3(-0.09f, 0.48f, 0f), new Vector3(0.11f, 0.96f, 0.13f), m, false);
        Box("LegR", t, new Vector3(0.09f, 0.48f, 0f), new Vector3(0.11f, 0.96f, 0.13f), m, false);
        Box("Torso", t, new Vector3(0f, 1.33f, 0f), new Vector3(0.36f, 0.76f, 0.2f), m, false);
        GameObject armL = Box("ArmL", t, new Vector3(-0.25f, 1.17f, 0f), new Vector3(0.08f, 0.9f, 0.1f), m, false);
        GameObject armR = Box("ArmR", t, new Vector3(0.25f, 1.17f, 0f), new Vector3(0.08f, 0.9f, 0.1f), m, false);
        armL.transform.localRotation = Quaternion.Euler(0f, 0f, -4f);
        armR.transform.localRotation = Quaternion.Euler(0f, 0f, 4f);
        Box("Neck", t, new Vector3(0f, 1.76f, 0f), new Vector3(0.08f, 0.1f, 0.08f), m, false);
        GameObject head = Part("Head", t, MeshUtil.Sphere(0.12f), MaterialLibrary.Get(m), new Vector3(0f, 1.92f, 0f), Vector3.zero);
        head.transform.localScale = new Vector3(1f, 1.3f, 1f);
        Part("EyeL", t, MeshUtil.Sphere(0.016f, 6, 8), MaterialLibrary.Get("Glow"), new Vector3(-0.042f, 1.94f, 0.11f), Vector3.zero);
        Part("EyeR", t, MeshUtil.Sphere(0.016f, 6, 8), MaterialLibrary.Get("Glow"), new Vector3(0.042f, 1.94f, 0.11f), Vector3.zero);
        return g;
    }

    // =====================================================================
    // Ayudantes
    // =====================================================================

    private static GameObject Group(string name, Transform parent, Vector3 localPos, float yaw)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return g;
    }

    private static GameObject Part(string name, Transform parent, Mesh mesh, Material mat, Vector3 localPos, Vector3 euler)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    /// Caja con UV en metros. "solid" añade un BoxCollider (paredes, muebles).
    private static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 size, string matKey, bool solid = true)
    {
        GameObject go = Part(name, parent, MeshUtil.Box(size), MaterialLibrary.Get(matKey), localPos, Vector3.zero);
        if (solid)
        {
            BoxCollider c = go.AddComponent<BoxCollider>();
            c.size = size;
        }
        return go;
    }

    /// Imagen plana (cuadro, cartel, pantalla...) con su propio material. Mira hacia el -Z local del padre.
    private static Renderer Image(string name, Transform parent, Vector3 localPos, float w, float h, Texture tex, string matKey,
                                  Color? glow = null)
    {
        Material m = MaterialLibrary.Instance(matKey);
        MaterialLibrary.SetMainTexture(m, tex);
        MaterialLibrary.SetTiling(m, Vector2.one);
        MaterialLibrary.SetColor(m, Color.white);
        if (glow.HasValue) MaterialLibrary.SetEmission(m, glow.Value, tex);
        GameObject go = Part(name, parent, MeshUtil.Quad(w, h), m, localPos, Vector3.zero);
        return go.GetComponent<Renderer>();
    }

    private static BoxCollider Zone(GameObject go, Vector3 center, Vector3 size)
    {
        BoxCollider z = go.AddComponent<BoxCollider>();
        z.isTrigger = true;
        z.center = center;
        z.size = size;
        return z;
    }

    private static Reportable MakeReportable(GameObject go, string name, Vector3 zoneCenter, Vector3 zoneSize)
    {
        Reportable r = go.AddComponent<Reportable>();
        r.displayName = name;
        r.zone = Zone(go, zoneCenter, zoneSize);
        return r;
    }

    private static T MakeAnomaly<T>(GameObject go, string name, Vector3 zoneCenter, Vector3 zoneSize) where T : Anomaly
    {
        T a = go.AddComponent<T>();
        a.displayName = name;
        a.anomalyName = name;
        a.zone = Zone(go, zoneCenter, zoneSize);
        a.Bind(cam);
        refs.Pool.Add(a);
        return a;
    }

    private static AudioSource Audio3D(GameObject go, float minDistance, float maxDistance)
    {
        AudioSource s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.spatialBlend = 1f;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = minDistance;
        s.maxDistance = maxDistance;
        s.dopplerLevel = 0f;
        return s;
    }
}
