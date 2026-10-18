/// Valores ajustables del juego. Cambia aquí tiempos y límites sin tocar el resto del código.
public static class GameSettings
{
    public const int MaxMistakes = 3;       // errores permitidos antes de perder
    public const int TotalAnomalies = 3;    // anomalías que hay que reportar para abrir la puerta

    // Segundos de partida antes de que cada anomalía pueda ocurrir.
    public const float PaintingAfter = 35f;
    public const float LightAfter = 90f;
    public const float ChairAfter = 150f;

    public const float ChairWatchSeconds = 1.5f;   // tiempo mirando la silla antes de que pueda desaparecer
    public const float ChairFallbackExtra = 45f;   // si nunca la miras, desaparece igualmente (sin que la veas)

    public const float ReportReach = 5f;           // distancia máxima para pulsar E
    public const float ReportCooldown = 0.4f;      // evita contar dos veces una misma pulsación
}
