using UnityEngine;

/// Si pulsas Play en cualquier escena que no tenga GameManager, lo crea automáticamente.
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureGame()
    {
        if (Object.FindFirstObjectByType<GameManager>() != null) return;
        new GameObject("Game").AddComponent<GameManager>();
    }
}
