using UnityEngine;

/// Toda la interfaz con IMGUI (OnGUI): menú, cómo jugar, opciones, HUD, pausa, victoria y derrota.
/// No necesita Canvas, EventSystem ni TextMeshPro. Usa las fuentes de Resources/Fonts y las imágenes de Resources/Sprites.
public class GameUI : MonoBehaviour
{
    private enum Panel { None, HowTo, Options }

    private GameManager game;
    private Panel panel = Panel.None;

    private Font fontText, fontTitle;
    private Texture2D texVignette, texGrain, texCrosshair, texEye, texJumpscare, texTitle;
    private Texture2D texBtn, texBtnHover, texBtnActive, texPanel;
    private GUIStyle sTitle, sSub, sHud, sCenter, sSmall, sBody, sButton, sToggle;
    private float s = 1f;
    private string hoveredButton = "";
    private GameState lastState = GameState.Menu;

    private const string CreditsLine =
        "Texturas: Backrooms Textures, Poly Haven (CC0) · Sonidos: Freesound y Pixabay (ver créditos) · Fuentes: Special Elite, Creepster";

    private void Awake()
    {
        game = GetComponent<GameManager>();
        fontText = GameAssets.Font("SpecialElite-Regular");
        fontTitle = GameAssets.Font("Creepster-Regular");
        texVignette = GameAssets.Sprite("ui_vignette");
        texGrain = GameAssets.Sprite("ui_grain");
        texCrosshair = GameAssets.Sprite("ui_crosshair");
        texEye = GameAssets.Sprite("ui_eye");
        texJumpscare = GameAssets.Sprite("jumpscare");
        texTitle = GameAssets.Sprite("title_background");
    }

    // ===================== Dibujo principal =====================

    private void OnGUI()
    {
        if (game == null) return;
        EnsureStyles();
        s = Mathf.Max(0.5f, Screen.height / 1080f);
        ApplySizes();
        if (game.State != lastState) { lastState = game.State; panel = Panel.None; }

        // Ambiente visual siempre presente: viñeta y grano de película
        DrawVignette(new Color(0f, 0f, 0f, 0.85f));
        DrawGrain(0.9f);

        switch (game.State)
        {
            case GameState.Menu: DrawMenu(); break;
            case GameState.Playing: DrawHud(); break;
            case GameState.Paused: DrawHud(); DrawPause(); break;
            case GameState.Won: DrawWin(); break;
        }
    }

    // ===================== Menú principal ========    // ===================== Menú principal =====================

    private void DrawMenu()
    {
        if (texTitle != null)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.35f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texTitle, ScaleMode.ScaleAndCrop);
            GUI.color = Color.white;
        }
        Fill(new Color(0f, 0f, 0f, 0.3f));

        float flicker = Random.value < 0.03f ? 0.4f : 1f;
        sTitle.normal.textColor = new Color(0.78f, 0.08f, 0.06f, flicker);
        Label(new Rect(0, 110 * s, Screen.width, 160 * s), "NO MIRES DOS VECES", sTitle);
        Label(new Rect(0, 265 * s, Screen.width, 50 * s), "Todo parece normal. Hasta que no lo es.", sSub);

        if (panel == Panel.HowTo) { DrawHowTo(); return; }
        if (panel == Panel.Options) { DrawOptionsPanel(true); return; }

        float bw = 520 * s, bh = 62 * s, x = (Screen.width - bw) / 2f, y = 370 * s;
        if (Button(new Rect(x, y, bw, bh), "JUGAR")) game.StartGame();
        if (Button(new Rect(x, y + 76 * s, bw, bh), "CÓMO JUGAR")) panel = Panel.HowTo;
        if (Button(new Rect(x, y + 152 * s, bw, bh), "OPCIONES")) panel = Panel.Options;
        if (Button(new Rect(x, y + 228 * s, bw, bh), "SALIR")) game.QuitGame();

        Label(new Rect(0, Screen.height - 110 * s, Screen.width, 40 * s),
              "WASD moverse  ·  Ratón mirar  ·  Shift correr  ·  E / clic reportar o abrir  ·  V cámara  ·  Esc pausa", sSmall);
        Label(new Rect(40 * s, Screen.height - 60 * s, Screen.width - 80 * s, 40 * s), CreditsLine, sSmall);
    }

    private void DrawHowTo()
    {
        Rect r = PanelRect(1150, 660, 320);
        GUI.DrawTexture(r, texPanel);
        string text =
            "Eres el vigilante del turno de noche. El pasillo de oficinas se repite: al cruzar la puerta del fondo\n" +
            "vuelves a entrar en el MISMO pasillo. Cada vuelta es un cuarto.\n\n" +
            "0.  El CUARTO 0 siempre es normal: recórrelo y memorízalo bien.\n" +
            "1.  En cada cuarto mira bien. A veces algo ha cambiado (un cuadro, una silla, una luz... o algo peor).\n" +
            "2.  Si algo cambió: apúntalo y pulsa E para reportarlo. Luego sigue hacia la puerta.\n" +
            "3.  Si todo está normal: no reportes nada, simplemente cruza la puerta.\n" +
            "4.  Si sales con una anomalía sin reportar, o reportas algo normal... vuelves al CUARTO 0.\n" +
            "5.  Encadena " + GameSettings.TotalRooms + " cuartos sin fallar y llegarás a la salida.\n\n" +
            "Cada cuarto es más difícil: hasta 3 anomalías a la vez, cosas que cambian mientras estás dentro...\n" +
            "y cosas que solo quieren asustarte. Si oyes susurros, no mires atrás. O sí.\n" +
            "Y no te fíes de las reglas. Pueden cambiar.";
        GUI.Label(new Rect(r.x + 40 * s, r.y + 30 * s, r.width - 80 * s, r.height - 120 * s), text, sBody);
        if (Button(new Rect(r.center.x - 160 * s, r.yMax - 84 * s, 320 * s, 60 * s), "VOLVER")) panel = Panel.None;
    }

    private void DrawOptionsPanel(bool fromMenu)
    {
        Rect r = PanelRect(820, 520, fromMenu ? 340 : 290);
        GUI.DrawTexture(r, texPanel);
        float x = r.x + 50 * s, w = r.width - 100 * s, y = r.y + 40 * s;

        GUI.Label(new Rect(x, y, w, 40 * s), "Sensibilidad del ratón: " + Options.Sensitivity.ToString("0.0"), sBody);
        float sens = GUI.HorizontalSlider(new Rect(x, y + 48 * s, w, 24 * s), Options.Sensitivity, 0.2f, 3f);
        if (!Mathf.Approximately(sens, Options.Sensitivity)) Options.Sensitivity = sens;

        y += 100 * s;
        GUI.Label(new Rect(x, y, w, 40 * s), "Volumen: " + Mathf.RoundToInt(Options.Volume * 100f) + "%", sBody);
        float vol = GUI.HorizontalSlider(new Rect(x, y + 48 * s, w, 24 * s), Options.Volume, 0f, 1f);
        if (!Mathf.Approximately(vol, Options.Volume)) Options.Volume = vol;

        y += 100 * s;
        GUI.Label(new Rect(x, y, w, 40 * s), "Brillo: " + Mathf.RoundToInt(Options.Brightness * 100f) + "%", sBody);
        float br = GUI.HorizontalSlider(new Rect(x, y + 48 * s, w, 24 * s), Options.Brightness, 0.5f, 2.5f);
        if (!Mathf.Approximately(br, Options.Brightness)) Options.Brightness = br;

        y += 100 * s;
        bool inv = GUI.Toggle(new Rect(x, y, w, 40 * s), Options.InvertY, "  Invertir eje vertical", sToggle);
        if (inv != Options.InvertY) Options.InvertY = inv;

        if (Button(new Rect(r.center.x - 160 * s, r.yMax - 84 * s, 320 * s, 60 * s), "VOLVER"))
        {
            Options.Save();
            panel = Panel.None;
        }
    }

    // ===================== HUD =====================

    private void DrawHud()
    {
        // Contador de cuartos
        float x = 30 * s, y = 24 * s;
        if (texEye != null) GUI.DrawTexture(new Rect(x, y, 44 * s, 44 * s), texEye);
        string room = game.IsFinalRoom ? "SALIDA" : "CUARTO  " + game.Room + " / " + game.TargetRooms;
        Label(new Rect(x + 56 * s, y, 600 * s, 44 * s), room, sHud);
        y += 50 * s;
        Label(new Rect(x, y, 600 * s, 36 * s), "Vueltas al cuarto 0: " + game.Resets, sSmallLeftGrey);

        string objective = game.IsFinalRoom ? "Sal por la puerta del fondo." : "¿Algo cambió? Repórtalo (E). Si no, sigue adelante.";
        Label(new Rect(0, 26 * s, Screen.width - 30 * s, 40 * s), objective, sSmallRight);

        // Punto de mira
        float cx = Screen.width / 2f, cy = Screen.height / 2f;
        if (game.State == GameState.Playing && game.Fade < 0.5f)
        {
            AnomalyReporter rep = game.Reporter;
            if (rep != null && rep.AimingAtReportable && texEye != null)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.85f);
                GUI.DrawTexture(new Rect(cx - 18 * s, cy - 18 * s, 36 * s, 36 * s), texEye);
            }
            else if (texCrosshair != null)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.8f);
                GUI.DrawTexture(new Rect(cx - 16 * s, cy - 16 * s, 32 * s, 32 * s), texCrosshair);
            }
            GUI.color = Color.white;
            if (rep != null && !string.IsNullOrEmpty(rep.HintText))
                Label(new Rect(0, cy + 30 * s, Screen.width, 40 * s), rep.HintText, sCenter);
        }

        // Destellos de acierto / error
        if (game.ReportFlash > 0f)
        {
            Fill(new Color(1f, 1f, 1f, game.ReportFlash * 0.25f));
            DrawGrain(1f + game.ReportFlash * 8f);
        }
        if (game.ErrorFlash > 0f) Fill(new Color(0.75f, 0f, 0f, game.ErrorFlash * 0.7f));

        // Fundido entre cuartos (con el número del cuarto que viene)
        if (game.Fade > 0f)
        {
            Fill(new Color(0f, 0f, 0f, game.Fade));
            Color c = sTitle.normal.textColor;
            sTitle.normal.textColor = new Color(0.85f, 0.83f, 0.78f, game.Fade);
            string t = game.IsFinalRoom ? "SALIDA" : "CUARTO " + game.Room;
            Label(new Rect(0, Screen.height / 2f - 80 * s, Screen.width, 160 * s), t, sTitle, false);
            sTitle.normal.textColor = c;
        }

        // Susto breve (Difícil, al dejarte una anomalía)
        if (game.Jumpscare > 0f && texJumpscare != null)
        {
            float k = game.Jumpscare;
            float size = Screen.height * (1.1f + (0.6f - k) * 0.5f);
            float shake = 20f * s * k;
            Rect r = new Rect((Screen.width - size) / 2f + Random.Range(-shake, shake), (Screen.height - size) / 2f + Random.Range(-shake, shake), size, size);
            Fill(Color.black);
            GUI.DrawTexture(r, texJumpscare, ScaleMode.ScaleToFit);
            DrawGrain(4f);
        }

        // Mensajes (encima del fundido)
        float a = game.MessageAlpha;
        if (a > 0f && !string.IsNullOrEmpty(game.Message))
        {
            Rect band = new Rect(0, Screen.height - 190 * s, Screen.width, 110 * s);
            GUI.color = new Color(0f, 0f, 0f, 0.5f * a);
            GUI.DrawTexture(band, Texture2D.whiteTexture);
            Color c = sCenter.normal.textColor;
            sCenter.normal.textColor = new Color(1f, 1f, 1f, a);
            GUI.color = Color.white;
            Label(band, game.Message, sCenter);
            sCenter.normal.textColor = c;
        }

        if (game.ShowDebug)
            GUI.Label(new Rect(30 * s, 140 * s, 900 * s, 200 * s), "[F2] " + game.DebugText, sSmallLeft);
    }

    // ===================== Pausa =====================

    private void DrawPause()
    {
        Fill(new Color(0f, 0f, 0f, 0.7f));
        bool twist = game.PauseTwist;     // en el último cuarto la pausa... no quiere pausar
        sTitle.normal.textColor = twist ? new Color(0.8f, 0.1f, 0.08f) : new Color(0.85f, 0.85f, 0.8f);
        Label(new Rect(0, 150 * s, Screen.width, 150 * s), twist ? "NO PUEDES PARAR AHORA" : "PAUSA", sTitle);

        if (panel == Panel.Options) { DrawOptionsPanel(false); return; }

        float bw = 400 * s, bh = 64 * s, x = (Screen.width - bw) / 2f, y = 360 * s;
        if (Button(new Rect(x, y, bw, bh), twist ? "...VALE" : "CONTINUAR")) game.Resume();
        if (Button(new Rect(x, y + 82 * s, bw, bh), "OPCIONES")) panel = Panel.Options;
        if (Button(new Rect(x, y + 164 * s, bw, bh), twist ? "EMPEZAR DE CERO (¿seguro?)" : "REINICIAR")) game.StartGame();
        if (Button(new Rect(x, y + 246 * s, bw, bh), "MENÚ PRINCIPAL")) { panel = Panel.None; game.ReturnToMenu(); }
        Label(new Rect(0, y + 340 * s, Screen.width, 40 * s), twist ? "Estás a un cuarto. Él lo sabe." : "Esc: continuar   ·   R: reiniciar", sSmall);
    }

    // ===================== Victoria =====================

    private void DrawWin()
    {
        float t = game.StateTime;
        Fill(new Color(0.96f, 0.96f, 0.94f, Mathf.Clamp01(t / 2f)));
        if (t < 1.5f) return;

        float a = Mathf.Clamp01((t - 1.5f) / 1f);
        sTitle.normal.textColor = new Color(0.12f, 0.12f, 0.12f, a);
        Label(new Rect(0, 160 * s, Screen.width, 160 * s), "HAS SALIDO", sTitle, false);
        int minutes = (int)(game.PlayTime / 60f), seconds = (int)(game.PlayTime % 60f);
        Color prev = sSub.normal.textColor;
        sSub.normal.textColor = new Color(0.2f, 0.2f, 0.2f, a);
        Label(new Rect(0, 320 * s, Screen.width, 160 * s),
              game.TargetRooms + " cuartos  ·  Tiempo " + minutes.ToString("00") + ":" + seconds.ToString("00") +
              "  ·  Vueltas al cuarto 0: " + game.Resets +
              (t > 5f ? "\n\nTurno de mañana: 03:00. Mismo pasillo." : ""), sSub, false);
        sSub.normal.textColor = prev;

        if (t < 2.5f) return;
        float bw = 420 * s, bh = 64 * s, x = (Screen.width - bw) / 2f;
        if (Button(new Rect(x, 540 * s, bw, bh), "OTRO TURNO")) game.StartGame();
        if (Button(new Rect(x, 622 * s, bw, bh), "MENÚ PRINCIPAL")) game.ReturnToMenu();
    }

    // ===================== Ayudantes de dibujo =====================

    private Rect PanelRect(float w, float h, float y)
    {
        w *= s; h *= s;
        w = Mathf.Min(w, Screen.width - 40f);
        return new Rect((Screen.width - w) / 2f, y * s, w, h);
    }

    private bool Button(Rect r, string text)
    {
        bool hover = r.Contains(Event.current.mousePosition);
        if (Event.current.type == EventType.Repaint)
        {
            if (hover && hoveredButton != text) { hoveredButton = text; game.PlayUISound("ui_hover", 0.6f); }
            else if (!hover && hoveredButton == text) hoveredButton = "";
        }
        bool clicked = GUI.Button(r, text, sButton);
        if (clicked) game.PlayUISound("ui_click", 0.8f);
        return clicked;
    }

    private void Fill(Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void DrawVignette(Color c)
    {
        if (texVignette == null) return;
        GUI.color = c;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texVignette, ScaleMode.StretchToFill);
        GUI.color = Color.white;
    }

    private void DrawGrain(float strength)
    {
        if (texGrain == null) return;
        float tile = 256f * s;
        Rect uv = new Rect(Random.value, Random.value, Screen.width / tile, Screen.height / tile);
        GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(strength));
        GUI.DrawTextureWithTexCoords(new Rect(0, 0, Screen.width, Screen.height), texGrain, uv);
        GUI.color = Color.white;
    }

    /// Texto con sombra.
    private void Label(Rect r, string text, GUIStyle style, bool shadow = true)
    {
        if (shadow)
        {
            Color original = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.8f * original.a);
            GUI.Label(new Rect(r.x + 2 * s, r.y + 2 * s, r.width, r.height), text, style);
            style.normal.textColor = original;
        }
        GUI.Label(r, text, style);
    }

    // ===================== Estilos =====================

    private GUIStyle sSmallRight, sSmallLeft, sSmallLeftGrey;

    private void EnsureStyles()
    {
        if (sTitle != null) return;

        texBtn = MakeTex(new Color(0.08f, 0.07f, 0.06f, 0.88f));
        texBtnHover = MakeTex(new Color(0.42f, 0.06f, 0.05f, 0.95f));
        texBtnActive = MakeTex(new Color(0.6f, 0.08f, 0.06f, 1f));
        texPanel = MakeTex(new Color(0.04f, 0.035f, 0.03f, 0.92f));

        sTitle = MakeLabel(TextAnchor.MiddleCenter, fontTitle != null ? fontTitle : fontText);
        sSub = MakeLabel(TextAnchor.MiddleCenter, fontText);
        sSub.normal.textColor = new Color(0.82f, 0.8f, 0.74f);
        sHud = MakeLabel(TextAnchor.MiddleLeft, fontText);
        sHud.normal.textColor = new Color(0.92f, 0.9f, 0.84f);
        sCenter = MakeLabel(TextAnchor.MiddleCenter, fontText);
        sSmall = MakeLabel(TextAnchor.MiddleCenter, fontText);
        sSmall.normal.textColor = new Color(0.7f, 0.68f, 0.62f);
        sSmallRight = MakeLabel(TextAnchor.MiddleRight, fontText);
        sSmallRight.normal.textColor = new Color(0.8f, 0.78f, 0.7f);
        sSmallLeft = MakeLabel(TextAnchor.UpperLeft, fontText);
        sSmallLeft.normal.textColor = new Color(1f, 0.85f, 0.4f);
        sSmallLeftGrey = MakeLabel(TextAnchor.MiddleLeft, fontText);
        sSmallLeftGrey.normal.textColor = new Color(0.75f, 0.73f, 0.66f);
        sBody = MakeLabel(TextAnchor.UpperLeft, fontText);
        sBody.normal.textColor = new Color(0.88f, 0.86f, 0.8f);

        sButton = new GUIStyle(GUI.skin.button);
        if (fontText != null) sButton.font = fontText;
        sButton.alignment = TextAnchor.MiddleCenter;
        sButton.normal.background = texBtn;
        sButton.hover.background = texBtnHover;
        sButton.active.background = texBtnActive;
        sButton.focused.background = texBtn;
        sButton.normal.textColor = new Color(0.9f, 0.88f, 0.82f);
        sButton.hover.textColor = Color.white;
        sButton.active.textColor = Color.white;
        sButton.focused.textColor = new Color(0.9f, 0.88f, 0.82f);
        sButton.border = new RectOffset(0, 0, 0, 0);

        sToggle = new GUIStyle(GUI.skin.toggle);
        if (fontText != null) sToggle.font = fontText;
        sToggle.normal.textColor = sToggle.onNormal.textColor = new Color(0.88f, 0.86f, 0.8f);
        sToggle.hover.textColor = sToggle.onHover.textColor = Color.white;
    }

    private static GUIStyle MakeLabel(TextAnchor anchor, Font font)
    {
        GUIStyle st = new GUIStyle(GUI.skin.label);
        if (font != null) st.font = font;
        st.alignment = anchor;
        st.wordWrap = true;
        st.richText = false;
        st.normal.textColor = Color.white;
        return st;
    }

    private static Texture2D MakeTex(Color c)
    {
        Texture2D t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        t.hideFlags = HideFlags.DontSave;
        return t;
    }

    private void ApplySizes()
    {
        sTitle.fontSize = Mathf.RoundToInt(112 * s);
        sSub.fontSize = Mathf.RoundToInt(34 * s);
        sHud.fontSize = Mathf.RoundToInt(28 * s);
        sCenter.fontSize = Mathf.RoundToInt(28 * s);
        sSmall.fontSize = Mathf.RoundToInt(21 * s);
        sSmallRight.fontSize = Mathf.RoundToInt(22 * s);
        sSmallLeft.fontSize = Mathf.RoundToInt(22 * s);
        sSmallLeftGrey.fontSize = Mathf.RoundToInt(21 * s);
        sBody.fontSize = Mathf.RoundToInt(25 * s);
        sButton.fontSize = Mathf.RoundToInt(30 * s);
        sToggle.fontSize = Mathf.RoundToInt(25 * s);
    }
}
