using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Menú "No Mires Dos Veces" en la barra superior de Unity:
///   1. Crear escena y configurar proyecto
///   2. Compilar para Windows (64-bit)
public static class NoMiresDosVecesTools
{
    private const string ScenePath = "Assets/Scenes/Main.unity";
    private const string BuildDir = "Builds/Windows";
    private const string ExeName = "NoMiresDosVeces.exe";

    [MenuItem("No Mires Dos Veces/1. Crear escena y configurar proyecto")]
    public static void SetupProject()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Directory.CreateDirectory("Assets/Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Se guardan en la escena para que la niebla y la luz ambiente también existan en la compilación
        // (así Unity no elimina las variantes de shader de niebla).
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.16f, 0.16f, 0.18f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.03f, 0.03f, 0.035f);
        RenderSettings.fogStartDistance = 8f;
        RenderSettings.fogEndDistance = 40f;

        GameObject game = new GameObject("Game");
        game.AddComponent<GameManager>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        PlayerSettings.companyName = "GameJam";
        PlayerSettings.productName = "NO MIRES DOS VECES";
        PlayerSettings.runInBackground = false;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[NoMiresDosVeces] Escena creada en " + ScenePath + " y añadida a Build Settings. Pulsa Play para probar.");
        EditorUtility.DisplayDialog("Listo",
            "Escena creada: " + ScenePath + "\n\nYa puedes pulsar Play.", "OK");
    }

    [MenuItem("No Mires Dos Veces/2. Compilar para Windows (64-bit)")]
    public static void BuildWindows()
    {
        if (!File.Exists(ScenePath)) SetupProject();
        if (!File.Exists(ScenePath)) return;

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
            Debug.LogError("[NoMiresDosVeces] La compilación falló: " + report.summary.result +
                           ". Revisa la consola (errores en rojo).");
        }
    }
}
