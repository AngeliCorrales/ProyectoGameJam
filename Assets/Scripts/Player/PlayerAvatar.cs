using UnityEngine;

/// Cuerpo del jugador: un vigilante nocturno (uniforme azul, gorra, placa, zapatos) hecho con piezas,
/// con animación procedural: caminar / correr (piernas y brazos), respiración en reposo,
/// cabeza que sigue la mirada y gesto de señalar al reportar.
/// En primera persona se oculta la cabeza (para no tapar la cámara) y ves tu cuerpo, piernas y brazo.
/// Lo que mueve a un personaje: el jugador, Mario o el "otro vigilante".
public interface IAvatarDriver
{
    bool ThirdPerson { get; }     // false = primera persona (se oculta la cabeza)
    float Speed { get; }          // m/s
    float WalkPhase { get; }      // radianes
    float Pitch { get; }          // grados (positivo = mirar abajo)
}

public enum AvatarStyle { Guard, Office, Shadow }

public class PlayerAvatar : MonoBehaviour
{
    private IAvatarDriver controller;
    private AvatarStyle style;
    private Transform body, hips, spine, neck, head;
    private Transform shoulderL, shoulderR, elbowL, elbowR;
    private Transform hipL, hipR, kneeL, kneeR, ankleL, ankleR;
    private Renderer[] headParts;
    private Renderer[] shadowEyes;

    private float move;            // 0 = quieto, 1 = andando, 1.6 = corriendo (suavizado)
    private float pointTimer = -1f;
    private const float PointDuration = 0.75f;
    private bool lastThirdPerson = true;

    // ===================== Construcción =====================

    public static PlayerAvatar Create(PlayerController pc)
    {
        return Create(pc, pc, AvatarStyle.Guard);
    }

    public static PlayerAvatar Create(MonoBehaviour owner, IAvatarDriver driver, AvatarStyle style)
    {
        GameObject root = new GameObject(style == AvatarStyle.Guard ? "Avatar (vigilante)" : "Avatar (oficinista)");
        root.transform.SetParent(owner.transform, false);
        PlayerAvatar a = root.AddComponent<PlayerAvatar>();
        a.controller = driver;
        a.style = style;
        a.Build();
        SetLayer(root.transform, owner.gameObject.layer);
        return a;
    }

    private string Shirt { get { return style == AvatarStyle.Guard ? "Uniform" : "Shirt"; } }

    private static void SetLayer(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        foreach (Transform c in t) SetLayer(c, layer);
    }

    private void Build()
    {
        body = Joint("Body", transform, Vector3.zero);
        hips = Joint("Hips", body, new Vector3(0f, 0.95f, 0f));

        // Pelvis y cinturón
        Piece("Pelvis", hips, MeshUtil.Cylinder(0.15f, 0.18f), "Pants", new Vector3(0f, -0.09f, 0f), new Vector3(1f, 1f, 0.72f));
        Piece("Belt", hips, MeshUtil.Cylinder(0.155f, 0.05f), "Shoe", new Vector3(0f, 0.06f, 0f), new Vector3(1f, 1f, 0.72f));
        Piece("Buckle", hips, MeshUtil.Box(new Vector3(0.06f, 0.04f, 0.02f)), "Badge", new Vector3(0f, 0.085f, 0.112f), Vector3.one);

        // Tronco
        spine = Joint("Spine", hips, new Vector3(0f, 0.1f, 0f));
        Piece("Torso", spine, MeshUtil.Cylinder(0.17f, 0.42f), Shirt, new Vector3(0f, -0.02f, 0f), new Vector3(1f, 1f, 0.64f));
        Piece("Chest", spine, MeshUtil.Sphere(0.17f, 8, 12), Shirt, new Vector3(0f, 0.38f, 0f), new Vector3(1.05f, 0.45f, 0.66f));
        if (style == AvatarStyle.Guard)
            Piece("Badge", spine, MeshUtil.Box(new Vector3(0.05f, 0.06f, 0.01f)), "Badge", new Vector3(-0.08f, 0.3f, 0.112f), Vector3.one);
        else
        {
            Piece("Tie", spine, MeshUtil.Box(new Vector3(0.05f, 0.3f, 0.012f)), "Tie", new Vector3(0f, 0.25f, 0.113f), Vector3.one);
            Piece("TieKnot", spine, MeshUtil.Box(new Vector3(0.04f, 0.04f, 0.02f)), "Tie", new Vector3(0f, 0.41f, 0.11f), Vector3.one);
        }
        Piece("PocketR", spine, MeshUtil.Box(new Vector3(0.08f, 0.08f, 0.01f)), "Pants", new Vector3(0.08f, 0.28f, 0.111f), Vector3.one);
        for (int i = 0; i < 4; i++)
            Piece("Button", spine, MeshUtil.Sphere(0.008f, 4, 6), "Badge", new Vector3(0f, 0.36f - i * 0.1f, 0.112f), Vector3.one);

        // Cuello y cabeza
        neck = Joint("Neck", spine, new Vector3(0f, 0.44f, 0f));
        Renderer n1 = Piece("Neck", neck, MeshUtil.Cylinder(0.05f, 0.09f), "Skin", Vector3.zero, Vector3.one);
        head = Joint("Head", neck, new Vector3(0f, 0.08f, 0f));
        Renderer h1 = Piece("Skull", head, MeshUtil.Sphere(0.105f, 10, 14), "Skin", new Vector3(0f, 0.11f, 0f), new Vector3(1f, 1.15f, 1.05f));
        Renderer h2 = Piece("Nose", head, MeshUtil.Sphere(0.018f, 5, 8), "Skin", new Vector3(0f, 0.1f, 0.11f), new Vector3(1f, 1.2f, 1f));
        Renderer h3 = Piece("EyeL", head, MeshUtil.Sphere(0.012f, 5, 8), "Black", new Vector3(-0.038f, 0.125f, 0.098f), Vector3.one);
        Renderer h4 = Piece("EyeR", head, MeshUtil.Sphere(0.012f, 5, 8), "Black", new Vector3(0.038f, 0.125f, 0.098f), Vector3.one);
        Renderer h5 = Piece("EarL", head, MeshUtil.Sphere(0.022f, 5, 8), "Skin", new Vector3(-0.105f, 0.11f, 0f), new Vector3(0.5f, 1.2f, 1f));
        Renderer h6 = Piece("EarR", head, MeshUtil.Sphere(0.022f, 5, 8), "Skin", new Vector3(0.105f, 0.11f, 0f), new Vector3(0.5f, 1.2f, 1f));
        Renderer h7 = Piece("Cap", head, MeshUtil.Cylinder(0.112f, 0.075f), "Uniform", new Vector3(0f, 0.165f, -0.005f), Vector3.one);
        Renderer h8 = Piece("CapTop", head, MeshUtil.Sphere(0.112f, 6, 12), "Uniform", new Vector3(0f, 0.24f, -0.005f), new Vector3(1f, 0.25f, 1f));
        Renderer h9 = Piece("Brim", head, MeshUtil.Box(new Vector3(0.19f, 0.012f, 0.1f)), "Shoe", new Vector3(0f, 0.17f, 0.13f), Vector3.one);
        Renderer h11 = Piece("Hair", head, MeshUtil.Sphere(0.108f, 10, 14), "Hair", new Vector3(0f, 0.125f, -0.012f), new Vector3(1.02f, 1.1f, 0.95f));
        Renderer h10 = Piece("CapBadge", head, MeshUtil.Box(new Vector3(0.035f, 0.03f, 0.008f)), "Badge", new Vector3(0f, 0.205f, 0.108f), Vector3.one);
        if (style == AvatarStyle.Office)
        {
            // Sin gorra: pelo completo y gafas
            h7.enabled = false; h8.enabled = false; h9.enabled = false; h10.enabled = false;
            h11.transform.localPosition = new Vector3(0f, 0.14f, -0.015f);
            h11.transform.localScale = new Vector3(1.06f, 1.1f, 0.98f);
            Renderer g1 = Piece("GlassL", head, MeshUtil.Box(new Vector3(0.05f, 0.035f, 0.006f)), "Black", new Vector3(-0.04f, 0.125f, 0.112f), Vector3.one);
            Renderer g2 = Piece("GlassR", head, MeshUtil.Box(new Vector3(0.05f, 0.035f, 0.006f)), "Black", new Vector3(0.04f, 0.125f, 0.112f), Vector3.one);
            headParts = new[] { n1, h1, h2, h3, h4, h5, h6, h11, g1, g2 };
        }
        else headParts = new[] { n1, h1, h2, h3, h4, h5, h6, h7, h8, h9, h10, h11 };

        // Sombra: la misma silueta del vigilante, toda negra y con ojos que brillan
        if (style == AvatarStyle.Shadow) shadowEyes = new[] { h3, h4 };

        // Brazos
        shoulderL = Arm("L", -1f, out elbowL);
        shoulderR = Arm("R", 1f, out elbowR);

        // Piernas
        hipL = Leg("L", -1f, out kneeL, out ankleL);
        hipR = Leg("R", 1f, out kneeR, out ankleR);

        if (style == AvatarStyle.Shadow)
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = MaterialLibrary.Get("Figure");
            foreach (Renderer r in shadowEyes) { r.sharedMaterial = MaterialLibrary.Get("Glow"); r.transform.localScale = Vector3.one * 2.6f; }
        }
    }

    private Transform Arm(string side, float sx, out Transform elbow)
    {
        Transform shoulder = Joint("Shoulder" + side, spine, new Vector3(0.215f * sx, 0.39f, 0f));
        Piece("ShoulderBall", shoulder, MeshUtil.Sphere(0.068f, 8, 10), Shirt, Vector3.zero, Vector3.one);
        Piece("UpperArm", shoulder, MeshUtil.Cylinder(0.05f, 0.29f, 10), Shirt, new Vector3(0f, -0.29f, 0f), Vector3.one);
        elbow = Joint("Elbow" + side, shoulder, new Vector3(0f, -0.29f, 0f));
        Piece("ElbowBall", elbow, MeshUtil.Sphere(0.047f, 6, 8), Shirt, Vector3.zero, Vector3.one);
        Piece("Forearm", elbow, MeshUtil.Cylinder(0.043f, 0.18f, 10), Shirt, new Vector3(0f, -0.18f, 0f), Vector3.one);
        Piece("Wrist", elbow, MeshUtil.Cylinder(0.033f, 0.07f, 8), "Skin", new Vector3(0f, -0.25f, 0f), Vector3.one);
        Piece("Hand", elbow, MeshUtil.Sphere(0.048f, 6, 8), "Skin", new Vector3(0f, -0.29f, 0.005f), new Vector3(0.75f, 1.2f, 1f));
        Piece("Thumb", elbow, MeshUtil.Sphere(0.018f, 4, 6), "Skin", new Vector3(-0.03f * sx, -0.27f, 0.03f), new Vector3(1f, 1.6f, 1f));
        return shoulder;
    }

    private Transform Leg(string side, float sx, out Transform knee, out Transform ankle)
    {
        Transform hip = Joint("Hip" + side, hips, new Vector3(0.093f * sx, 0f, 0f));
        Piece("Thigh", hip, MeshUtil.Cylinder(0.072f, 0.44f, 10), "Pants", new Vector3(0f, -0.44f, 0f), Vector3.one);
        knee = Joint("Knee" + side, hip, new Vector3(0f, -0.44f, 0f));
        Piece("KneeBall", knee, MeshUtil.Sphere(0.064f, 6, 8), "Pants", Vector3.zero, Vector3.one);
        Piece("Shin", knee, MeshUtil.Cylinder(0.06f, 0.43f, 10), "Pants", new Vector3(0f, -0.43f, 0f), Vector3.one);
        ankle = Joint("Ankle" + side, knee, new Vector3(0f, -0.43f, 0f));
        Piece("Shoe", ankle, MeshUtil.Box(new Vector3(0.11f, 0.08f, 0.26f)), "Shoe", new Vector3(0f, -0.04f, 0.06f), Vector3.one);
        Piece("Toe", ankle, MeshUtil.Sphere(0.055f, 6, 8), "Shoe", new Vector3(0f, -0.045f, 0.18f), new Vector3(1f, 0.7f, 1f));
        return hip;
    }

    private static Transform Joint(string name, Transform parent, Vector3 localPos)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        return g.transform;
    }

    private static Renderer Piece(string name, Transform parent, Mesh mesh, string mat, Vector3 localPos, Vector3 scale)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        g.transform.localScale = scale;
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer r = g.AddComponent<MeshRenderer>();
        r.sharedMaterial = MaterialLibrary.Get(mat);
        return r;
    }

    // ===================== Animación =====================

    /// Gesto de señalar con el brazo derecho (al reportar o usar una puerta).
    public void Point()
    {
        pointTimer = 0f;
    }

    private void LateUpdate()
    {
        if (controller == null) return;
        float dt = Time.deltaTime;
        float t = Time.time;

        // Cabeza oculta en primera persona (si no, taparía la cámara)
        if (controller.ThirdPerson != lastThirdPerson)
        {
            lastThirdPerson = controller.ThirdPerson;
            for (int i = 0; i < headParts.Length; i++) headParts[i].enabled = lastThirdPerson;
        }

        float target = controller.Speed < 0.1f ? 0f : controller.Speed / GameSettings.WalkSpeed;
        move = Mathf.MoveTowards(move, Mathf.Min(target, 1.6f), dt * 4f);
        float walk = Mathf.Clamp01(move);
        float run = Mathf.Clamp01(move - 1f) / 0.6f;
        float phase = controller.WalkPhase;
        float sinP = Mathf.Sin(phase), cosP = Mathf.Cos(phase);

        float legAmp = Mathf.Lerp(0f, 28f, walk) + 14f * run;
        float armAmp = Mathf.Lerp(0f, 22f, walk) + 20f * run;
        float breathe = Mathf.Sin(t * 1.6f);

        // Cuerpo: rebote al andar, inclinación al correr, respiración en reposo
        body.localPosition = new Vector3(0f, Mathf.Abs(sinP) * 0.035f * walk - 0.015f * walk, 0f);
        hips.localRotation = Quaternion.Euler(0f, sinP * 6f * walk, 0f);
        spine.localRotation = Quaternion.Euler(4f * run + breathe * 1.2f * (1f - walk), -sinP * 9f * walk, 0f);
        neck.localRotation = Quaternion.Euler(Mathf.Clamp(controller.Pitch * 0.45f, -25f, 30f) - 4f * run, sinP * 4f * walk, 0f);
        head.localRotation = Quaternion.Euler(Mathf.Clamp(controller.Pitch * 0.35f, -20f, 25f), 0f, 0f);

        // Piernas: negativo en X = hacia delante. La rodilla se dobla al levantar el pie.
        hipL.localRotation = Quaternion.Euler(-sinP * legAmp, 0f, 0f);
        hipR.localRotation = Quaternion.Euler(sinP * legAmp, 0f, 0f);
        kneeL.localRotation = Quaternion.Euler(Mathf.Max(0f, cosP) * legAmp * 1.6f + 2f, 0f, 0f);
        kneeR.localRotation = Quaternion.Euler(Mathf.Max(0f, -cosP) * legAmp * 1.6f + 2f, 0f, 0f);
        ankleL.localRotation = Quaternion.Euler(-Mathf.Max(0f, cosP) * legAmp * 0.4f, 0f, 0f);
        ankleR.localRotation = Quaternion.Euler(-Mathf.Max(0f, -cosP) * legAmp * 0.4f, 0f, 0f);

        // Brazos: al contrario que las piernas; codos algo doblados (más al correr)
        float rest = 3f * breathe * (1f - walk);
        shoulderL.localRotation = Quaternion.Euler(sinP * armAmp + rest, 0f, -7f);
        float elbowBend = -12f - 8f * walk - 55f * run;
        elbowL.localRotation = Quaternion.Euler(elbowBend - Mathf.Max(0f, -sinP) * 15f * walk, 0f, 0f);

        Quaternion rShoulder = Quaternion.Euler(-sinP * armAmp + rest, 0f, 7f);
        Quaternion rElbow = Quaternion.Euler(elbowBend - Mathf.Max(0f, sinP) * 15f * walk, 0f, 0f);

        // Gesto de señalar: el brazo derecho sube y apunta hacia donde miras
        if (pointTimer >= 0f)
        {
            pointTimer += dt;
            float k = pointTimer / PointDuration;
            float w = k < 0.25f ? k / 0.25f : k > 0.7f ? Mathf.Clamp01((1f - k) / 0.3f) : 1f;
            w = Mathf.SmoothStep(0f, 1f, w);
            Quaternion aim = Quaternion.Euler(-85f + Mathf.Clamp(controller.Pitch, -40f, 40f), -12f, 4f);
            rShoulder = Quaternion.Slerp(rShoulder, aim, w);
            rElbow = Quaternion.Slerp(rElbow, Quaternion.Euler(-5f, 0f, 0f), w);
            if (k >= 1f) pointTimer = -1f;
        }
        shoulderR.localRotation = rShoulder;
        elbowR.localRotation = rElbow;
    }
}
