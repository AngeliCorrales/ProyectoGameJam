using System.Collections.Generic;
using UnityEngine;

public enum GameState { Menu, Playing, Paused, Won }

/// Bucle de cuartos (estilo "Exit 8"):
///  - Cada vez que cruzas la puerta del fondo entras OTRA VEZ en el mismo pasillo (cuarto siguiente).
///  - Un cuarto puede estar normal o tener UNA anomalía. Si la ves, repórtala (E). Si no, sigue adelante.
///  - Si sales de un cuarto con una anomalía sin reportar, o reportaste algo normal, vuelves al cuarto 0.
///  - Llega al cuarto 5 (Fácil) o 9 (Difícil) y la puerta lleva a la salida.
/// Giros ("¿Espera, qué?"): Mario el compañero sabe demasiado, una vez la salida está "ATRÁS",
/// otro vigilante te imita, pasos que te siguen, la pausa que no te deja parar...
public class GameManager : MonoBehaviour
{
    // ---- Estado público (lo lee GameUI) ----
    public GameState State { get; private set; }
    public int Room { get; private set; }               // cuartos superados seguidos
    public int TargetRooms { get { return GameSettings.TotalRooms; } }
    public int Stage { get { return GameSettings.Stage(Room); } }
    public int Resets { get; private set; }
    public int Visits { get; private set; }
    public float PlayTime { get; private set; }
    public float StateTime { get { return Time.unscaledTime - stateStart; } }
    public string Message { get; private set; }
    public float MessageAlpha { get { return Mathf.Clamp01((messageUntil - Time.unscaledTime) * 2f); } }
    public float ErrorFlash { get; private set; }
    public float ReportFlash { get; private set; }
    public float Fade { get; private set; }              // 0 = nada, 1 = negro (cambio de cuarto)
    public float Jumpscare { get; private set; }         // >0 mientras se muestra el susto
    public bool IsFinalRoom { get; private set; }
    public bool IsBackRoom { get; private set; }
    public bool PauseTwist { get { return !IsFinalRoom && Room == TargetRooms - 1; } }
    public bool ShowDebug { get; private set; }
    public AnomalyReporter Reporter { get { return reporter; } }
    public string DebugText
    {
        get
        {
            if (IsBackRoom) return "Cuarto especial: la salida está ATRÁS";
            if (roomAnomalies.Count == 0) return "Cuarto normal (sin anomalía)";
            string t = "";
            foreach (Anomaly a in roomAnomalies)
            {
                string st = a.IsReported ? "reportada" : a.IsActive ? "ACTIVA" : a == delayedAnomaly ? "aparecerá pronto" : "?";
                t += "Anomalía: " + a.anomalyName + " (" + st + ")   ";
            }
            return t;
        }
    }

    private Camera cam;
    private PlayerController player;
    private PlayerAvatar avatar;
    private AnomalyReporter reporter;
    private SoundManager sound;
    private GameObject world;
    private LevelRefs level;
    private static readonly Color BaseAmbient = new Color(0.20f, 0.19f, 0.15f);

    private float stateStart, messageUntil;

    // Cuarto actual
    private readonly List<Anomaly> roomAnomalies = new List<Anomaly>();
    private Anomaly delayedAnomaly;          // la que ocurrirá estando tú dentro (o la sombra)
    private bool delayedPending;
    private AudioSource marioVoice;
    private float marioVoiceStop;
    private float delayedAt, roomTime;
    private bool roomTainted;                 // reportaste algo normal en este cuarto
    private bool echoRoom;
    private int normalStreak;
    private readonly List<string> recent = new List<string>();

    // Transición entre cuartos
    private bool transitioning;
    private float transitionT;
    private bool transitionForward;
    private string pendingMessage;

    // Mario
    private Transform mario;
    private readonly Queue<string> marioLines = new Queue<string>();
    private float marioNextLine;
    private bool marioTalking;
    private bool marioCountDone, marioReportDone, marioResetDone, marioDoubtDone;
    private string lastReported;
    private bool justReset;

    // Eco de pasos
    private readonly List<float> echoQueue = new List<float>();
    private AudioSource echoSource;

    // ===================== Ciclo de vida =====================

    private void Awake()
    {
        RemoveEditorPreview();
        DisableForeignSceneObjects();
        SetupRender();

        sound = gameObject.AddComponent<SoundManager>();
        BuildPlayer();
        gameObject.AddComponent<GameUI>();
        ShadowChaserAnomaly.Caught += OnShadowCaught;

        ReturnToMenu();
    }

    private void OnDestroy()
    {
        ShadowChaserAnomaly.Caught -= OnShadowCaught;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && State == GameState.Playing && !transitioning) Pause();
    }

    private void Update()
    {
        float udt = Time.unscaledDeltaTime;
        ErrorFlash = Mathf.Max(0f, ErrorFlash - udt * 1.6f);
        ReportFlash = Mathf.Max(0f, ReportFlash - udt * 2.5f);
        Jumpscare = Mathf.Max(0f, Jumpscare - udt);
        ApplyBrightness();

        switch (State)
        {
            case GameState.Menu:
                player.transform.rotation = Quaternion.Euler(0f, Mathf.Sin(Time.unscaledTime * 0.15f) * 8f, 0f);
                break;
            case GameState.Playing:
                UpdatePlaying();
                break;
            case GameState.Paused:
                if (GameInput.PausePressed) Resume();
                else if (GameInput.RestartPressed) StartGame();
                break;
            case GameState.Won:
                if (GameInput.RestartPressed && StateTime > 2f) StartGame();
                break;
        }
    }

    private void UpdatePlaying()
    {
        PlayTime += Time.deltaTime;

        if (transitioning) { UpdateTransition(); return; }
        Fade = Mathf.MoveTowards(Fade, 0f, Time.unscaledDeltaTime * 2f);

        if (GameInput.PausePressed) { Pause(); return; }
        roomTime += Time.deltaTime;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (GameInput.DebugListPressed) ShowDebug = !ShowDebug;
        if (GameInput.DebugActivatePressed && delayedPending && delayedAnomaly != null) { delayedAnomaly.Activate(); delayedPending = false; }
#endif

        // Difícil: anomalía que ocurre estando dentro, cuando ha pasado un rato y no la miras.
        if (delayedPending && delayedAnomaly != null && roomTime >= delayedAt && delayedAnomaly.CanActivate())
        {
            delayedAnomaly.Activate();
            delayedPending = false;
            sound.Play("anomaly_cue", 0.6f, Random.Range(0.9f, 1.05f));
        }
        // La sombra (solo susto): aparece detrás de ti al pasar cierto punto del pasillo
        if (shadowEvent != null && !shadowEvent.IsActive && !shadowEvent.IsReported && player.transform.position.z > shadowTriggerZ)
        {
            shadowEvent.Activate();
            sound.Play("static", 0.6f, 0.7f);
        }
        if (marioVoice != null && marioVoice.isPlaying && Time.unscaledTime > marioVoiceStop) marioVoice.Stop();

        UpdateMario();
        UpdateEcho();
        UpdateScares();
        // Cuanto más cerca de la salida, más se oye el latido
        sound.SetTension(IsFinalRoom ? 0f : 0.8f * Room / TargetRooms);

        float z = player.transform.position.z;
        if (IsFinalRoom)
        {
            // Mario se despide antes de que termine el turno
            bool marioDone = marioLines.Count == 0 && Time.unscaledTime > marioNextLine;
            if (z > GameSettings.WinZ && marioDone) Win();
        }
        else if (z > GameSettings.ExitZ) LeaveRoom(true);
        else if (IsBackRoom && z < GameSettings.BackZ) LeaveRoom(false);
    }

    // ===================== Preparación =====================

    private void RemoveEditorPreview()
    {
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go != null && go.name == LevelPreview.PreviewName && go.transform.parent == null && go.scene.IsValid())
                Destroy(go);
    }

    private void DisableForeignSceneObjects()
    {
        foreach (Camera c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) c.gameObject.SetActive(false);
        foreach (AudioListener a in FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) a.gameObject.SetActive(false);
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) l.gameObject.SetActive(false);
    }

    private void BuildPlayer()
    {
        GameObject p = new GameObject("Player");
        p.layer = ViewUtil.PlayerLayer;          // Ignore Raycast: los rayos de juego no chocan con el jugador
        CharacterController cc = p.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.25f;
        player = p.AddComponent<PlayerController>();
        player.Stepped += OnPlayerStep;

        GameObject camGo = new GameObject("PlayerCamera");
        camGo.tag = "MainCamera";
        camGo.layer = ViewUtil.PlayerLayer;
        camGo.transform.SetParent(p.transform, false);
        camGo.transform.localPosition = new Vector3(0f, player.eyeHeight, 0f);
        cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 70f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 60f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.015f);
        camGo.AddComponent<AudioListener>();
        player.cameraTransform = camGo.transform;

        avatar = PlayerAvatar.Create(player);

        reporter = p.AddComponent<AnomalyReporter>();
        reporter.playerCamera = cam;
        reporter.player = player;
        reporter.ReportPressed += OnReport;
        reporter.InteractPressed += OnInteract;

        // Fuente de sonido para los pasos que te siguen (eco)
        GameObject echo = new GameObject("Eco");
        echoSource = echo.AddComponent<AudioSource>();
        echoSource.playOnAwake = false;
        echoSource.spatialBlend = 1f;
        echoSource.minDistance = 1f;
        echoSource.maxDistance = 15f;
    }

    private void SetupRender()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = BaseAmbient;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.04f, 0.037f, 0.028f);
        RenderSettings.fogStartDistance = 8f;
        RenderSettings.fogEndDistance = 36f;
        RenderSettings.skybox = null;
    }

    private void ApplyBrightness()
    {
        float b = Options.Brightness;
        RenderSettings.ambientLight = BaseAmbient * b;
        if (level != null)
            for (int i = 0; i < level.Lamps.Count; i++)
                if (level.Lamps[i] != null) level.Lamps[i].intensity = level.LampBaseIntensity[i] * b;
    }

    private void BuildWorld(int roomNumber, bool finalRoom)
    {
        CleanupWorld();
        world = new GameObject("World");
        level = LevelBuilder.Build(world.transform, cam, roomNumber, finalRoom);
    }

    private void CleanupWorld()
    {
        if (world != null) Destroy(world);
        world = null;
        level = null;
        mario = null;
        MaterialLibrary.ReleaseInstances();
    }

    // ===================== Estados =====================

    public void StartGame()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        Room = 0; Resets = 0; Visits = 0; PlayTime = 0f;
        normalStreak = 0; recent.Clear();
        marioCountDone = marioReportDone = marioResetDone = marioDoubtDone = false;
        lastReported = null; justReset = false; backTwistDone = false;
        ErrorFlash = ReportFlash = Jumpscare = 0f; ShowDebug = false;
        transitioning = false;

        player.controlEnabled = true;
        player.thirdPerson = Options.ThirdPerson;
        reporter.InputEnabled = true;
        sound.SetAmbience(true);
        sound.SetTension(0f);
        SetState(GameState.Playing);
        SetCursorLocked(true);

        EnterRoom(true);
        Fade = 1f;
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        transitioning = false;
        Fade = 0f;
        BuildWorld(0, false);
        player.controlEnabled = false;
        player.thirdPerson = true;       // en el menú se ve al vigilante de espaldas
        player.TeleportTo(new Vector3(0f, 0.05f, 4.5f), 0f);
        reporter.InputEnabled = false;
        sound.SetAmbience(false);
        sound.SetTension(0f);
        SetState(GameState.Menu);
        SetCursorLocked(false);
        Message = "";
    }

    public void Pause()
    {
        if (State != GameState.Playing) return;
        SetState(GameState.Paused);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        player.controlEnabled = false;
        reporter.InputEnabled = false;
        SetCursorLocked(false);
    }

    public void Resume()
    {
        if (State != GameState.Paused) return;
        SetState(GameState.Playing);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        player.controlEnabled = !transitioning;
        reporter.InputEnabled = !transitioning;
        SetCursorLocked(true);
        Options.Save();
    }

    private void Win()
    {
        SetState(GameState.Won);
        player.controlEnabled = false;
        reporter.InputEnabled = false;
        SetCursorLocked(false);
        sound.SetAmbience(false);
        sound.PlayUI("win", 0.9f);
    }

    public void QuitGame()
    {
        Options.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetState(GameState s)
    {
        State = s;
        stateStart = Time.unscaledTime;
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    public void ShowMessage(string text, float seconds)
    {
        Message = text;
        messageUntil = Time.unscaledTime + seconds;
    }

    public void PlayUISound(string clip, float volume = 1f)
    {
        sound.PlayUI(clip, volume);
    }

    // ===================== Cuartos =====================

    private bool backTwistDone;

    private void EnterRoom(bool cameForward)
    {
        Visits++;
        IsFinalRoom = Room >= TargetRooms;
        IsBackRoom = false;
        roomAnomalies.Clear();
        delayedAnomaly = null;
        shadowEvent = null;
        scareTimes.Clear(); scareKinds.Clear();
        blackoutUntil = -1f;
        apparition = null;
        delayedPending = false;
        marioVoice = null;
        roomTainted = false;
        roomTime = 0f;
        echoRoom = false;
        echoQueue.Clear();
        marioLines.Clear();
        marioTalking = false;

        BuildWorld(Room, IsFinalRoom);
        player.TeleportTo(level.PlayerStart + new Vector3(Mathf.Clamp(player.transform.position.x, -0.5f, 0.5f), 0f, 0f), 0f);

        // Las dos puertas: la del fondo se abre (salvo giro), la de entrada no.
        level.ExitDoor.Unlock(true);
        level.ExitDoor.lockedMessage = "No es por aquí.";
        level.ExitDoor.prompt = IsFinalRoom ? "[E] Salir" : "[E] Abrir la puerta";

        if (IsFinalRoom)
        {
            SpawnMario(new Vector3(0.6f, 0f, 33.6f));
            QueueMario("Turno terminado. " + TargetRooms + " cuartos sin fallar.",
                       "Nadie lo había hecho tan rápido. Bueno... nadie lo había hecho.",
                       "Nos vemos mañana. Mismo turno. Mismo pasillo.");
            sound.Play("door_unlock", 0.7f);
        }
        else if (!backTwistDone && Room == TargetRooms / 2 && Room > 0)
        {
            // GIRO: la regla cambia sin avisar. La salida está detrás.
            backTwistDone = true;
            IsBackRoom = true;
            level.ExitDoor.Lock();
            level.StartDoor.Unlock(true);
            level.StartDoor.openMessage = "";
            Texture back = GameAssets.Sprite("exit_sign_back");
            if (level.ExitSignRenderer != null && back != null)
            {
                MaterialLibrary.SetMainTexture(level.ExitSignRenderer.sharedMaterial, back);
                MaterialLibrary.SetEmission(level.ExitSignRenderer.sharedMaterial, new Color(0.9f, 0.9f, 0.9f), back);
            }
            normalStreak++;
        }
        else
        {
            ChooseAnomaly();
            if (Random.value < GameSettings.EchoChance[Stage]) echoRoom = true;
            ScheduleScares();
        }

        if (!IsFinalRoom) ScriptMario();

        if (!string.IsNullOrEmpty(pendingMessage)) { ShowMessage(pendingMessage, 4.5f); pendingMessage = null; }
    }

    private float shadowTriggerZ;
    private ShadowChaserAnomaly shadowEvent;   // la sombra: solo asusta, no cuenta como anomalía

    private void ChooseAnomaly()
    {
        // Cuarto 0: siempre normal. Es la intro y el momento de memorizar.
        if (Room == 0) { normalStreak = 0; return; }
        int st = Stage;

        // La sombra (solo susto) puede aparecer aunque el cuarto sea normal
        if (Random.value < GameSettings.ShadowChance[st])
        {
            foreach (Anomaly a in level.Pool)
                if (a is ShadowChaserAnomaly) shadowEvent = (ShadowChaserAnomaly)a;
            shadowTriggerZ = Random.Range(9f, 17f);
        }

        bool force = normalStreak >= GameSettings.MaxNormalStreak;
        if (!force && Random.value > GameSettings.AnomalyChance[st]) { normalStreak++; return; }
        normalStreak = 0;

        int count = 1;
        float r = Random.value;
        if (r < GameSettings.ThreeChance[st]) count = 3;
        else if (r < GameSettings.ThreeChance[st] + GameSettings.TwoChance[st]) count = 2;
        if (count > GameSettings.MaxAnomaliesPerRoom) count = GameSettings.MaxAnomaliesPerRoom;

        List<Anomaly> options = new List<Anomaly>();
        foreach (Anomaly a in level.Pool)
        {
            if (a is ShadowChaserAnomaly) continue;
            if (recent.Contains(a.anomalyName)) continue;
            if (!GameSettings.SubtleAllowed[st] && IsSubtle(a.anomalyName)) continue;
            options.Add(a);
        }
        if (options.Count < count) { options.Clear(); foreach (Anomaly a in level.Pool) if (!(a is ShadowChaserAnomaly)) options.Add(a); }

        for (int n = 0; n < count && options.Count > 0; n++)
        {
            Anomaly pick = options[Random.Range(0, options.Count)];
            options.Remove(pick);
            roomAnomalies.Add(pick);
            recent.Add(pick.anomalyName);
            if (recent.Count > 8) recent.RemoveAt(0);

            if (delayedAnomaly == null && Random.value < GameSettings.DelayedChance[st] && pick.anomalyName != "Número del cuarto")
            {
                delayedAnomaly = pick;
                delayedPending = true;
                delayedAt = Random.Range(GameSettings.DelayMin, GameSettings.DelayMax);
            }
            else pick.Activate();
        }
    }

    private static bool IsSubtle(string name)
    {
        return name == "Número de puerta" || name == "Días sin incidentes" || name == "Cajón abierto" || name == "Figura en el cuadro";
    }

    private void LeaveRoom(bool forward)
    {
        Anomaly missedOne = null;
        foreach (Anomaly a in roomAnomalies) if (a.IsActive) { missedOne = a; break; }
        bool missed = missedOne != null;
        if (missed || roomTainted)
        {
            string why = missed ? "Se te pasó algo: " + missedOne.anomalyName + "." : "Reportaste algo que era normal.";
            pendingMessage = why + "  Vuelves al cuarto 0.";
            Room = 0;
            Resets++;
            justReset = true;
            if (missed && Stage >= 1) { Jumpscare = 0.6f; sound.PlayUI("jumpscare", 0.7f); }
            else sound.Play("lose", 0.5f);
        }
        else
        {
            Room++;
            if (!forward) pendingMessage = "Espera... ¿qué?  Cuarto " + Room + ".";
            else if (Room >= TargetRooms) pendingMessage = "Esta vez el pasillo huele distinto.";
        }

        transitioning = true;
        transitionT = 0f;
        transitionForward = forward;
        player.controlEnabled = false;
        reporter.InputEnabled = false;
    }

    private void UpdateTransition()
    {
        transitionT += Time.unscaledDeltaTime;
        if (Fade < 1f)
        {
            Fade = Mathf.MoveTowards(Fade, 1f, Time.unscaledDeltaTime * 3.5f);
            if (Fade >= 1f) EnterRoom(transitionForward);
            return;
        }
        if (transitionT > 0.45f)
        {
            transitioning = false;
            player.controlEnabled = true;
            reporter.InputEnabled = true;
        }
    }

    // ===================== Mario (sabe demasiado) =====================

    private void ScriptMario()
    {
        if (Visits == 1)
        {
            SpawnMario(new Vector3(1.15f, 0f, 3.3f));
            QueueMario("¡Ah, el nuevo! Soy Mario. Te explico rápido.",
                       "Este pasillo se repite. Siempre. Es el mismo cuarto una y otra vez.",
                       "Si algo cambia, apúntalo y pulsa E. Si todo está normal, sigue hasta la puerta del fondo.",
                       "Si se te escapa algo, o reportas algo normal... vuelves al cuarto 0.",
                       "Llega al cuarto " + TargetRooms + " y saldrás. Yo no cuento como anomalía. Creo.");
            return;
        }
        if (justReset && !marioResetDone)
        {
            marioResetDone = true; justReset = false;
            SpawnMario(new Vector3(-1.25f, 0f, 10.6f));
            QueueMario("Cuarto 0 otra vez. Llevas " + Resets + (Resets == 1 ? " vuelta" : " vueltas") + ".",
                       "No me mires así. Yo las cuento todas. Es lo único que hago.");
            return;
        }
        justReset = false;
        if (Room == 2 && !marioCountDone)
        {
            marioCountDone = true;
            SpawnMario(new Vector3(1.15f, 0f, 18.6f));
            QueueMario("Cuarto 2. Vas bien.", "...¿Que cómo sé en qué cuarto vas? Buena pregunta.");
            return;
        }
        if (lastReported != null && !marioReportDone && Room >= 1)
        {
            marioReportDone = true;
            SpawnMario(new Vector3(-1.25f, 0f, 10.6f));
            QueueMario("Lo de \"" + lastReported + "\" fue buen ojo.", "Yo también lo vi. Desde aquí. Aunque yo no estaba en ese cuarto.");
            return;
        }
        if (Room == TargetRooms - 2 && !marioDoubtDone)
        {
            marioDoubtDone = true;
            SpawnMario(new Vector3(1.15f, 0f, 18.6f));
            QueueMario("¿Sabes qué es lo raro?", "Que tú también te repites. Mismo uniforme. Mismos pasos. Mismo turno.", "Igual el que tendría que salir de aquí... no eres tú.");
        }
    }

    private void SpawnMario(Vector3 pos)
    {
        GameObject g = new GameObject("Mario");
        g.transform.SetParent(world.transform, false);
        g.transform.localPosition = pos;
        g.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        IdleDriver driver = g.AddComponent<IdleDriver>();
        PlayerAvatar.Create(driver, driver, AvatarStyle.Office);
        FaceCamera fc = g.AddComponent<FaceCamera>();
        fc.target = cam;
        fc.turnSpeed = 60f;
        BoxCollider solid = g.AddComponent<BoxCollider>();
        solid.center = new Vector3(0f, 0.9f, 0f);
        solid.size = new Vector3(0.5f, 1.8f, 0.4f);
        Reportable r = g.AddComponent<Reportable>();
        r.displayName = "Mario";
        BoxCollider z = g.AddComponent<BoxCollider>();
        z.isTrigger = true;
        z.center = new Vector3(0f, 0.95f, 0f);
        z.size = new Vector3(0.7f, 1.9f, 0.6f);
        r.zone = z;
        mario = g.transform;
        marioVoice = g.AddComponent<AudioSource>();
        marioVoice.playOnAwake = false;
        marioVoice.spatialBlend = 1f;
        marioVoice.rolloffMode = AudioRolloffMode.Linear;
        marioVoice.minDistance = 2f;
        marioVoice.maxDistance = 16f;
        marioVoice.clip = GameAssets.Clip("mario_voice");
    }

    private void QueueMario(params string[] lines)
    {
        marioLines.Clear();
        foreach (string l in lines) marioLines.Enqueue(l);
        marioTalking = false;
    }

    private void UpdateMario()
    {
        if (mario == null || marioLines.Count == 0) return;
        float d = Vector3.Distance(mario.position, player.transform.position);
        if (!marioTalking && d < 5.5f) { marioTalking = true; marioNextLine = 0f; }
        if (!marioTalking || Time.unscaledTime < marioNextLine) return;
        string line = marioLines.Dequeue();
        ShowMessage("MARIO:  " + line, 4.2f);
        // Voz de Mario: un trozo distinto de los murmullos en cada frase
        if (marioVoice != null && marioVoice.clip != null)
        {
            float len = marioVoice.clip.length;
            marioVoice.time = Random.Range(0f, Mathf.Max(0f, len - 2.6f));
            marioVoice.pitch = Random.Range(0.95f, 1.08f);
            marioVoice.Play();
            marioVoiceStop = Time.unscaledTime + Mathf.Min(2.6f, 0.9f + line.Length * 0.03f);
        }
        marioNextLine = Time.unscaledTime + 4.4f;
    }

    // ===================== Sustos (no cuentan como anomalías) =====================

    private readonly List<float> scareTimes = new List<float>();
    private readonly List<int> scareKinds = new List<int>();
    private float blackoutUntil = -1f;
    private readonly List<bool> lampWasOn = new List<bool>();
    private GameObject apparition;
    private float apparitionSeen, apparitionEnd;

    private void ScheduleScares()
    {
        scareTimes.Clear(); scareKinds.Clear();
        if (Room == 0 && Visits == 1) return;            // la intro con Mario, tranquila
        int st = Stage;
        if (Random.value < GameSettings.ScareChance[st]) AddScare(Random.Range(7f, 18f));
        if (Random.value < GameSettings.SecondScareChance[st]) AddScare(Random.Range(20f, 32f));
    }

    private void AddScare(float t)
    {
        scareTimes.Add(t);
        scareKinds.Add(Random.Range(0, 4));
    }

    private void UpdateScares()
    {
        for (int i = scareTimes.Count - 1; i >= 0; i--)
        {
            if (roomTime < scareTimes[i]) continue;
            int kind = scareKinds[i];
            scareTimes.RemoveAt(i); scareKinds.RemoveAt(i);
            DoScare(kind);
        }

        // Fin del apagón: cada lámpara vuelve a como estaba
        if (blackoutUntil > 0f && Time.time > blackoutUntil && level != null)
        {
            blackoutUntil = -1f;
            for (int i = 0; i < level.Lamps.Count && i < lampWasOn.Count; i++)
                if (level.Lamps[i] != null) level.Lamps[i].enabled = lampWasOn[i];
            sound.Play("light_flicker", 0.5f);
        }

        // La aparición se esfuma en cuanto la miras un instante (o a los pocos segundos)
        if (apparition != null)
        {
            Bounds b = new Bounds(apparition.transform.position + Vector3.up * 1f, new Vector3(0.7f, 2f, 0.7f));
            if (ViewUtil.IsSeen(cam, b, apparition.transform, 40f)) apparitionSeen += Time.deltaTime;
            if (apparitionSeen > 0.6f || Time.time > apparitionEnd)
            {
                if (apparitionSeen > 0.6f) sound.Play("static", 0.7f, 0.8f);
                Destroy(apparition);
                apparition = null;
            }
        }
    }

    /// Punto delante del jugador (dentro del pasillo), a "dist" metros hacia donde mira.
    private Vector3 InFront(float dist)
    {
        Vector3 f = player.transform.forward; f.y = 0f;
        if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
        Vector3 q = player.transform.position + f.normalized * dist;
        return new Vector3(Mathf.Clamp(q.x, -1.3f, 1.3f), 0f, Mathf.Clamp(q.z, 0.8f, 29.2f));
    }

    /// Silueta negra (sombra) mirándote en "pos". Desaparece al cabo de "life" segundos o al mirarla un instante.
    private void SpawnApparition(Vector3 pos, float life)
    {
        if (apparition != null) Destroy(apparition);
        GameObject g = new GameObject("Aparicion");
        g.transform.SetParent(world.transform, false);
        g.transform.position = pos;
        Vector3 to = player.transform.position - pos; to.y = 0f;
        g.transform.rotation = to.sqrMagnitude > 0.01f ? Quaternion.LookRotation(to.normalized, Vector3.up) : Quaternion.identity;
        IdleDriver d = g.AddComponent<IdleDriver>();
        PlayerAvatar.Create(d, d, AvatarStyle.Shadow);
        FaceCamera fc = g.AddComponent<FaceCamera>(); fc.target = cam; fc.turnSpeed = 360f;
        apparition = g; apparitionSeen = 0f; apparitionEnd = Time.time + life;
    }

    private void DoScare(int kind)
    {
        if (level == null) return;
        Vector3 p = player.transform.position;
        switch (kind)
        {
            case 0: // Apagón... y en la oscuridad, la sombra justo delante de ti
                lampWasOn.Clear();
                foreach (Light l in level.Lamps) { lampWasOn.Add(l != null && l.enabled); if (l != null) l.enabled = false; }
                blackoutUntil = Time.time + Random.Range(1.3f, 2.0f);
                sound.Play("light_off", 0.9f);
                sound.Play("anomaly_cue", 0.7f, 0.8f);
                SpawnApparition(InFront(2.4f), blackoutUntil - Time.time);
                break;
            case 1: // Golpes en la puerta que tienes delante
            {
                float[] doorsZ = { 4f, 8.6f, 19f, 30.1f };
                float[] doorsX = { -2f, 2f, -2f, 0f };
                float fz = player.transform.forward.z >= 0f ? 1f : -1f;
                int best = 3; float bd = 999f;
                for (int i = 0; i < doorsZ.Length; i++)
                {
                    float d = (doorsZ[i] - p.z) * fz;
                    if (d > 1f && d < bd) { bd = d; best = i; }
                }
                AudioClip c = GameAssets.Clip("door_locked");
                if (c != null)
                {
                    Vector3 at = new Vector3(doorsX[best], 1.2f, doorsZ[best]);
                    AudioSource.PlayClipAtPoint(c, at, 1f);
                    AudioSource.PlayClipAtPoint(c, at, 0.8f);
                }
                break;
            }
            case 2: // Aparición: de golpe, una sombra delante de ti, con grito
                SpawnApparition(InFront(3.2f), 0.9f);
                sound.Play("static", 0.8f, 0.7f);
                sound.Play("jumpscare", 0.55f);
                break;
            default: // Susurro delante de ti, en la oscuridad del pasillo
            {
                AudioClip c = GameAssets.Clip("anomaly_cue");
                if (c != null) AudioSource.PlayClipAtPoint(c, InFront(2f) + Vector3.up * 1.6f, 1f);
                break;
            }
        }
    }

    // ===================== Pasos que te siguen =====================

    private void OnPlayerStep()
    {
        if (echoRoom && State == GameState.Playing) echoQueue.Add(Time.time + 0.32f);
    }

    private void UpdateEcho()
    {
        for (int i = echoQueue.Count - 1; i >= 0; i--)
        {
            if (Time.time < echoQueue[i]) continue;
            echoQueue.RemoveAt(i);
            Transform pt = player.transform;
            echoSource.transform.position = pt.position - pt.forward * 3.5f + Vector3.up * 0.2f;
            AudioClip c = GameAssets.Clip("step_" + Random.Range(1, 5));
            if (c != null) { echoSource.pitch = Random.Range(0.8f, 0.9f); echoSource.PlayOneShot(c, 0.55f); }
        }
    }

    // ===================== Reportar =====================

    private void OnReport(Reportable r)
    {
        if (State != GameState.Playing || transitioning) return;
        if (avatar != null) avatar.Point();

        if (mario != null && r.transform == mario)
        {
            ShowMessage("MARIO:  ¿Me acabas de reportar? A mí. Eso cuenta como fallo, ¿sabes?", 4f);
            Wrong();
            return;
        }

        Anomaly a = r as Anomaly;
        if (a != null && a == shadowEvent)
        {
            if (a.TryReport())
            {
                // Al reportarla... te da un susto antes de desaparecer
                Jumpscare = 0.7f;
                sound.PlayUI("jumpscare", 0.9f);
                sound.Play("static", 0.7f);
                ShowMessage("...La sombra se deshace en el aire.", 3f);
            }
            return;
        }
        if (a != null && a.IsReported)
        {
            ShowMessage("Ya lo reportaste.", 2.5f);
            return;
        }

        if (a != null && roomAnomalies.Contains(a) && a.TryReport())
        {
            ReportFlash = 0.6f;
            sound.Play("report_ok", 0.8f);
            sound.Play(a.anomalyName == "Pato de goma gigante" ? "squeak" : "static", a.anomalyName == "Pato de goma gigante" ? 1f : 0.5f);
            lastReported = a.anomalyName;
            int left = 0;
            foreach (Anomaly x in roomAnomalies) if (x.IsActive) left++;
            string tail = left > 0 ? "  Aún hay algo más raro aquí..." : "  Ahora sigue adelante.";
            ShowMessage(a.anomalyName == "Pato de goma gigante" ? "Anomalía reportada: un pato. Un pato gigante. Vale." + tail :
                        "Anomalía reportada: " + a.anomalyName + "." + tail, 4f);
            return;
        }

        ShowMessage(r.displayName + ": parecía normal...  Al salir volverás al cuarto 0.", 4f);
        Wrong();
    }

    private void OnShadowCaught()
    {
        if (State != GameState.Playing || transitioning) return;
        // Solo un susto: la sombra desaparece y sigues en el mismo cuarto
        Jumpscare = 0.8f;
        sound.PlayUI("jumpscare", 0.9f);
        if (shadowEvent != null) shadowEvent.TryReport();
        ShowMessage("...Ya no está.", 3f);
    }

    private void Wrong()
    {
        roomTainted = true;
        ErrorFlash = 0.5f;
        sound.Play("report_wrong", 0.8f);
    }

    private void OnInteract(Interactable it)
    {
        if (State != GameState.Playing || it == null || transitioning) return;
        if (avatar != null) avatar.Point();
        it.Interact(this);
    }
}
