using UnityEngine;

/// Primera persona: WASD + ratón con CharacterController.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public Transform cameraTransform;
    public float walkSpeed = 3.2f;
    public float sensitivity = 1f;
    public float gravity = -20f;
    public bool controlEnabled;

    private CharacterController controller;
    private AudioSource stepSource;
    private float pitch;
    private float verticalVelocity;
    private float stepTimer;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        stepSource = gameObject.AddComponent<AudioSource>();
        stepSource.playOnAwake = false;
        stepSource.spatialBlend = 0f;
        stepSource.volume = 0.35f;
    }

    public void TeleportTo(Vector3 position, float yaw)
    {
        controller.enabled = false;
        transform.position = position;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        controller.enabled = true;
        pitch = 0f;
        verticalVelocity = 0f;
        if (cameraTransform != null) cameraTransform.localRotation = Quaternion.identity;
    }

    private void Update()
    {
        if (!controlEnabled) return;

        // Mirar
        Vector2 look = GameInput.Look * sensitivity;
        transform.Rotate(0f, look.x, 0f);
        pitch = Mathf.Clamp(pitch - look.y, -80f, 80f);
        if (cameraTransform != null) cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // Moverse
        Vector2 input = GameInput.Move;
        Vector3 dir = transform.right * input.x + transform.forward * input.y;
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        controller.Move((dir * walkSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);

        // Pasos
        if (dir.sqrMagnitude > 0.01f && controller.isGrounded)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                stepSource.PlayOneShot(ProceduralAudio.Step);
                stepTimer = 0.5f;
            }
        }
        else
        {
            stepTimer = 0.1f;
        }
    }
}
