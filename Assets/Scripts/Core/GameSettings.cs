/// Valores ajustables del juego. Cambia aquí sin tocar el resto del código.
public static class GameSettings
{
    // ---- Bucle de cuartos (un solo modo que se vuelve más difícil) ----
    public const int TotalRooms = 8;               // cuartos seguidos sin fallar para salir
    public const int MaxAnomaliesPerRoom = 3;      // como máximo 3 anomalías distintas a la vez
    public const int MaxNormalStreak = 2;          // nunca más de 2 cuartos normales seguidos
    public const float DelayMin = 6f, DelayMax = 16f;

    /// Etapa de dificultad según el cuarto: 0 = cuartos 1-2, 1 = cuartos 3-5, 2 = cuartos 6-7.
    public static int Stage(int room) { return room <= 2 ? 0 : room <= 5 ? 1 : 2; }

    //                                          etapa:  0      1      2
    public static readonly float[] AnomalyChance =    { 0.70f, 0.85f, 0.95f };  // cuarto con anomalía
    public static readonly float[] TwoChance =        { 0.00f, 0.35f, 0.45f };  // dos anomalías a la vez
    public static readonly float[] ThreeChance =      { 0.00f, 0.00f, 0.25f };  // tres anomalías a la vez
    public static readonly bool[]  SubtleAllowed =    { false, true,  true  };  // placa 666, cajón, días sin incidentes...
    public static readonly float[] DelayedChance =    { 0.00f, 0.20f, 0.40f };  // una anomalía ocurre estando tú dentro
    public static readonly float[] ShadowChance =     { 0.00f, 0.30f, 0.45f };  // la sombra te persigue (solo susto)
    public static readonly float[] EchoChance =       { 0.00f, 0.20f, 0.35f };  // pasos que te siguen
    public static readonly float[] ScareChance =      { 0.25f, 0.60f, 0.85f };  // un susto (apagón, golpes, aparición, susurro)
    public static readonly float[] SecondScareChance ={ 0.00f, 0.20f, 0.45f };  // un segundo susto en el mismo cuarto

    // ---- Puertas (posición z que dispara el cambio de cuarto) ----
    public const float ExitZ = 30.75f;             // cruzar la puerta del fondo
    public const float BackZ = -0.75f;             // volver por la puerta de entrada (solo en el giro "ATRÁS")
    public const float WinZ = 31.2f;               // entrar en la sala blanca del último cuarto

    public const float Brightness = 1f;            // brillo base (el jugador lo ajusta en Opciones)

    // ---- Jugador ----
    public const float WalkSpeed = 2.6f;
    public const float RunSpeed = 4.0f;            // con Shift
    public const float ReportReach = 6f;           // distancia máxima para pulsar E
    public const float ReportCooldown = 0.4f;      // evita contar dos veces la misma pulsación
}
