using UnityEngine;

/// Jugador: WASD/flechas + ratón con CharacterController. Shift para ir más rápido.
/// Cámara en primera persona (por defecto) o en tercera persona detrás del hombro (tecla V).
/// Expone velocidad y fase de paso para que PlayerAvatar anime el cuerpo; los pasos suenan sincronizados.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, IAvatarDriver
{
    public Transform cameraTransform;
    public float eyeHeight = 1.62f;
    public float gravity = -20f;
    public bool controlEnabled;
    public bool thirdPerson;

    public bool ThirdPerson { get { return thirdPerson; } }
    /// Se lanza en cada paso (lo usa el eco de pasos).
    public event System.Action Stepped;

    /// Velocidad horizontal real (m/s).
    public float Speed { get; private set; }
    /// Fase del ciclo de paso (radianes). Cada medio ciclo es un paso.
    public float WalkPhase { get; private set; }
    /// Inclinación vertical de la mirada (grados, positivo = abajo).
    public float Pitch { get { return pitch; } }
    /// Distancia extra de la cámara en tercera persona (para el alcance de la E).
    public float CameraExtraReach { get; private set; }

    private CharacterController controller;
    private AudioSource stepSource;
    private float pitch;
    private float verticalVelocity;
    private int lastStep = -1;
    private int lastHalfCycle;
    private float camDistance = 2.1f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        stepSource = gameObject.AddComponent<AudioSource>();
        stepSource.playOnAwake = false;
        stepSource.spatialBlend = 0f;
    }

    public void TeleportTo(Vector3 position, float yaw)
    {
        controller.enabled = false;
        transform.position = position;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        controller.enabled = true;
        pitch = 0f;
        verticalVelocity = 0f;
        WalkPhase = 0f;
        lastHalfCycle = 0;
        Speed = 0f;
    }

    private void Update()
    {
        if (!controlEnabled || Time.timeScale <= 0f) { Speed = 0f; return; }

        if (GameInput.ViewTogglePressed)
        {
            thirdPerson = !thirdPerson;
            Options.ThirdPerson = thirdPerson;
        }

        // Mirar
        Vector2 look = GameInput.Look * Options.Sensitivity;
        if (Options.InvertY) look.y = -look.y;
        transform.Rotate(0f, look.x, 0f);
        pitch = Mathf.Clamp(pitch - look.y, -75f, 75f);

        // Moverse
        Vector2 input = GameInput.Move;
        bool running = GameInput.RunHeld && input.y > 0f;
        float speed = running ? GameSettings.RunSpeed : GameSettings.WalkSpeed;
        if (input.y < 0f) speed *= 0.7f;                         // hacia atrás, más despacio
        Vector3 dir = transform.right * input.x + transform.forward * input.y;
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        Vector3 before = transform.position;
        controller.Move((dir * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
        Vector3 delta = transform.position - before; delta.y = 0f;
        Speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;

        // Ciclo de paso: avanza con la velocidad real (si chocas con una pared, no "patina")
        if (Speed > 0.1f && controller.isGrounded)
        {
            WalkPhase += Time.deltaTime * Speed * 2.9f;
            int half = Mathf.FloorToInt(WalkPhase / Mathf.PI);
            if (half != lastHalfCycle)
            {
                lastHalfCycle = half;
                PlayStep(running);
                if (Stepped != null) Stepped();
            }
        }
        else
        {
            // Al pararse, las piernas vuelven a la posición de reposo
            float target = Mathf.Round(WalkPhase / Mathf.PI) * Mathf.PI;
            WalkPhase = Mathf.MoveTowards(WalkPhase, target, Time.deltaTime * 6f);
            lastHalfCycle = Mathf.FloorToInt(WalkPhase / Mathf.PI);
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;
        float bob = Mathf.Sin(WalkPhase * 2f) * 0.025f * Mathf.Clamp01(Speed / GameSettings.WalkSpeed);

        if (!thirdPerson)
        {
            // Primera persona: ojos un poco por delante de la cara para no ver el interior de la cabeza
            cameraTransform.localPosition = new Vector3(0f, eyeHeight + bob, 0.13f);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, Mathf.Sin(WalkPhase) * 0.5f * Mathf.Clamp01(Speed / GameSettings.WalkSpeed));
            CameraExtraReach = 0f;
            return;
        }

        // Tercera persona: detrás del hombro derecho, sin atravesar paredes
        Vector3 pivot = transform.position + Vector3.up * 1.55f;
        Quaternion rot = transform.rotation * Quaternion.Euler(pitch, 0f, 0f);
        Vector3 desired = pivot + rot * new Vector3(0.42f, 0.18f, -2.1f);
        Vector3 d = desired - pivot;
        float want = d.magnitude;
        Vector3 dirN = d / want;
        RaycastHit hit;
        float dist = want;
        if (Physics.SphereCast(pivot, 0.18f, dirN, out hit, want, ViewUtil.RayMask, QueryTriggerInteraction.Ignore))
            dist = Mathf.Max(0.25f, hit.distance - 0.05f);
        camDistance = dist < camDistance ? dist : Mathf.MoveTowards(camDistance, dist, Time.deltaTime * 3f);
        cameraTransform.position = pivot + dirN * camDistance;
        cameraTransform.rotation = rot;
        CameraExtraReach = camDistance;
    }

    private void PlayStep(bool running)
    {
        int i = Random.Range(1, 5);
        if (i == lastStep) i = i % 4 + 1;
        lastStep = i;
        AudioClip c = GameAssets.Clip("step_" + i);
        if (c == null) return;
        stepSource.pitch = Random.Range(0.9f, 1.1f);
        stepSource.PlayOneShot(c, running ? 0.6f : 0.42f);
    }
}
