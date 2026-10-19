using UnityEngine;

/// Puerta con bisagra (la del fondo y la de entrada). Si está bloqueada, muestra un mensaje.
public class ExitDoor : Interactable
{
    public bool Unlocked;
    public Collider leafCollider;
    public AudioSource audioSource;
    public float openAngle = -100f;
    public float openSeconds = 1.2f;
    public string lockedMessage = "Está cerrada.";
    public string openMessage = "";
    public string prompt = "[E] Abrir la puerta";

    public bool IsOpen { get; private set; }

    private bool opening;
    private float t;

    public override string Prompt
    {
        get { return IsOpen ? "" : Unlocked ? prompt : "[E] Puerta (cerrada)"; }
    }

    public override void Interact(GameManager game)
    {
        if (IsOpen) return;
        if (!Unlocked)
        {
            Play("door_locked", 0.9f);
            if (!string.IsNullOrEmpty(lockedMessage)) game.ShowMessage(lockedMessage, 3f);
            return;
        }
        Open();
        if (!string.IsNullOrEmpty(openMessage)) game.ShowMessage(openMessage, 3f);
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        opening = true;
        if (leafCollider != null) leafCollider.enabled = false;
        Play("door_open", 1f);
    }

    public void Unlock(bool silent = false)
    {
        if (Unlocked) return;
        Unlocked = true;
        if (!silent) Play("door_unlock", 1f);
    }

    public void Lock()
    {
        Unlocked = false;
    }

    private void Play(string clip, float vol)
    {
        AudioClip c = GameAssets.Clip(clip);
        if (audioSource != null && c != null) audioSource.PlayOneShot(c, vol);
    }

    private void Update()
    {
        if (!opening) return;
        t = Mathf.Min(1f, t + Time.deltaTime / openSeconds);
        transform.localRotation = Quaternion.Euler(0f, openAngle * Mathf.SmoothStep(0f, 1f, t), 0f);
        if (t >= 1f) opening = false;
    }
}
