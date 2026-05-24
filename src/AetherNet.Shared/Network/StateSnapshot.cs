namespace AetherNet.Network;

/// <summary>Lightweight tick header — bulk state lives in PhysicsWorldManager's pre-allocated buffer.</summary>
public struct StateSnapshot
{
    public uint  TickNumber;
    public float SimulationTime;
    public int   EntityCount;
    public uint  DeterminismHash; // FNV-1a over EntityState[] — zero when not computed
}
