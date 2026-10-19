using UnityEngine;

/// Solo para el Editor: muestra el pasillo completo en la vista Scene sin pulsar Play.
/// Lo que crea no se guarda en la escena (HideFlags.DontSave) y desaparece al pulsar Play,
/// porque en el juego el nivel lo construye GameManager en cada partida (así las anomalías se reinician).
/// El objeto que lleva este componente tiene la etiqueta "EditorOnly": no entra en la compilación.
[ExecuteAlways]
public class LevelPreview : MonoBehaviour
{
    public const string PreviewName = "__LevelPreview (solo Editor, no se guarda)";

#if UNITY_EDITOR
    private GameObject preview;

    private void OnEnable()
    {
        if (Application.isPlaying) return;
        UnityEditor.EditorApplication.delayCall += Rebuild;
    }

    private void OnDisable()
    {
        UnityEditor.EditorApplication.delayCall -= Rebuild;
        Clear();
    }

    [ContextMenu("Reconstruir vista previa")]
    public void Rebuild()
    {
        if (this == null || Application.isPlaying || !isActiveAndEnabled) return;
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
        Clear();
        preview = new GameObject(PreviewName);
        preview.hideFlags = HideFlags.DontSave;
        LevelBuilder.Build(preview.transform, null);

        // Cámara de vista previa: la ventana Game muestra el pasillo aunque no hayas pulsado Play.
        // (Al pulsar Play se borra y el juego usa la cámara del jugador.)
        GameObject camGo = new GameObject("CamaraVistaPrevia");
        camGo.transform.SetParent(preview.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 1.62f, 1.0f);
        Camera c = camGo.AddComponent<Camera>();
        c.fieldOfView = 70f;
        c.nearClipPlane = 0.05f;
        c.farClipPlane = 60f;
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = new Color(0.02f, 0.02f, 0.015f);
        foreach (Transform t in preview.GetComponentsInChildren<Transform>(true))
            t.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
        preview.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
    }

    private void Clear()
    {
        if (preview != null) DestroyImmediate(preview);
        preview = null;
        if (!Application.isPlaying) MaterialLibrary.ReleaseInstances();
        // Restos de recargas anteriores
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go != null && go.name == PreviewName && go.transform.parent == null && go.scene.IsValid())
                DestroyImmediate(go);
    }
#endif
}
