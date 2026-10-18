using UnityEngine;

public enum GameState { Menu, Playing, Paused, Won, Lost }

/// Controla el flujo completo: menú -> partida -> pausa -> victoria / derrota -> reinicio.
/// Toda la interfaz usa IMGUI (OnGUI): no necesita Canvas, EventSystem ni fuentes.
public class GameManager : MonoBehaviour
{
    private Camera cam;
    private PlayerController player;
    private AnomalyReporter reporter;
    private AudioSource ambient;
    private AudioSource sfx;
    private GameObject world;
    private LevelRefs level;

    private GameState state = GameState.Menu;
    private int mistakes;
    private int reportedCount;
    private float playTime;
    private string feedback = "";
    private float feedbackUntil;
    private float flash;

    // Estilos de interfaz
    private GUIStyle styleTitle, styleSub, styleHud, styleCenter, styleSmall, styleButton;
    private Texture2D texBtn, texBtnHover, texBtnActive;

    // ===================== Ciclo de vida =====================

    private void Awake()
    {
        DisableForeignSceneObjects();
        BuildPlayer();
        SetupAudio();
        SetupRender();
        ReturnToMenu();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && state == GameState.Playing) Pause();
    }

    private void Update()
    {
        if (flash > 0f) flash = Mathf.Max(0f, flash - Time.unscaledDeltaTime * 1.5f);

        switch (state)
        {
            case GameState.Playing:
                playTime += Time.deltaTime;
                if (GameInput.PausePressed) Pause();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (GameInput.DebugPressed && level != null)
                    foreach (Anomaly a in level.Anomalies) a.ForceActivate();
#endif
                break;
            case GameState.Paused:
                if (GameInput.PausePressed) Resume();
                else if (GameInput.RestartPressed) StartGame();
                break;
            case GameState.Won:
            case GameState.Lost:
                if (GameInput.RestartPressed) StartGame();
                break;
        }
    }

    // ===================== Preparación =====================

    private void DisableForeignSceneObjects()
    {
        // Cámaras, listeners y sol de la escena de plantilla: se desactivan para que no estorben.
        foreach (Camera c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) c.gameObject.SetActive(false);
        foreach (AudioListener a in FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) a.gameObject.SetActive(false);
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) l.gameObject.SetActive(false);
    }

    private void BuildPlayer()
    {
        GameObject p = new GameObject("Player");
        p.layer = 2; // Ignore Raycast: los rayos de juego no chocan con el jugador
        CharacterController cc = p.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        player = p.AddComponent<PlayerController>();

        GameObject camGo = new GameObject("PlayerCamera");
        camGo.tag = "MainCamera";
        camGo.layer = 2;
        camGo.transform.SetParent(p.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 70f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 80f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGo.AddComponent<AudioListener>();
        player.cameraTransform = camGo.transform;

        reporter = p.AddComponent<AnomalyReporter>();
        reporter.playerCamera = cam;
        reporter.CorrectReport += OnCorrectReport;
        reporter.WrongReport += OnWrongReport;
        reporter.Notice += msg => SetFeedback(msg, 3f);
        reporter.DoorOpened += OnDoorOpened;
    }

    private void SetupAudio()
    {
        ambient = gameObject.AddComponent<AudioSource>();
        ambient.clip = ProceduralAudio.Hum;
        ambient.loop = true;
        ambient.spatialBlend = 0f;
        ambient.volume = 0.35f;
        ambient.Play();

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.spatialBlend = 0f;
        sfx.playOnAwake = false;
    }

    private void SetupRender()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.16f, 0.16f, 0.18f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.03f, 0.03f, 0.035f);
        RenderSettings.fogStartDistance = 8f;
        RenderSettings.fogEndDistance = 40f;
    }

    // ===================== Estados =====================

    public void StartGame()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        CleanupWorld();

        world = new GameObject("World");
        level = LevelBuilder.Build(world.transform, cam);

        mistakes = 0;
        reportedCount = 0;
        playTime = 0f;
        flash = 0f;
        feedback = "";

        player.TeleportTo(new Vector3(0f, 0.05f, 1.5f), 0f);
        player.controlEnabled = true;
        reporter.InputEnabled = true;

        state = GameState.Playing;
        SetCursorLocked(true);
        SetFeedback("Memoriza el pasillo. Algo va a cambiar...", 6f);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        CleanupWorld();
        player.controlEnabled = false;
        reporter.InputEnabled = false;
        state = GameState.Menu;
        SetCursorLocked(false);
    }

    private void Pause()
    {
        state = GameState.Paused;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        player.controlEnabled = false;
        reporter.InputEnabled = false;
        SetCursorLocked(false);
    }

    private void Resume()
    {
        state = GameState.Playing;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        player.controlEnabled = true;
        reporter.InputEnabled = true;
        SetCursorLocked(true);
    }

    private void Win()
    {
        state = GameState.Won;
        player.controlEnabled = false;
        reporter.InputEnabled = false;
        SetCursorLocked(false);
        sfx.PlayOneShot(ProceduralAudio.Creak, 0.8f);
    }

    private void Lose()
    {
        state = GameState.Lost;
        player.controlEnabled = false;
        reporter.InputEnabled = false;
        Time.timeScale = 0f;
        SetCursorLocked(false);
        sfx.PlayOneShot(ProceduralAudio.Lose, 0.9f);
    }

    private void CleanupWorld()
    {
        if (world != null) Destroy(world);
        world = null;
        level = null;
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void SetFeedback(string text, float seconds)
    {
        feedback = text;
        feedbackUntil = Time.unscaledTime + seconds;
    }

    // ===================== Eventos del reporter =====================

    private void OnCorrectReport(Anomaly a)
    {
        if (state != GameState.Playing) return;
        reportedCount++;
        sfx.PlayOneShot(ProceduralAudio.Chime, 0.7f);
        SetFeedback("Anomalía reportada: " + a.AnomalyName + "  (" + reportedCount + "/" + GameSettings.TotalAnomalies + ")", 4f);

        if (reportedCount >= GameSettings.TotalAnomalies && level != null && level.Door != null)
        {
            level.Door.Unlocked = true;
            SetFeedback("Lo has visto todo. La puerta del fondo está desbloqueada.", 6f);
        }
    }

    private void OnWrongReport(string message)
    {
        if (state != GameState.Playing) return;
        mistakes++;
        flash = 0.45f;
        sfx.PlayOneShot(ProceduralAudio.Buzz, 0.8f);
        SetFeedback(message + "  Error " + mistakes + "/" + GameSettings.MaxMistakes, 3f);
        if (mistakes >= GameSettings.MaxMistakes) Lose();
    }

    private void OnDoorOpened()
    {
        if (state == GameState.Playing) Win();
    }

    // ===================== Interfaz (IMGUI) =====================

    private void OnGUI()
    {
        EnsureStyles();
        float s = Mathf.Max(0.5f, Screen.height / 1080f);
        ApplySizes(s);

        switch (state)
        {
            case GameState.Menu: DrawMenu(s); break;
            case GameState.Playing: DrawHud(s); break;
            case GameState.Paused: DrawHud(s); DrawPause(s); break;
            case GameState.Won: DrawHud(s); DrawWin(s); break;
            case GameState.Lost: DrawHud(s); DrawLose(s); break;
        }

        if (flash > 0f && state == GameState.Playing)
        {
            Fill(new Color(0.8f, 0f, 0f, flash));
        }
    }

    private void DrawMenu(float s)
    {
        Fill(Color.black);
        styleTitle.normal.textColor = new Color(0.75f, 0.1f, 0.1f);
        Label(new Rect(0, 170 * s, Screen.width, 120 * s), "NO MIRES DOS VECES", styleTitle);
        Label(new Rect(0, 280 * s, Screen.width, 60 * s), "Something's Off", styleSub);

        float bw = 380 * s, bh = 70 * s, x = (Screen.width - bw) / 2f;
        if (GUI.Button(new Rect(x, 450 * s, bw, bh), "JUGAR", styleButton)) StartGame();
        if (GUI.Button(new Rect(x, 540 * s, bw, bh), "SALIR", styleButton)) QuitGame();

        Label(new Rect(0, 700 * s, Screen.width, 120 * s),
              "Observa el pasillo. Si algo cambia, repórtalo.\nPero no reportes lo que es normal: solo tienes " +
              GameSettings.MaxMistakes + " errores.", styleSmall);
        Label(new Rect(0, Screen.height - 70 * s, Screen.width, 50 * s),
              "WASD moverse   ·   Ratón mirar   ·   E reportar / abrir   ·   Esc pausa", styleSmall);
    }

    private void DrawHud(float s)
    {
        Label(new Rect(24 * s, 20 * s, 600 * s, 40 * s),
              "ERRORES  " + mistakes + " / " + GameSettings.MaxMistakes, styleHud);
        Label(new Rect(24 * s, 62 * s, 600 * s, 40 * s),
              "ANOMALÍAS  " + reportedCount + " / " + GameSettings.TotalAnomalies, styleHud);

        string objective = reportedCount >= GameSettings.TotalAnomalies
            ? "Sal por la puerta del fondo del pasillo (E)."
            : "Objetivo: encuentra y reporta las anomalías (E) para abrir la puerta.";
        Label(new Rect(0, 20 * s, Screen.width, 40 * s), objective, styleCenter);

        float cx = Screen.width / 2f, cy = Screen.height / 2f;
        if (state == GameState.Playing)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.7f);
            GUI.DrawTexture(new Rect(cx - 2 * s, cy - 2 * s, 4 * s, 4 * s), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (!string.IsNullOrEmpty(reporter.HintText))
                Label(new Rect(0, cy + 30 * s, Screen.width, 40 * s), reporter.HintText, styleCenter);
        }

        if (Time.unscaledTime < feedbackUntil && !string.IsNullOrEmpty(feedback))
            Label(new Rect(0, Screen.height - 150 * s, Screen.width, 50 * s), feedback, styleCenter);
    }

    private void DrawPause(float s)
    {
        Fill(new Color(0f, 0f, 0f, 0.65f));
        styleTitle.normal.textColor = Color.white;
        Label(new Rect(0, 230 * s, Screen.width, 120 * s), "PAUSA", styleTitle);

        float bw = 380 * s, bh = 70 * s, x = (Screen.width - bw) / 2f;
        if (GUI.Button(new Rect(x, 400 * s, bw, bh), "CONTINUAR", styleButton)) Resume();
        if (GUI.Button(new Rect(x, 490 * s, bw, bh), "REINICIAR", styleButton)) StartGame();
        if (GUI.Button(new Rect(x, 580 * s, bw, bh), "MENÚ PRINCIPAL", styleButton)) ReturnToMenu();
        Label(new Rect(0, 690 * s, Screen.width, 40 * s), "Esc: continuar   ·   R: reiniciar", styleSmall);
    }

    private void DrawWin(float s)
    {
        Fill(new Color(0f, 0f, 0f, 0.55f));
        styleTitle.normal.textColor = new Color(0.85f, 0.95f, 0.85f);
        Label(new Rect(0, 220 * s, Screen.width, 120 * s), "HAS ESCAPADO", styleTitle);
        int minutes = (int)(playTime / 60f), seconds = (int)(playTime % 60f);
        Label(new Rect(0, 340 * s, Screen.width, 120 * s),
              "Viste lo que estaba fuera de lugar.\nTiempo: " + minutes.ToString("00") + ":" + seconds.ToString("00") +
              "     Errores: " + mistakes + " / " + GameSettings.MaxMistakes, styleSub);

        float bw = 420 * s, bh = 70 * s, x = (Screen.width - bw) / 2f;
        if (GUI.Button(new Rect(x, 520 * s, bw, bh), "JUGAR DE NUEVO", styleButton)) StartGame();
        if (GUI.Button(new Rect(x, 610 * s, bw, bh), "MENÚ PRINCIPAL", styleButton)) ReturnToMenu();
    }

    private void DrawLose(float s)
    {
        Fill(new Color(0.15f, 0f, 0f, 0.75f));
        styleTitle.normal.textColor = new Color(0.9f, 0.15f, 0.15f);
        Label(new Rect(0, 220 * s, Screen.width, 120 * s), "DEMASIADOS ERRORES", styleTitle);
        Label(new Rect(0, 340 * s, Screen.width, 120 * s),
              "Algo sigue mal en este pasillo.\nY ahora tú formas parte de ello.", styleSub);

        float bw = 420 * s, bh = 70 * s, x = (Screen.width - bw) / 2f;
        if (GUI.Button(new Rect(x, 520 * s, bw, bh), "REINTENTAR", styleButton)) StartGame();
        if (GUI.Button(new Rect(x, 610 * s, bw, bh), "MENÚ PRINCIPAL", styleButton)) ReturnToMenu();
        Label(new Rect(0, 710 * s, Screen.width, 40 * s), "R: reintentar", styleSmall);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ----- Helpers de dibujo -----

    private void Fill(Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void Label(Rect r, string text, GUIStyle style)
    {
        Color original = style.normal.textColor;
        style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
        style.normal.textColor = original;
        GUI.Label(r, text, style);
    }

    private void EnsureStyles()
    {
        if (styleTitle != null) return;

        texBtn = MakeTex(new Color(0.12f, 0.12f, 0.12f, 0.95f));
        texBtnHover = MakeTex(new Color(0.35f, 0.08f, 0.08f, 0.98f));
        texBtnActive = MakeTex(new Color(0.55f, 0.1f, 0.1f, 1f));

        styleTitle = MakeLabel(TextAnchor.MiddleCenter, FontStyle.Bold);
        styleSub = MakeLabel(TextAnchor.MiddleCenter, FontStyle.Italic);
        styleSub.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        styleHud = MakeLabel(TextAnchor.MiddleLeft, FontStyle.Bold);
        styleCenter = MakeLabel(TextAnchor.MiddleCenter, FontStyle.Normal);
        styleSmall = MakeLabel(TextAnchor.UpperCenter, FontStyle.Normal);
        styleSmall.normal.textColor = new Color(0.75f, 0.75f, 0.75f);

        styleButton = new GUIStyle(GUI.skin.button);
        styleButton.alignment = TextAnchor.MiddleCenter;
        styleButton.fontStyle = FontStyle.Bold;
        styleButton.normal.background = texBtn;
        styleButton.hover.background = texBtnHover;
        styleButton.active.background = texBtnActive;
        styleButton.normal.textColor = Color.white;
        styleButton.hover.textColor = Color.white;
        styleButton.active.textColor = Color.white;
    }

    private static GUIStyle MakeLabel(TextAnchor anchor, FontStyle fontStyle)
    {
        GUIStyle st = new GUIStyle(GUI.skin.label);
        st.alignment = anchor;
        st.fontStyle = fontStyle;
        st.wordWrap = true;
        st.normal.textColor = Color.white;
        return st;
    }

    private static Texture2D MakeTex(Color c)
    {
        Texture2D t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    private void ApplySizes(float s)
    {
        styleTitle.fontSize = Mathf.RoundToInt(78 * s);
        styleSub.fontSize = Mathf.RoundToInt(34 * s);
        styleHud.fontSize = Mathf.RoundToInt(28 * s);
        styleCenter.fontSize = Mathf.RoundToInt(28 * s);
        styleSmall.fontSize = Mathf.RoundToInt(22 * s);
        styleButton.fontSize = Mathf.RoundToInt(30 * s);
    }
}
