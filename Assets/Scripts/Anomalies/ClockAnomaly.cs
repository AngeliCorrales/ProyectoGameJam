/// El reloj de pared empieza a girar hacia atrás.
public class ClockAnomaly : Anomaly
{
    public WallClock clock;

    protected override void OnActivate() { if (clock != null) clock.reverse = true; }
    protected override void OnReported() { if (clock != null) clock.reverse = false; }
}
