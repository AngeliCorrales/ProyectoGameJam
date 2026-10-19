using UnityEngine;

/// Personaje quieto (Mario): solo respira; FaceCamera lo gira hacia ti.
public class IdleDriver : MonoBehaviour, IAvatarDriver
{
    public bool ThirdPerson { get { return true; } }
    public float Speed { get { return 0f; } }
    public float WalkPhase { get { return 0f; } }
    public float Pitch { get { return 0f; } }
}
