using UnityEngine;

/// Reloj de pared. Empieza a las 03:00 y avanza con el tiempo de juego; su tic-tac suena en 3D (clock_loop).
/// Si "reverse" está activo (anomalía), las agujas giran hacia atrás muy deprisa.
public class WallClock : MonoBehaviour
{
    public Transform hourHand, minuteHand, secondHand;
    public AudioSource tickSource;
    public bool reverse;
    public float reverseSpeed = 300f;   // las agujas giran hacia atrás a la vista

    private double seconds = 3 * 3600;     // 03:00:00

    private void Update()
    {
        seconds += reverse ? -Time.deltaTime * reverseSpeed : Time.deltaTime;
        if (seconds < 0) seconds += 12 * 3600;

        int whole = (int)seconds;
        // Normal: la aguja de segundos va a saltos. Al revés: movimiento continuo.
        double shown = reverse ? seconds : whole;
        float s = (float)(shown % 60.0);
        float m = (float)((shown / 60.0) % 60.0);
        float h = (float)((shown / 3600.0) % 12.0);

        // Rotación negativa en Z = sentido horario visto de frente.
        if (secondHand != null) secondHand.localRotation = Quaternion.Euler(0, 0, -s * 6f);
        if (minuteHand != null) minuteHand.localRotation = Quaternion.Euler(0, 0, -m * 6f);
        if (hourHand != null) hourHand.localRotation = Quaternion.Euler(0, 0, -h * 30f);

        // Sonido: el audio del reloj en bucle (3D). Al revés, se reproduce hacia atrás.
        if (tickSource != null && Time.timeScale > 0f)
        {
            if (tickSource.clip == null)
            {
                tickSource.clip = GameAssets.Clip("clock_loop");
                tickSource.loop = true;
            }
            // El audio clock_loop.wav ya viene al 20 % de su volumen original.
            tickSource.volume = 1f;
            if (tickSource.clip != null && !tickSource.isPlaying) tickSource.Play();
            tickSource.pitch = reverse ? -1.25f : 1f;
        }
    }
}
