using SNV2 = System.Numerics.Vector2;

namespace VelcroNet;

public struct WorldConfig
{
    public SNV2 Gravity;
    public bool AllowSleeping;
    public int  MaxBodies;

    public static WorldConfig Default => new WorldConfig
    {
        Gravity      = new SNV2(0f, -9.81f),
        AllowSleeping = true,
        MaxBodies    = SimulationConstants.MaxBodies,
    };
}
