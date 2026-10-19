using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// Configuración automática del proyecto y menú "No Mires Dos Veces" en la barra superior de Unity.
///
/// Al abrir/compilar el proyecto (automático, una vez):
///   - Crea los materiales en Assets/Resources/Materials con el shader del pipeline activo (URP o Built-in).
///   - Ajusta la importación de texturas (mapas de normales, imágenes de interfaz sin mipmaps...).
///   - Pone Assets/Scenes/Main.unity como primera escena de la compilación.
///   - La primera vez, abre la escena Main.
[InitializeOnLoad]
public static class NoMiresDosVecesTools
{
    public const string ScenePath = "Assets/Scenes/Main.unity";
    private const string MaterialsDir = "Assets/Resources/Materials";
    private const string TexturesDir = "Assets/Resources/Textures";
    private const string SpritesDir = "Assets/Resources/Sprites";
    private const string BuildDir = "Builds/Windows";
    private const string ExeName = "NoMiresDosVeces.exe";

    static NoMiresDosVecesTools()
    {
        EditorApplication.delayCall += AutoSetup;
        EditorApplication.delayCall += HideGizmos;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode) HideGizmos();
        };
    }

    // ===================== Iconos del Editor (Gizmos) =====================

    /// Quita los iconos de bombillas, altavoces y cámaras que Unity dibuja sobre la escena y la vista Game.
    /// (Solo existen en el Editor; en el juego compilado nunca aparecen.)
    [MenuItem("No Mires Dos Veces/Ocultar iconos del Editor (bombillas, altavoces)", false, 10)]
    public static void HideGizmos()
    {
        // Solo los tipos que dibujan icono (bombilla, altavoz, cámara). Una vez por sesión: sin avisos repetidos.
        if (!SessionState.GetBool("NoMiresDosVeces_IconsHidden", false))
        {
            SessionState.SetBool("NoMiresDosVeces_IconsHidden", true);
            System.Type[] types = { typeof(Light), typeof(AudioSource), typeof(Camera) };
            foreach (System.Type t in types)
            {
                try { GizmoUtility.SetIconEnabled(t, false); }
                catch (System.Exception) { }
            }
        }

        // Apaga el botón "Gizmos" de la ventana Game.
        try
        {
            System.Type gameView = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameView == null) return;
            FieldInfo f = gameView.GetField("m_Gizmos", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (Object w in Resources.FindObjectsOfTypeAll(gameView))
            {
                if (f != null) f.SetValue(w, false);
                ((EditorWindow)w).Repaint();
            }
        }
        catch (System.Exception) { }
    }

    // ===================== Automático =====================

    private static void AutoSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += AutoSetup;
            return;
        }

        bool changed = false;
        if (FixTextureImports()) changed = true;
        if (MaterialsNeedRebuild()) { GenerateMaterials(); changed = true; }
        if (EnsureBuildScene()) changed = true;

        string key = "NoMiresDosVeces_SceneOpened_" + Application.dataPath.GetHashCode();
        if (!EditorPrefs.GetBool(key, false) && File.Exists(ScenePath))
        {
            EditorPrefs.SetBool(key, true);
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath && !active.isDirty)
            {
                EditorSceneManager.OpenScene(ScenePath);
                Debug.Log("[NoMiresDosVeces] Escena principal abierta: " + ScenePath + ". Pulsa Play para jugar.");
            }
        }

        if (changed) Debug.Log("[NoMiresDosVeces] Proyecto configurado (materiales, texturas y escena de compilación).");
    }

    // ===================== Menú =====================

    [MenuItem("No Mires Dos Veces/1. Configurar todo (materiales, texturas, escena)", false, 1)]
    public static void SetupAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        FixTextureImports();
        GenerateMaterials();
        if (!File.Exists(ScenePath)) CreateMainScene();
        EnsureBuildScene();

        PlayerSettings.companyName = "GameJam";
        PlayerSettings.productName = "NO MIRES DOS VECES";
        PlayerSettings.runInBackground = false;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;

        EditorSceneManager.OpenScene(ScenePath);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("No Mires Dos Veces",
            "Todo listo.\n\nEscena: " + ScenePath + "\nMateriales: " + MaterialsDir + "\n\nPulsa Play para jugar.", "OK");
    }

    [MenuItem("No Mires Dos Veces/2. Abrir escena principal", false, 2)]
    public static void OpenMainScene()
    {
        if (!File.Exists(ScenePath)) CreateMainScene();
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
    }

    [MenuItem("No Mires Dos Veces/3. Compilar para Windows (64-bit)", false, 3)]
    public static void BuildWindows()
    {
        FixTextureImports();
        if (MaterialsNeedRebuild()) GenerateMaterials();
        if (!File.Exists(ScenePath)) CreateMainScene();
        EnsureBuildScene();

        Directory.CreateDirectory(BuildDir);
        string exePath = Path.Combine(BuildDir, ExeName);
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[NoMiresDosVeces] Compilación correcta: " + Path.GetFullPath(exePath));
            EditorUtility.RevealInFinder(exePath);
        }
        else
        {
            Debug.LogError("[NoMiresDosVeces] La compilación falló: " + report.summary.result + ". Revisa la consola.");
        }
    }

    [MenuItem("No Mires Dos Veces/Avanzado/Regenerar materiales", false, 20)]
    public static void MenuRegenerateMaterials()
    {
        GenerateMaterials();
        EditorUtility.DisplayDialog("No Mires Dos Veces", "Materiales regenerados en " + MaterialsDir + ".", "OK");
    }

    [MenuItem("No Mires Dos Veces/Avanzado/Recrear escena Main.unity", false, 21)]
    public static void MenuRecreateScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        CreateMainScene();
        EnsureBuildScene();
        EditorSceneManager.OpenScene(ScenePath);
    }

    // ===================== Escena =====================

    public static void CreateMainScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Se guardan en la escena para que la niebla y la luz ambiente existan también en la compilación
        // (así Unity no elimina las variantes de shader de niebla).
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.13f, 0.125f, 0.1f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.03f, 0.028f, 0.02f);
        RenderSettings.fogStartDistance = 6f;
        RenderSettings.fogEndDistance = 32f;
        RenderSettings.skybox = null;

        new GameObject("Game").AddComponent<GameManager>();

        GameObject preview = new GameObject("LevelPreview (solo Editor)");
        preview.tag = "EditorOnly";
        preview.AddComponent<LevelPreview>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
    }

    private static bool EnsureBuildScene()
    {
        if (!File.Exists(ScenePath)) return false;
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Length > 0 && scenes[0].path == ScenePath && scenes[0].enabled) return false;

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        list.Add(new EditorBuildSettingsScene(ScenePath, true));
        foreach (EditorBuildSettingsScene s in scenes)
            if (s.path != ScenePath) list.Add(new EditorBuildSettingsScene(s.path, false));
        EditorBuildSettings.scenes = list.ToArray();
        return true;
    }

    // ===================== Materiales =====================

    /// Shader "Lit" del pipeline del proyecto: URP Lit o Standard (Built-in).
    private static Shader PipelineShader()
    {
        if (GraphicsSettings.currentRenderPipeline != null)
        {
            Shader urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp != null) return urp;
            Debug.LogWarning("[NoMiresDosVeces] Pipeline no reconocido (¿HDRP?). Se usa el material por defecto. " +
                             "Recomendado: plantilla Universal 3D (URP) o 3D Built-in.");
            return MaterialLibrary.Template().shader;
        }
        return Shader.Find("Standard");
    }

    private const int MaterialsVersion = 6;   // súbelo para forzar que se regeneren los materiales

    private static string VersionKey { get { return "NoMiresDosVeces_MatVersion_" + Application.dataPath.GetHashCode(); } }

    private static bool MaterialsNeedRebuild()
    {
        if (EditorPrefs.GetInt(VersionKey, 0) != MaterialsVersion) return true;
        Material wall = AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/Wall.mat");
        if (wall == null) return true;
        Shader sh = PipelineShader();
        if (sh != null && wall.shader != sh) return true;           // se cambió de pipeline: evita el rosa
        foreach (MatDef d in MaterialLibrary.Definitions)
            if (AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/" + d.key + ".mat") == null) return true;
        return false;
    }

    public static void GenerateMaterials()
    {
        Shader sh = PipelineShader();
        if (sh == null)
        {
            Debug.LogError("[NoMiresDosVeces] No se encontró un shader válido para crear los materiales.");
            return;
        }
        Directory.CreateDirectory(MaterialsDir);
        GameAssets.ClearCache();

        foreach (MatDef d in MaterialLibrary.Definitions)
        {
            string path = MaterialsDir + "/" + d.key + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = m == null;
            if (isNew) m = new Material(sh);
            else m.shader = sh;
            m.name = d.key;
            m.shaderKeywords = new string[0];
            MaterialLibrary.Configure(m, d);
            if (isNew) AssetDatabase.CreateAsset(m, path);
            else EditorUtility.SetDirty(m);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorPrefs.SetInt(VersionKey, MaterialsVersion);
        Debug.Log("[NoMiresDosVeces] " + MaterialLibrary.Definitions.Count + " materiales creados con el shader '" + sh.name + "'.");
    }

    // ===================== Texturas =====================

    /// Reglas de importación (las usa también NoMiresDosVecesTextureImport al importar).
    public static bool ApplyTextureRules(TextureImporter ti, string path)
    {
        string file = Path.GetFileNameWithoutExtension(path);
        bool changed = false;

        if (path.Contains("/Resources/Textures/"))
        {
            TextureImporterType type = file.EndsWith("_normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (ti.textureType != type) { ti.textureType = type; changed = true; }
            if (ti.wrapMode != TextureWrapMode.Repeat) { ti.wrapMode = TextureWrapMode.Repeat; changed = true; }
            if (ti.anisoLevel < 4) { ti.anisoLevel = 4; changed = true; }
            if (!ti.mipmapEnabled) { ti.mipmapEnabled = true; changed = true; }
        }
        else if (path.Contains("/Resources/Sprites/"))
        {
            bool ui = file.StartsWith("ui_") || file == "title_background" || file == "jumpscare";
            if (ti.textureType != TextureImporterType.Default) { ti.textureType = TextureImporterType.Default; changed = true; }
            if (ti.npotScale != TextureImporterNPOTScale.None) { ti.npotScale = TextureImporterNPOTScale.None; changed = true; }
            if (ti.mipmapEnabled == ui) { ti.mipmapEnabled = !ui; changed = true; }
            TextureWrapMode wrap = file == "ui_grain" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            if (ti.wrapMode != wrap) { ti.wrapMode = wrap; changed = true; }
            if (!ti.alphaIsTransparency) { ti.alphaIsTransparency = true; changed = true; }
            if (ui && ti.textureCompression != TextureImporterCompression.Uncompressed)
            {
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }
        }
        return changed;
    }

    private static bool FixTextureImports()
    {
        bool any = false;
        string[] folders = { TexturesDir, SpritesDir };
        foreach (string folder in folders)
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                if (ApplyTextureRules(ti, path))
                {
                    ti.SaveAndReimport();
                    any = true;
                }
            }
        }
        return any;
    }
}

/// Aplica las reglas de importación a las texturas del juego en cuanto Unity las importa.
public class NoMiresDosVecesTextureImport : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Resources/Textures/") && !assetPath.Contains("/Resources/Sprites/")) return;
        TextureImporter ti = assetImporter as TextureImporter;
        if (ti != null) NoMiresDosVecesTools.ApplyTextureRules(ti, assetPath);
    }
}
