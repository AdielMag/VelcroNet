namespace VelcroNet;

public static class SimulationConstants
{
    public const float FixedTimestep       = 1f / 60f;
    public const int   VelocityIterations  = 8;
    public const int   PositionIterations  = 3;
    public const int   MaxBodies           = 5000;
    public const int   MaxFixtures         = 10000;
    public const int   MaxContacts         = 20000;

    // Physics operates in meters; Unity scene is authored in pixels.
    // Divide by PixelsPerMeter when writing to the sim; multiply when reading back.
    public const float PixelsPerMeter      = 100f;
}
