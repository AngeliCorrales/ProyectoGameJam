using UnityEngine;

/// Si pulsas Play en una escena que no tiene GameManager (por ejemplo la SampleScene de la plantilla),
/// lo crea automáticamente. La escena principal es Assets/Scenes/Main.unity.
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureGame()
    {
        if (Object.FindFirstObjectByType<GameManager>() != null) return;
        new GameObject("Game").AddComponent<GameManager>();
    }
}
